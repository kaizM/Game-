using System.Collections.Generic;
using UnityEngine;
using Meridian.GameAdmin;

namespace Harborlight
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class HarborlightPlayer : MonoBehaviour
    {
        private CharacterController _controller;
        private Animator _animator;
        private HarborlightGameState _state;
        private HarborlightVehicle _vehicle;
        private Renderer[] _visuals;
        private float _verticalVelocity;
        private HarborlightCamera _camera;
        private HarborlightGarageService _garage;
        private HarborlightNetworkPlayer _networkPlayer;
        private static readonly int LocomotionSpeed = Animator.StringToHash("Speed");

        public bool IsDriving => _vehicle != null;
        public string VehicleName => _vehicle != null ? _vehicle.DisplayName : "";
        public float VehicleSpeed => _vehicle != null ? _vehicle.Speed : 0f;

        public void Bind(HarborlightGameState state)
        {
            _state = state;
        }

        public void BindCamera(HarborlightCamera camera)
        {
            _camera = camera;
        }

        public void BindGarage(HarborlightGarageService garage)
        {
            _garage = garage;
        }

        public void BindNetworkPlayer(HarborlightNetworkPlayer networkPlayer)
        {
            _networkPlayer = networkPlayer;
        }

        public void RestoreSavedVehicle(IReadOnlyList<HarborlightVehicle> vehicles)
        {
            if (_state == null || !_state.TryConsumeVehicleResume(out var vehicleIndex, out var position, out var heading)) return;
            if (vehicles == null || vehicleIndex < 0 || vehicleIndex >= vehicles.Count || !_state.IsVehicleOwned(vehicleIndex)) return;

            var vehicle = vehicles[vehicleIndex];
            if (vehicle == null || vehicle.GarageIndex != vehicleIndex) return;
            var safePosition = HarborlightDistrictSurfaceNetwork.ClampToPlayableLand(
                new Vector2(position.x, Mathf.Clamp(position.z,
                    HarborlightDistrictSurfaceNetwork.PlayableMinZ,
                    HarborlightDistrictSurfaceNetwork.VehicleMaxZ)));
            vehicle.transform.SetPositionAndRotation(
                new Vector3(safePosition.x,
                    HarborlightDistrictSurfaceNetwork.TerrainHeight(safePosition.y) + 0.02f, safePosition.y),
                Quaternion.Euler(0f, heading, 0f));
            EnterVehicle(vehicle);
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _animator = GetComponentInChildren<Animator>(true);
            _visuals = GetComponentsInChildren<Renderer>();
            _networkPlayer = GetComponent<HarborlightNetworkPlayer>();
            if (_camera == null) _camera = FindAnyObjectByType<HarborlightCamera>();
        }

        private void Update()
        {
            if (_controller == null) return;

            if (_networkPlayer != null && _networkPlayer.IsSpawned)
            {
                var gameplayInputEnabled = (_state == null || (!_state.TutorialOpen && !_state.GarageOpen && !_state.BankOpen && !_state.InventoryOpen)) &&
                    !GameAdminOverlay.IsVisible;
                if (_networkPlayer.IsOwner && gameplayInputEnabled &&
                    (Input.GetKeyDown(KeyCode.E) || HarborlightTouchControls.ConsumeInteract()))
                {
                    if (TryGetNearestInteractable(out var interactable))
                    {
                        if (interactable.Action == WorldAction.OpenBank) _state?.OpenBank();
                        else _networkPlayer.RequestWorldInteraction(interactable.Action, interactable.transform.position);
                    }
                }
                return;
            }

            if (_state != null && _state.TutorialOpen)
            {
                SetLocomotionSpeed(0f);
                return;
            }

            if (_state != null && _state.GarageOpen)
            {
                SetLocomotionSpeed(0f);
                if (Input.GetKeyDown(KeyCode.Escape)) _state.CloseGarage();
                return;
            }

            if (_state != null && _state.BankOpen)
            {
                SetLocomotionSpeed(0f);
                if (Input.GetKeyDown(KeyCode.Escape)) _state.CloseBank();
                return;
            }

            if (_state != null && _state.InventoryOpen)
            {
                SetLocomotionSpeed(0f);
                if (Input.GetKeyDown(KeyCode.Escape)) _state.CloseInventory();
                return;
            }

            var input = HarborlightTouchControls.Move;
            if (!HarborlightTouchControls.IsSupported)
                input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            var horizontal = input.x;
            var vertical = input.y;
            if (_vehicle != null)
            {
                SetLocomotionSpeed(0f);
                var throttle = Mathf.Clamp(vertical + HarborlightTouchControls.Throttle, -1f, 1f);
                _vehicle.SetDriveInput(GameAdminOverlay.IsVisible ? 0f : throttle, GameAdminOverlay.IsVisible ? 0f : horizontal);
                if (!GameAdminOverlay.IsVisible && (Input.GetKeyDown(KeyCode.E) || HarborlightTouchControls.ConsumeExit())) ExitVehicle();
                return;
            }
            if (GameAdminOverlay.IsVisible) return;

            var forward = _camera != null ? _camera.PlanarForward : Vector3.forward;
            var right = _camera != null ? _camera.PlanarRight : Vector3.right;
            var direction = right * horizontal + forward * vertical;
            if (direction.sqrMagnitude > 1f) direction.Normalize();
            SetLocomotionSpeed(direction.magnitude);

            var speed = Input.GetKey(KeyCode.LeftShift) ? 7f : 4.5f;
            _verticalVelocity = _controller.isGrounded ? -1f : _verticalVelocity - 20f * Time.deltaTime;
            _controller.Move((direction * speed + Vector3.up * _verticalVelocity) * Time.deltaTime);

            var position = transform.position;
            if (position.y < -4f)
            {
                position = new Vector3(0f, 0f, -2f);
                _verticalVelocity = 0f;
            }
            var safePosition = HarborlightDistrictSurfaceNetwork.ClampToPlayableLand(
                new Vector2(position.x, position.z));
            position.x = safePosition.x;
            position.z = safePosition.y;
            transform.position = position;

            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 12f * Time.deltaTime);
            }

            if (Input.GetKeyDown(KeyCode.E) || HarborlightTouchControls.ConsumeInteract()) InteractNearby();
        }

        private void SetLocomotionSpeed(float speed)
        {
            if (_animator != null) _animator.SetFloat(LocomotionSpeed, speed, 0.12f, Time.deltaTime);
        }

        public Vector3 ReadNetworkMoveDirection()
        {
            if ((_state != null && (_state.TutorialOpen || _state.GarageOpen || _state.BankOpen || _state.InventoryOpen)) || GameAdminOverlay.IsVisible)
                return Vector3.zero;

            var input = HarborlightTouchControls.Move;
            if (!HarborlightTouchControls.IsSupported)
                input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            var forward = _camera != null ? _camera.PlanarForward : Vector3.forward;
            var right = _camera != null ? _camera.PlanarRight : Vector3.right;
            var direction = right * input.x + forward * input.y;
            return direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }

        public bool NetworkSprintRequested =>
            !((_state != null && (_state.TutorialOpen || _state.GarageOpen || _state.BankOpen || _state.InventoryOpen)) || GameAdminOverlay.IsVisible) &&
            Input.GetKey(KeyCode.LeftShift);

        public void ApplyAuthoritativeMovement(Vector3 direction, bool sprint)
        {
            if (_controller == null || !_controller.enabled) return;

            direction.y = 0f;
            direction = Vector3.ClampMagnitude(direction, 1f);
            SetLocomotionSpeed(direction.magnitude);
            _verticalVelocity = _controller.isGrounded ? -1f : _verticalVelocity - 20f * Time.deltaTime;
            _controller.Move((direction * (sprint ? 7f : 4.5f) + Vector3.up * _verticalVelocity) * Time.deltaTime);

            var position = transform.position;
            if (position.y < -4f)
            {
                position = new Vector3(0f, 0f, -2f);
                _verticalVelocity = 0f;
            }
            var safePosition = HarborlightDistrictSurfaceNetwork.ClampToPlayableLand(
                new Vector2(position.x, position.z));
            position.x = safePosition.x;
            position.z = safePosition.y;
            transform.position = position;

            if (direction.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 12f * Time.deltaTime);
        }

        public void ApplyNetworkLocomotion(float speed)
        {
            SetLocomotionSpeed(speed);
        }

        public void ApplyNetworkMissionSnapshot(int beaconStage, int courierStage, int inspectionStage,
            int credits, int reputation, int deliveriesCompleted, string message)
        {
            _state?.ApplyNetworkProgressSnapshot(beaconStage, courierStage, inspectionStage,
                credits, reputation, deliveriesCompleted, message);
        }

        public void ClearNetworkMissionSnapshot()
        {
            _state?.ClearNetworkProgressSnapshot();
        }

        public void ReportNetworkActionMessage(string message)
        {
            _state?.ReportMessage(message);
        }

        private void InteractNearby()
        {
            if (!TryGetNearestInteractable(out var nearest)) return;
            if (nearest.Action == WorldAction.EnterVehicle && nearest.Vehicle != null)
            {
                if (_garage != null && !_garage.CanUseVehicle(nearest.Vehicle.GarageIndex))
                {
                    _state?.ReportMessage("That vehicle is not in your collection. Visit Harborlight Motor Works to buy it.");
                    return;
                }
                EnterVehicle(nearest.Vehicle);
                return;
            }
            if (nearest.Action == WorldAction.OpenGarage && _garage != null)
            {
                _state?.OpenGarage();
                return;
            }
            if (_state != null) _state.Interact(nearest.Action);
        }

        private bool TryGetNearestInteractable(out HarborlightInteractable nearest)
        {
            var interactables = FindObjectsByType<HarborlightInteractable>();
            nearest = null;
            var nearestDistance = 3.2f;
            foreach (var candidate in interactables)
            {
                if (candidate == null) continue;
                var distance = Vector3.Distance(transform.position, candidate.transform.position);
                if (distance >= nearestDistance) continue;
                nearest = candidate;
                nearestDistance = distance;
            }

            if (nearest == null) return false;
            return true;
        }

        private void EnterVehicle(HarborlightVehicle vehicle)
        {
            _vehicle = vehicle;
            _vehicle.SetOccupied(true);
            _controller.enabled = false;
            foreach (var visual in _visuals) visual.enabled = false;
            transform.SetParent(vehicle.transform, false);
            transform.localPosition = new Vector3(0f, 0.65f, 0f);
            transform.localRotation = Quaternion.identity;
        }

        private void ExitVehicle()
        {
            var vehicle = _vehicle;
            var exitPosition = vehicle.transform.position + vehicle.transform.right * 2.15f;
            transform.SetParent(null, true);
            transform.SetPositionAndRotation(exitPosition, vehicle.transform.rotation);
            _controller.enabled = true;
            _verticalVelocity = 0f;
            foreach (var visual in _visuals) visual.enabled = true;
            _vehicle = null;
            vehicle.SetOccupied(false);
        }
    }

    public sealed class HarborlightInteractable : MonoBehaviour
    {
        public WorldAction Action;
        public string DisplayName;
        public HarborlightVehicle Vehicle;
    }
}
