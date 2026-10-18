using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Harborlight
{
    [RequireComponent(typeof(HarborlightPlayer))]
    [RequireComponent(typeof(CharacterController))]
    public sealed class HarborlightNetworkPlayer : NetworkBehaviour
    {
        private const float InputSendInterval = 1f / 20f;
        private const float InputTimeout = 0.3f;
        private const float MinimumInputInterval = 0.03f;
        private readonly NetworkVariable<float> _locomotionSpeed = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<FixedString64Bytes> _authenticatedPlayerId = new NetworkVariable<FixedString64Bytes>(
            default, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _beaconStage = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _courierStage = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _inspectionStage = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _credits = new NetworkVariable<int>(
            125, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _reputation = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _deliveriesCompleted = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<FixedString64Bytes> _lastMissionAction = new NetworkVariable<FixedString64Bytes>(
            default, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<FixedString128Bytes> _missionMessage = new NetworkVariable<FixedString128Bytes>(
            default, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _missionRevision = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

        private HarborlightPlayer _player;
        private Guid _pendingAuthenticatedPlayerId;
        private Vector3 _serverDirection;
        private bool _serverSprint;
        private float _lastInputAt;
        private float _lastAcceptedInputAt;
        private float _nextInputSendAt;
        private float _lastInteractionAt;
        private float _nextSmokeProgressLogAt;
        private Vector3 _spawnPosition;
        private bool _movementEvidenceLogged;
        private bool _smokeInputEvidenceLogged;
        private bool _smokeAnimationEvidenceLogged;
        private bool _smokeInteractionRequested;
        private bool _smokeInteractionAckLogged;
        private bool _smokeTargetMissingLogged;
        private bool _smokeAllJobsCompleteLogged;

        public bool TryGetAuthenticatedPlayerId(out Guid playerId)
        {
            playerId = Guid.Empty;
            return IsServer && Guid.TryParse(_authenticatedPlayerId.Value.ToString(), out playerId);
        }

        public void BindAuthenticatedPlayerId(Guid playerId)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
                throw new InvalidOperationException("Only the authoritative game server can bind a player identity.");
            if (IsSpawned || playerId == Guid.Empty)
                throw new InvalidOperationException("A valid player identity must be bound before network spawn.");
            _pendingAuthenticatedPlayerId = playerId;
        }

        public void ApplyAuthoritativeMissionSnapshot(int beaconStage, int courierStage, int inspectionStage,
            int credits, int reputation, int deliveriesCompleted, string action, string message)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
                throw new InvalidOperationException("Only the authoritative game server can update mission progress.");

            _beaconStage.Value = beaconStage;
            _courierStage.Value = courierStage;
            _inspectionStage.Value = inspectionStage;
            _credits.Value = credits;
            _reputation.Value = reputation;
            _deliveriesCompleted.Value = deliveriesCompleted;
            _lastMissionAction.Value = new FixedString64Bytes(action ?? string.Empty);
            _missionMessage.Value = new FixedString128Bytes(message ?? string.Empty);
            _missionRevision.Value++;
            ApplyMissionSnapshotToOwner();
        }

        public void ReportServerActionMessage(string message)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
                throw new InvalidOperationException("Only the authoritative game server can report a network action result.");

            _lastMissionAction.Value = default;
            _missionMessage.Value = new FixedString128Bytes(message ?? string.Empty);
            _missionRevision.Value++;
            ApplyMissionSnapshotToOwner();
        }

        public override void OnNetworkSpawn()
        {
            _player = GetComponent<HarborlightPlayer>();
            _player.BindNetworkPlayer(this);
            GetComponent<CharacterController>().enabled = IsServer;
            _locomotionSpeed.OnValueChanged += OnLocomotionSpeedChanged;
            _missionRevision.OnValueChanged += OnMissionRevisionChanged;
            _player.ApplyNetworkLocomotion(_locomotionSpeed.Value);

            if (IsServer)
            {
                if (_pendingAuthenticatedPlayerId == Guid.Empty)
                    throw new InvalidOperationException("An authenticated player identity is required before network spawn.");
                _authenticatedPlayerId.Value = new FixedString64Bytes(_pendingAuthenticatedPlayerId.ToString("D"));
                _pendingAuthenticatedPlayerId = Guid.Empty;

                var x = 0f;
                var z = 0f;
                if (OwnerClientId < 8)
                {
                    var slot = (int)(OwnerClientId % 8);
                    x = -7.5f + slot % 4 * 5f;
                    z = -12f - slot / 4 * 4f;
                }
                else
                {
                    var distributedClientId = OwnerClientId - 8;
                    var entryPointCount = (ulong)HarborlightDistrictSurfaceNetwork.MultiplayerEntryPoints.Length;
                    var entryPoint = HarborlightDistrictSurfaceNetwork.MultiplayerEntryPoints[
                        (int)(distributedClientId % entryPointCount)];
                    var entrySlot = (int)(distributedClientId / entryPointCount %
                        (ulong)HarborlightDistrictSurfaceNetwork.MultiplayerEntrySlotsPerZone);
                    x = entryPoint.x + (entrySlot % 5 - 2) * 3f;
                    z = entryPoint.y + (entrySlot / 5 - 2) * 3f;
                }
                if (HarborlightNetworkSession.AutomatedInterestSmokeTest)
                {
                    x = OwnerClientId % 2 == 1
                        ? -HarborlightNetworkSession.InterestSmokeSeparationMeters / 2f
                        : HarborlightNetworkSession.InterestSmokeSeparationMeters / 2f;
                    z = -1200f;
                }
                else if (HarborlightNetworkSession.AutomatedSmokeTest)
                {
                    x = OwnerClientId % 2 == 1 ? -2.5f : -1.5f;
                    z = -0.5f;
                }
                var position = new Vector3(x, HarborlightDistrictSurfaceNetwork.TerrainHeight(z) + 0.02f, z);
                transform.SetPositionAndRotation(position, Quaternion.identity);
                _lastInputAt = Time.time;
            }
            _spawnPosition = transform.position;
            if (HarborlightNetworkSession.AutomatedSmokeTest)
                Debug.Log("HL_NET_PLAYER_SPAWN owner=" + OwnerClientId + " server=" + IsServer + " ownerLocal=" + IsOwner + " position=" + transform.position.ToString("F2"));

            if (IsOwner)
            {
                HarborlightBootstrap.Current?.AttachNetworkPlayer(_player, this);
                ApplyMissionSnapshotToOwner();
            }
        }

        public override void OnNetworkDespawn()
        {
            _locomotionSpeed.OnValueChanged -= OnLocomotionSpeedChanged;
            _missionRevision.OnValueChanged -= OnMissionRevisionChanged;
            if (IsOwner) _player?.ClearNetworkMissionSnapshot();
        }

        public void RequestWorldInteraction(WorldAction action, Vector3 targetPosition)
        {
            if (!IsOwner) return;
            RequestWorldInteractionServerRpc((int)action, targetPosition.x, targetPosition.y, targetPosition.z);
        }

        public void RequestPoliceDispatchResolution(string dispatchCode, Vector3 targetPosition)
        {
            if (!IsOwner || string.IsNullOrEmpty(dispatchCode) || dispatchCode.Length > 48) return;
            ResolvePoliceDispatchServerRpc(dispatchCode, targetPosition.x, targetPosition.y, targetPosition.z);
        }

        public void ApplyPoliceDispatchResult(int credits, string message)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
                throw new InvalidOperationException("Only the authoritative game server can update police dispatch progress.");
            ApplyAuthoritativeMissionSnapshot(_beaconStage.Value, _courierStage.Value, _inspectionStage.Value,
                credits, _reputation.Value, _deliveriesCompleted.Value, "ResolveDispatch", message);
        }

        private void Update()
        {
            if (!IsSpawned) return;

            if (IsOwner)
            {
                var direction = HarborlightNetworkSession.AutomatedSmokeTest
                    ? ReadSmokeTestDirection()
                    : _player.ReadNetworkMoveDirection();
                var sprint = !HarborlightNetworkSession.AutomatedSmokeTest && _player.NetworkSprintRequested;
                if (IsServer)
                {
                    _serverDirection = direction;
                    _serverSprint = sprint;
                    _lastInputAt = Time.time;
                }
                else if (Time.time >= _nextInputSendAt)
                {
                    _nextInputSendAt = Time.time + InputSendInterval;
                    SubmitInputServerRpc(direction.x, direction.z, sprint);
                }
            }

            if (!IsServer) return;

            var directionToApply = Time.time - _lastInputAt > InputTimeout ? Vector3.zero : _serverDirection;
            _player.ApplyAuthoritativeMovement(directionToApply, _serverSprint);
            var speed = _player.NetworkLocomotionSpeed;
            if (Mathf.Abs(_locomotionSpeed.Value - speed) > 0.05f)
                _locomotionSpeed.Value = speed;
            if (HarborlightNetworkSession.AutomatedSmokeTest && !_movementEvidenceLogged &&
                Vector3.Distance(_spawnPosition, transform.position) > 0.5f)
            {
                _movementEvidenceLogged = true;
                Debug.Log("HL_NET_SERVER_MOVED owner=" + OwnerClientId + " position=" + transform.position.ToString("F2"));
            }
            if (HarborlightNetworkSession.AutomatedSmokeTest && Time.time >= _nextSmokeProgressLogAt)
            {
                _nextSmokeProgressLogAt = Time.time + 2f;
                Debug.Log("HL_NET_PROGRESS owner=" + OwnerClientId + " position=" + transform.position.ToString("F2") +
                    " input=" + _serverDirection.ToString("F2") + " inputAge=" + (Time.time - _lastInputAt).ToString("F2"));
            }
        }

        [ServerRpc]
        private void SubmitInputServerRpc(float directionX, float directionZ, bool sprint, ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId ||
                Time.time - _lastAcceptedInputAt < MinimumInputInterval ||
                float.IsNaN(directionX) || float.IsNaN(directionZ) ||
                float.IsInfinity(directionX) || float.IsInfinity(directionZ))
                return;

            var input = new Vector2(directionX, directionZ);
            if (input.sqrMagnitude > 1.05f * 1.05f) return;
            _serverDirection = Vector3.ClampMagnitude(new Vector3(directionX, 0f, directionZ), 1f);
            _serverSprint = sprint;
            _lastInputAt = Time.time;
            _lastAcceptedInputAt = Time.time;
            if (HarborlightNetworkSession.AutomatedSmokeTest && !_smokeInputEvidenceLogged)
            {
                _smokeInputEvidenceLogged = true;
                Debug.Log("HL_NET_INPUT_ACCEPTED owner=" + OwnerClientId + " direction=" + _serverDirection.ToString("F2"));
            }
        }

        [ServerRpc]
        private void RequestWorldInteractionServerRpc(int actionValue, float targetX, float targetY, float targetZ,
            ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId || Time.time - _lastInteractionAt < 0.75f ||
                float.IsNaN(targetX) || float.IsNaN(targetY) || float.IsNaN(targetZ) ||
                float.IsInfinity(targetX) || float.IsInfinity(targetY) || float.IsInfinity(targetZ))
                return;

            var action = (WorldAction)actionValue;
            if (!System.Enum.IsDefined(typeof(WorldAction), action)) return;

            var requestedTarget = new Vector3(targetX, targetY, targetZ);
            var nearby = FindObjectsByType<HarborlightInteractable>();
            var valid = false;
            foreach (var candidate in nearby)
            {
                if (candidate == null || candidate.Action != action ||
                    Vector3.Distance(transform.position, candidate.transform.position) > 3.2f ||
                    Vector3.Distance(requestedTarget, candidate.transform.position) > 1.25f)
                    continue;
                valid = true;
                break;
            }
            if (!valid) return;

            _lastInteractionAt = Time.time;
            var session = NetworkManager.Singleton.GetComponent<HarborlightNetworkSession>();
            if (session == null)
            {
                ReportServerActionMessage("The online mission service is unavailable.");
                return;
            }
            session.SubmitMissionAction(this, action);
        }

        [ServerRpc]
        private void ResolvePoliceDispatchServerRpc(string dispatchCode, float targetX, float targetY, float targetZ,
            ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId || Time.time - _lastInteractionAt < 0.75f ||
                string.IsNullOrEmpty(dispatchCode) || dispatchCode.Length > 48 ||
                float.IsNaN(targetX) || float.IsNaN(targetY) || float.IsNaN(targetZ) ||
                float.IsInfinity(targetX) || float.IsInfinity(targetY) || float.IsInfinity(targetZ))
                return;

            var requestedTarget = new Vector3(targetX, targetY, targetZ);
            var valid = false;
            foreach (var candidate in FindObjectsByType<HarborlightPoliceDispatchSite>())
            {
                if (candidate == null || candidate.DispatchCode != dispatchCode ||
                    Vector3.Distance(transform.position, candidate.transform.position) > 3.2f ||
                    Vector3.Distance(requestedTarget, candidate.transform.position) > 1.25f)
                    continue;
                valid = true;
                break;
            }
            if (!valid) return;

            _lastInteractionAt = Time.time;
            var session = NetworkManager.Singleton.GetComponent<HarborlightNetworkSession>();
            if (session == null)
            {
                ReportServerActionMessage("The online dispatch service is unavailable.");
                return;
            }
            session.SubmitPoliceDispatchResolution(this, dispatchCode);
        }

        private void OnLocomotionSpeedChanged(float previous, float current)
        {
            if (_player != null) _player.ApplyNetworkLocomotion(current);
            if (IsClient && !IsServer && HarborlightNetworkSession.AutomatedSmokeTest &&
                current > 0.1f && !_smokeAnimationEvidenceLogged)
            {
                _smokeAnimationEvidenceLogged = true;
                Debug.Log("HL_NET_ANIMATION_SPEED owner=" + OwnerClientId + " value=" + current.ToString("F2"));
            }
        }

        private void OnMissionRevisionChanged(int previous, int current)
        {
            if (IsOwner && _player != null)
            {
                ApplyMissionSnapshotToOwner();
                if (HarborlightNetworkSession.AutomatedSmokeTest)
                {
                    if (!_smokeInteractionAckLogged)
                    {
                        _smokeInteractionAckLogged = true;
                        Debug.Log("HL_NET_INTERACTION_ACK owner=" + OwnerClientId + " action=" + _lastMissionAction.Value);
                    }
                    if (HarborlightNetworkSession.AutomatedJobsSmokeTest)
                    {
                        _smokeInteractionRequested = false;
                        _smokeInteractionAckLogged = false;
                    }
                }
                if (_lastMissionAction.Value.ToString() == "ResolveDispatch")
                    _player.RefreshPoliceCareer();
            }
        }

        private void ApplyMissionSnapshotToOwner()
        {
            if (!IsOwner || _player == null) return;
            _player.ApplyNetworkMissionSnapshot(_beaconStage.Value, _courierStage.Value, _inspectionStage.Value,
                _credits.Value, _reputation.Value, _deliveriesCompleted.Value, _missionMessage.Value.ToString());
        }

        private Vector3 ReadSmokeTestDirection()
        {
            if (HarborlightNetworkSession.AutomatedInterestSmokeTest) return Vector3.zero;

            var targetAction = WorldAction.AcceptBeaconRepair;
            if (HarborlightNetworkSession.AutomatedJobsSmokeTest)
            {
                if (_beaconStage.Value == 1)
                    targetAction = WorldAction.RepairBeacon;
                else if (_beaconStage.Value >= 2 && _courierStage.Value == 0)
                    targetAction = WorldAction.AcceptCourierDelivery;
                else if (_courierStage.Value == 1)
                    targetAction = WorldAction.CollectCourierParcel;
                else if (_courierStage.Value == 2)
                    targetAction = WorldAction.DeliverCourierParcel;
                else if (_courierStage.Value >= 3 && _inspectionStage.Value == 0)
                    targetAction = WorldAction.AcceptDockInspection;
                else if (_inspectionStage.Value == 1)
                    targetAction = WorldAction.InspectDock;
                else if (_inspectionStage.Value == 2)
                    targetAction = WorldAction.AcceptDockInspection;
                else if (_beaconStage.Value >= 2 && _courierStage.Value >= 3 && _inspectionStage.Value >= 3)
                {
                    if (!_smokeAllJobsCompleteLogged)
                    {
                        _smokeAllJobsCompleteLogged = true;
                        Debug.Log("HL_NET_ALL_JOBS_COMPLETE owner=" + OwnerClientId +
                            " beacon=" + _beaconStage.Value + " courier=" + _courierStage.Value +
                            " inspection=" + _inspectionStage.Value + " credits=" + _credits.Value +
                            " reputation=" + _reputation.Value + " deliveries=" + _deliveriesCompleted.Value);
                    }
                    return Vector3.zero;
                }
            }

            var interactables = FindObjectsByType<HarborlightInteractable>();
            HarborlightInteractable target = null;
            var targetDistance = float.MaxValue;
            foreach (var candidate in interactables)
            {
                if (candidate == null || candidate.Action != targetAction) continue;
                var distance = Vector3.Distance(transform.position, candidate.transform.position);
                if (distance >= targetDistance) continue;
                target = candidate;
                targetDistance = distance;
            }

            if (target == null)
            {
                if (!_smokeTargetMissingLogged)
                {
                    _smokeTargetMissingLogged = true;
                    Debug.LogError("HL_NET_SMOKE_TARGET_MISSING owner=" + OwnerClientId + " action=" + targetAction);
                }
                return Vector3.zero;
            }

            if (targetDistance <= 2.5f)
            {
                if (!_smokeInteractionRequested)
                {
                    _smokeInteractionRequested = true;
                    RequestWorldInteraction(target.Action, target.transform.position);
                }
                return Vector3.zero;
            }

            var direction = target.transform.position - transform.position;
            direction.y = 0f;
            return direction.normalized;
        }
    }
}
