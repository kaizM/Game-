using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Networking;

namespace Harborlight
{
    public sealed class HarborlightNetworkSession : MonoBehaviour
    {
        private const ushort DefaultPort = 7777;
        private const int SmokeTestLifetimeSeconds = 15;
        private const int MaximumSmokeTestLifetimeSeconds = 120;
        private const float MaximumReconnectDelaySeconds = 30f;
        private const float PlayerInterestUpdateInterval = 0.5f;
        private const float PlayerShowDistance = 700f;
        private const float PlayerHideDistance = 850f;
        private const float PlayerInterestCellSize = 200f;
        private const int PlayerInterestCellRadius = 4;
        private const string NetworkPlayerPath = "Network/HarborlightNetworkPlayer";
        private const string BackendSettingsPath = "HarborlightBackendSettings";
        private NetworkManager _manager;
        private HarborlightBackendClient _backendClient;
        private GameObject _playerPrefab;
        private RSA _serverTicketPrivateKey;
        private string _serverApiBaseUrl;
        private string _serverSecret;
        private readonly Dictionary<ulong, Guid> _authenticatedPlayers = new Dictionary<ulong, Guid>();
        private readonly HashSet<ulong> _pendingMissionActions = new HashSet<ulong>();
        private readonly Dictionary<ulong, HarborlightNetworkPlayer> _interestPlayers = new Dictionary<ulong, HarborlightNetworkPlayer>();
        private readonly Dictionary<Vector2Int, List<HarborlightNetworkPlayer>> _interestCells = new Dictionary<Vector2Int, List<HarborlightNetworkPlayer>>();
        private readonly Stack<List<HarborlightNetworkPlayer>> _interestCellPool = new Stack<List<HarborlightNetworkPlayer>>();
        private readonly List<ulong> _interestHideClients = new List<ulong>();
        private string _launchHost;
        private ushort _launchPort;
        private float _smokeTestStartedAt;
        private float _smokeTestExitAt;
        private float _nextPlayerInterestUpdateAt;
        private float _reconnectAt;
        private int _reconnectAttempt;
        private bool _isClientSession;
        private bool _reconnectPending;
        private bool _intentionalShutdown;
        private bool _ticketRequestPending;
        private float _interestSmokeObservationAt;
        private bool _interestSmokeEvidenceLogged;
        private float _interestSmokeTransitionsStartedAt;
        private int _interestSmokeTransitionPhase = -1;
        private int _interestSmokeAssertionCount;

        public static bool AutomatedSmokeTest { get; private set; }
        public static bool AutomatedJobsSmokeTest { get; private set; }
        public static bool AutomatedInterestSmokeTest { get; private set; }
        public static bool AutomatedInterestSmokeTransitions { get; private set; }
        public static int InterestSmokeSeparationMeters { get; private set; }

        public sealed class LaunchOptions
        {
            public bool IsServer;
            public string Host;
            public string ListenAddress = "127.0.0.1";
            public ushort Port = DefaultPort;
            public bool SmokeTest;
            public bool SmokeTestJobs;
            public bool SmokeTestInterest;
            public bool SmokeTestInterestTransitions;
            public int InterestSmokeSeparationMeters;
            public int SmokeTestDurationSeconds = SmokeTestLifetimeSeconds;
            public bool IsValid = true;
            public string Error;
        }

        public static bool TryReadLaunchOptions(out LaunchOptions options)
        {
            options = new LaunchOptions();
            var arguments = Environment.GetCommandLineArgs();
            var foundMode = false;
            var smokeDurationSpecified = false;
            for (var index = 0; index < arguments.Length; index++)
            {
                if (arguments[index] == "-harborlight-server")
                {
                    options.IsServer = true;
                    foundMode = true;
                }
                else if (arguments[index] == "-harborlight-connect")
                {
                    foundMode = true;
                    if (index + 1 < arguments.Length) options.Host = arguments[++index];
                    else SetInvalid(options, "-harborlight-connect requires a host address.");
                }
                else if (arguments[index] == "-harborlight-port")
                {
                    if (index + 1 >= arguments.Length ||
                        !ushort.TryParse(arguments[++index], out var port) || port == 0)
                        SetInvalid(options, "-harborlight-port requires a number between 1 and 65535.");
                    else options.Port = port;
                }
                else if (arguments[index] == "-harborlight-listen")
                {
                    if (index + 1 < arguments.Length) options.ListenAddress = arguments[++index];
                    else SetInvalid(options, "-harborlight-listen requires an IPv4 address.");
                }
                else if (arguments[index] == "-harborlight-smoke-test")
                {
                    options.SmokeTest = true;
                }
                else if (arguments[index] == "-harborlight-smoke-test-jobs")
                {
                    options.SmokeTestJobs = true;
                }
                else if (arguments[index] == "-harborlight-smoke-test-interest-separation")
                {
                    options.SmokeTest = true;
                    options.SmokeTestInterest = true;
                    if (index + 1 >= arguments.Length ||
                        !int.TryParse(arguments[++index], out var separation) || separation < 1 || separation > 4000)
                        SetInvalid(options, "-harborlight-smoke-test-interest-separation requires a distance from 1 to 4000 metres.");
                    else options.InterestSmokeSeparationMeters = separation;
                }
                else if (arguments[index] == "-harborlight-smoke-test-interest-transitions")
                {
                    options.SmokeTest = true;
                    options.SmokeTestInterest = true;
                    options.SmokeTestInterestTransitions = true;
                    options.InterestSmokeSeparationMeters = (int)PlayerShowDistance;
                }
                else if (arguments[index] == "-harborlight-smoke-test-seconds")
                {
                    if (index + 1 >= arguments.Length ||
                        !int.TryParse(arguments[++index], out var seconds) ||
                        seconds < SmokeTestLifetimeSeconds || seconds > MaximumSmokeTestLifetimeSeconds)
                        SetInvalid(options, "-harborlight-smoke-test-seconds requires a duration from 15 to 120.");
                    else
                    {
                        options.SmokeTestDurationSeconds = seconds;
                        smokeDurationSpecified = true;
                    }
                }
            }

            if (foundMode && !options.IsServer && string.IsNullOrWhiteSpace(options.Host))
                SetInvalid(options, "A client launch requires -harborlight-connect <host>.");
            if (foundMode && options.IsServer && !IsLoopbackAddress(options.ListenAddress))
                SetInvalid(options, "Direct NGO transport is restricted to loopback until a secure transport is configured.");
            if (foundMode && !options.IsServer && !IsLoopbackHost(options.Host))
                SetInvalid(options, "Direct NGO transport is restricted to loopback until a secure transport is configured.");
            if (options.SmokeTest && !Debug.isDebugBuild)
                SetInvalid(options, "-harborlight-smoke-test is available only in development builds.");
            if (options.SmokeTestJobs && !options.SmokeTest)
                SetInvalid(options, "-harborlight-smoke-test-jobs requires -harborlight-smoke-test.");
            if (options.SmokeTestJobs && !smokeDurationSpecified)
                options.SmokeTestDurationSeconds = MaximumSmokeTestLifetimeSeconds;
            if (options.SmokeTestInterestTransitions && !smokeDurationSpecified)
                options.SmokeTestDurationSeconds = 25;
            return foundMode;
        }

        public static HarborlightNetworkSession Start(LaunchOptions options)
        {
            var sessionObject = new GameObject(options.IsServer
                ? "Harborlight Dedicated Server"
                : "Harborlight Network Client");
            DontDestroyOnLoad(sessionObject);
            var session = sessionObject.AddComponent<HarborlightNetworkSession>();
            AutomatedSmokeTest = options.SmokeTest;
            AutomatedJobsSmokeTest = options.SmokeTestJobs;
            AutomatedInterestSmokeTest = options.SmokeTestInterest;
            AutomatedInterestSmokeTransitions = options.SmokeTestInterestTransitions;
            InterestSmokeSeparationMeters = options.InterestSmokeSeparationMeters;
            session._isClientSession = !options.IsServer && options.IsValid;
            session._launchHost = options.Host;
            session._launchPort = options.Port;
            if (!options.IsServer)
            {
                session._backendClient = sessionObject.AddComponent<HarborlightBackendClient>();
                session._backendClient.Connect(Resources.Load<HarborlightBackendSettings>(BackendSettingsPath));
            }
            if (options.SmokeTest)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 30;
                session._smokeTestStartedAt = Time.realtimeSinceStartup;
                session._smokeTestExitAt = Time.realtimeSinceStartup + options.SmokeTestDurationSeconds;
                if (options.SmokeTestInterest)
                    session._interestSmokeObservationAt = Time.realtimeSinceStartup + 4f;
            }
            session.ConfigureAndStart(options);
            return session;
        }

        private void Update()
        {
            if (_manager != null && _manager.IsListening && _manager.IsServer &&
                AutomatedInterestSmokeTransitions)
                UpdateInterestSmokeTransitions();

            if (_manager != null && _manager.IsListening && _manager.IsServer &&
                Time.unscaledTime >= _nextPlayerInterestUpdateAt)
            {
                _nextPlayerInterestUpdateAt = Time.unscaledTime + PlayerInterestUpdateInterval;
                RefreshPlayerInterest();
            }

            if (AutomatedSmokeTest && Time.realtimeSinceStartup >= _smokeTestExitAt)
            {
                Debug.Log("HL_NET_SMOKE_EXIT");
                _intentionalShutdown = true;
                if (_manager != null && _manager.IsListening) _manager.Shutdown();
                Application.Quit();
                return;
            }

            if (AutomatedInterestSmokeTransitions)
                RecordInterestSmokeTransitionEvidence();

            if (AutomatedInterestSmokeTest && !_interestSmokeEvidenceLogged && _manager != null &&
                !AutomatedInterestSmokeTransitions &&
                _manager.IsListening && _manager.IsClient && !_manager.IsServer &&
                Time.realtimeSinceStartup >= _interestSmokeObservationAt)
            {
                _interestSmokeEvidenceLogged = true;
                var visiblePlayers = 0;
                foreach (var player in FindObjectsByType<HarborlightNetworkPlayer>())
                {
                    if (player != null && player.IsSpawned) visiblePlayers++;
                }
                var expectedPlayers = InterestSmokeSeparationMeters <= PlayerShowDistance ? 2 : 1;
                var result = visiblePlayers == expectedPlayers ? "PASS" : "FAIL";
                Debug.Log("HL_NET_INTEREST_ASSERT client=" + _manager.LocalClientId +
                    " separation=" + InterestSmokeSeparationMeters + " visible=" + visiblePlayers +
                    " expected=" + expectedPlayers + " result=" + result);
            }

            if (!_reconnectPending || Time.realtimeSinceStartup < _reconnectAt || _manager == null ||
                _manager.IsListening || _manager.ShutdownInProgress)
                return;

            _reconnectPending = false;
            StartClientWithTicket();
        }

        private void ConfigureAndStart(LaunchOptions options)
        {
            if (!options.IsValid)
            {
                Debug.LogError("Harborlight network launch failed: " + options.Error);
                return;
            }

            var playerPrefab = Resources.Load<GameObject>(NetworkPlayerPath);
            if (playerPrefab == null || playerPrefab.GetComponent<NetworkObject>() == null)
            {
                Debug.LogError("Harborlight network player prefab is missing or has no NetworkObject: Resources/" + NetworkPlayerPath);
                return;
            }
            _playerPrefab = playerPrefab;

            var transport = gameObject.AddComponent<UnityTransport>();
            _manager = gameObject.AddComponent<NetworkManager>();
            _manager.NetworkConfig = new NetworkConfig();
            _manager.NetworkConfig.NetworkTransport = transport;
            _manager.NetworkConfig.PlayerPrefab = playerPrefab;
            _manager.NetworkConfig.TickRate = 30;
            _manager.NetworkConfig.ConnectionApproval = true;
            _manager.NetworkConfig.ClientConnectionBufferTimeout = 20;
            _manager.NetworkConfig.EnableSceneManagement = false;

            if (options.IsServer)
            {
                if (!TryLoadServerCredentials())
                {
                    Debug.LogError("Harborlight server requires a valid API URL, server secret, and RSA private key in its environment.");
                    return;
                }
                Application.runInBackground = true;
                transport.SetConnectionData("127.0.0.1", options.Port, options.ListenAddress);
                _manager.ConnectionApprovalCallback = ApproveConnection;
                _manager.OnClientConnectedCallback += OnClientConnected;
                _manager.OnClientDisconnectCallback += OnClientDisconnected;
                if (!_manager.StartServer())
                    Debug.LogError("Harborlight dedicated server failed to start on " + options.ListenAddress + ":" + options.Port);
                else
                    Debug.Log("Harborlight dedicated server listening on " + options.ListenAddress + ":" + options.Port);
                return;
            }

            transport.SetConnectionData(options.Host, options.Port);
            _manager.OnClientConnectedCallback += OnClientConnected;
            _manager.OnClientDisconnectCallback += OnClientDisconnected;
            StartClientWithTicket();
        }

        private void OnClientConnected(ulong clientId)
        {
            if (_isClientSession)
            {
                _reconnectPending = false;
                _reconnectAttempt = 0;
            }
            else
                StartCoroutine(LoadMissionProgressAndSpawn(clientId));
            if (AutomatedSmokeTest) Debug.Log("HL_NET_CONNECTED client=" + clientId);
        }

        private void OnClientDisconnected(ulong clientId)
        {
            _authenticatedPlayers.Remove(clientId);
            _pendingMissionActions.Remove(clientId);
            if (AutomatedSmokeTest) Debug.Log("HL_NET_DISCONNECTED client=" + clientId);
            ScheduleReconnect();
        }

        private void StartClientWithTicket()
        {
            if (_ticketRequestPending || _backendClient == null || _manager == null || _intentionalShutdown) return;
            _ticketRequestPending = true;
            _backendClient.RequestRealtimeJoinTicket((ticket, error) =>
            {
                _ticketRequestPending = false;
                if (_intentionalShutdown || _manager == null) return;
                if (string.IsNullOrEmpty(ticket) || !_backendClient.TryEncryptRealtimeJoinTicket(ticket, out var encryptedTicket))
                {
                    Debug.LogWarning(error ?? "Harborlight realtime sign-in could not encrypt its join ticket.");
                    ScheduleReconnect();
                    return;
                }

                _manager.NetworkConfig.ConnectionData = encryptedTicket;
                if (!_manager.StartClient())
                {
                    Debug.LogWarning("Harborlight client failed to start for " + _launchHost + ":" + _launchPort);
                    ScheduleReconnect();
                }
                else if (AutomatedSmokeTest)
                    Debug.Log("HL_NET_CONNECTING host=" + _launchHost + " port=" + _launchPort + " attempt=" + _reconnectAttempt);
            });
        }

        private bool TryLoadServerCredentials()
        {
            var apiBaseUrl = Environment.GetEnvironmentVariable("HARBORLIGHT_API_BASE_URL");
            _serverSecret = Environment.GetEnvironmentVariable("HARBORLIGHT_REALTIME_SERVER_SECRET");
            var privateKeyBase64 = Environment.GetEnvironmentVariable("HARBORLIGHT_REALTIME_PRIVATE_KEY_DER_BASE64");
            if (!TryNormalizeApiBaseUrl(apiBaseUrl, out _serverApiBaseUrl))
            {
                Debug.LogError("Harborlight server API URL is missing or invalid.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(_serverSecret) || _serverSecret.Length < 32)
            {
                Debug.LogError("Harborlight realtime server secret is missing or shorter than 32 characters.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(privateKeyBase64))
            {
                Debug.LogError("Harborlight realtime RSA private key is missing.");
                return false;
            }

            try
            {
                var privateKey = Convert.FromBase64String(privateKeyBase64);
                _serverTicketPrivateKey = HarborlightRsaKeyImport.CreateFromPkcs8PrivateKey(privateKey);
                return true;
            }
            catch (CryptographicException exception)
            {
                Debug.LogError("Harborlight realtime RSA private key could not be imported as PKCS#8: " + exception.Message);
                _serverTicketPrivateKey?.Dispose();
                _serverTicketPrivateKey = null;
                return false;
            }
            catch (FormatException)
            {
                Debug.LogError("Harborlight realtime RSA private key is not valid base64.");
                _serverTicketPrivateKey?.Dispose();
                _serverTicketPrivateKey = null;
                return false;
            }
            catch (PlatformNotSupportedException)
            {
                Debug.LogError("Harborlight realtime RSA PKCS#8 import is unsupported by this runtime.");
                _serverTicketPrivateKey?.Dispose();
                _serverTicketPrivateKey = null;
                return false;
            }
        }

        private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = false;
            response.CreatePlayerObject = false;
            response.Reason = "Harborlight profile authentication failed.";
            if (request.Payload == null || request.Payload.Length > 512 || _serverTicketPrivateKey == null)
            {
                response.Pending = false;
                return;
            }

            string ticket;
            try
            {
                var plaintext = _serverTicketPrivateKey.Decrypt(request.Payload, HarborlightRsaKeyImport.JoinTicketPadding);
                if (plaintext.Length != 43)
                {
                    response.Pending = false;
                    return;
                }
                ticket = Encoding.UTF8.GetString(plaintext);
            }
            catch (CryptographicException)
            {
                response.Pending = false;
                return;
            }

            response.Pending = true;
            StartCoroutine(ValidateTicketRoutine(request.ClientNetworkId, ticket, response));
        }

        private IEnumerator ValidateTicketRoutine(ulong clientId, string ticket,
            NetworkManager.ConnectionApprovalResponse response)
        {
            var payload = JsonUtility.ToJson(new TicketConsumptionRequest { ticket = ticket });
            using (var request = new UnityWebRequest(_serverApiBaseUrl + "/api/v1/server/realtime-join-tickets/consume", "POST"))
            {
                request.timeout = 12;
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("X-Harborlight-Server-Key", _serverSecret);
                yield return request.SendWebRequest();

                var consumed = request.responseCode >= 200 && request.responseCode < 300
                    ? JsonUtility.FromJson<TicketConsumptionResponse>(request.downloadHandler.text)
                    : null;
                if (consumed == null || !Guid.TryParse(consumed.playerId, out var playerId) ||
                    _authenticatedPlayers.ContainsValue(playerId))
                {
                    response.Approved = false;
                    response.Reason = "Harborlight profile authentication failed.";
                    response.Pending = false;
                    yield break;
                }

                _authenticatedPlayers[clientId] = playerId;
                response.Approved = true;
                response.CreatePlayerObject = false;
                response.Reason = null;
                response.Pending = false;
                if (AutomatedSmokeTest) Debug.Log("HL_NET_APPROVAL_ACCEPTED client=" + clientId);
            }
        }

        private IEnumerator LoadMissionProgressAndSpawn(ulong clientId)
        {
            if (!_authenticatedPlayers.TryGetValue(clientId, out var playerId))
            {
                _manager.DisconnectClient(clientId);
                yield break;
            }

            MissionProgressResponse progress = null;
            using (var request = UnityWebRequest.Get(_serverApiBaseUrl + "/api/v1/server/players/" + playerId.ToString("D") + "/mission-progress"))
            {
                request.timeout = 12;
                request.SetRequestHeader("X-Harborlight-Server-Key", _serverSecret);
                yield return request.SendWebRequest();
                if (request.responseCode >= 200 && request.responseCode < 300)
                    progress = JsonUtility.FromJson<MissionProgressResponse>(request.downloadHandler.text);
            }

            if (!IsValidMissionProgress(progress, playerId))
            {
                Debug.LogWarning("Harborlight could not load authenticated player mission progress; disconnecting the session.");
                _authenticatedPlayers.Remove(clientId);
                _manager.DisconnectClient(clientId);
                yield break;
            }

            progress.message = "Online progress loaded from your Harborlight profile.";
            SpawnAuthenticatedPlayer(clientId, progress);
        }

        private void SpawnAuthenticatedPlayer(ulong clientId, MissionProgressResponse progress)
        {
            if (!_authenticatedPlayers.TryGetValue(clientId, out var playerId) ||
                !_manager.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject != null)
                return;

            var playerObject = Instantiate(_playerPrefab);
            var identity = playerObject.GetComponent<HarborlightNetworkPlayer>();
            var networkObject = playerObject.GetComponent<NetworkObject>();
            if (identity == null || networkObject == null)
            {
                Destroy(playerObject);
                _manager.DisconnectClient(clientId);
                return;
            }

            identity.BindAuthenticatedPlayerId(playerId);
            // Player avatars begin hidden from other clients. The server reveals the owner's object
            // and nearby avatars in RefreshPlayerInterest, avoiding a world-wide spawn burst.
            networkObject.SpawnWithObservers = false;
            networkObject.SpawnAsPlayerObject(clientId, destroyWithScene: false);
            identity.ApplyAuthoritativeMissionSnapshot(progress.beaconStage, progress.courierStage,
                progress.inspectionStage, progress.credits, progress.reputation, progress.deliveriesCompleted,
                "session-loaded", progress.message);
            RefreshPlayerInterest();
            if (AutomatedSmokeTest) Debug.Log("HL_NET_IDENTITY_BOUND client=" + clientId);
        }

        private void RefreshPlayerInterest()
        {
            if (_manager == null || !_manager.IsServer || !_manager.IsListening) return;

            foreach (var cell in _interestCells.Values)
            {
                cell.Clear();
                _interestCellPool.Push(cell);
            }
            _interestCells.Clear();
            _interestPlayers.Clear();

            foreach (var client in _manager.ConnectedClientsList)
            {
                if (client.PlayerObject == null) continue;
                var player = client.PlayerObject.GetComponent<HarborlightNetworkPlayer>();
                if (player == null || !player.IsSpawned) continue;

                _interestPlayers[client.ClientId] = player;
                var cellKey = InterestCell(player.transform.position);
                if (!_interestCells.TryGetValue(cellKey, out var playersInCell))
                {
                    playersInCell = _interestCellPool.Count > 0
                        ? _interestCellPool.Pop()
                        : new List<HarborlightNetworkPlayer>();
                    _interestCells.Add(cellKey, playersInCell);
                }
                playersInCell.Add(player);
            }

            var showDistanceSquared = PlayerShowDistance * PlayerShowDistance;
            var hideDistanceSquared = PlayerHideDistance * PlayerHideDistance;
            var serverClientId = NetworkManager.ServerClientId;

            foreach (var subject in _interestPlayers.Values)
            {
                var networkObject = subject.NetworkObject;
                if (networkObject == null || !networkObject.IsSpawned) continue;

                ShowPlayerTo(networkObject, subject.OwnerClientId);
                if (_manager.IsHost) ShowPlayerTo(networkObject, serverClientId);

                _interestHideClients.Clear();
                var observers = networkObject.GetObservers();
                while (observers.MoveNext())
                {
                    var observerId = observers.Current;
                    if (observerId == subject.OwnerClientId || observerId == serverClientId ||
                        !_interestPlayers.TryGetValue(observerId, out var observer))
                        continue;

                    if (PlanarDistanceSquared(subject.transform.position, observer.transform.position) > hideDistanceSquared)
                        _interestHideClients.Add(observerId);
                }
                foreach (var observerId in _interestHideClients)
                {
                    if (AutomatedInterestSmokeTransitions &&
                        _interestPlayers.TryGetValue(observerId, out var observer))
                        Debug.Log("HL_NET_INTEREST_HIDE owner=" + subject.OwnerClientId +
                            " observer=" + observerId + " distance=" +
                            Mathf.Sqrt(PlanarDistanceSquared(subject.transform.position, observer.transform.position)).ToString("F1") +
                            " elapsed=" + (Time.realtimeSinceStartup - _smokeTestStartedAt).ToString("F1"));
                    networkObject.NetworkHide(observerId);
                }

                var centerCell = InterestCell(subject.transform.position);
                for (var x = -PlayerInterestCellRadius; x <= PlayerInterestCellRadius; x++)
                for (var z = -PlayerInterestCellRadius; z <= PlayerInterestCellRadius; z++)
                {
                    if (!_interestCells.TryGetValue(centerCell + new Vector2Int(x, z), out var candidates)) continue;
                    foreach (var candidate in candidates)
                    {
                        if (candidate.OwnerClientId == subject.OwnerClientId ||
                            PlanarDistanceSquared(subject.transform.position, candidate.transform.position) > showDistanceSquared)
                            continue;

                        if (!networkObject.IsNetworkVisibleTo(candidate.OwnerClientId) &&
                            AutomatedInterestSmokeTransitions)
                            Debug.Log("HL_NET_INTEREST_SHOW owner=" + subject.OwnerClientId +
                                " observer=" + candidate.OwnerClientId + " elapsed=" +
                                (Time.realtimeSinceStartup - _smokeTestStartedAt).ToString("F1"));
                        ShowPlayerTo(networkObject, candidate.OwnerClientId);
                    }
                }
            }
        }

        private void UpdateInterestSmokeTransitions()
        {
            var connectedPlayers = 0;
            foreach (var client in _manager.ConnectedClientsList)
                if (client.PlayerObject != null && client.PlayerObject.GetComponent<HarborlightNetworkPlayer>() != null)
                    connectedPlayers++;
            if (connectedPlayers < 2) return;

            if (_interestSmokeTransitionsStartedAt <= 0f)
            {
                _interestSmokeTransitionsStartedAt = Time.realtimeSinceStartup;
                _interestSmokeTransitionPhase = 0;
                Debug.Log("HL_NET_INTEREST_TRANSITION phase=initial separation=" + PlayerShowDistance +
                    " elapsed=" + (Time.realtimeSinceStartup - _smokeTestStartedAt).ToString("F1"));
                return;
            }

            var elapsed = Time.realtimeSinceStartup - _interestSmokeTransitionsStartedAt;
            var phase = elapsed >= 10f ? 2 : elapsed >= 5f ? 1 : 0;
            if (phase == _interestSmokeTransitionPhase) return;

            var separation = phase == 1 ? PlayerHideDistance + 50f : PlayerShowDistance;
            foreach (var client in _manager.ConnectedClientsList)
            {
                var player = client.PlayerObject != null
                    ? client.PlayerObject.GetComponent<HarborlightNetworkPlayer>()
                    : null;
                if (player == null) continue;

                var z = -1200f;
                var x = player.OwnerClientId % 2 == 1 ? -separation * 0.5f : separation * 0.5f;
                var controller = player.GetComponent<CharacterController>();
                if (controller != null) controller.enabled = false;
                player.transform.SetPositionAndRotation(
                    new Vector3(x, HarborlightDistrictSurfaceNetwork.TerrainHeight(z) + 0.02f, z),
                    Quaternion.identity);
                if (controller != null) controller.enabled = true;
            }

            _interestSmokeTransitionPhase = phase;
            Debug.Log("HL_NET_INTEREST_TRANSITION phase=" + (phase == 1 ? "hidden" : "reshown") +
                " separation=" + separation + " elapsed=" +
                (Time.realtimeSinceStartup - _smokeTestStartedAt).ToString("F1"));
        }

        private void RecordInterestSmokeTransitionEvidence()
        {
            if (_manager == null || !_manager.IsListening || !_manager.IsClient || _manager.IsServer) return;

            var elapsed = Time.realtimeSinceStartup - _smokeTestStartedAt;
            var scheduled = _interestSmokeAssertionCount == 0 ? 4f
                : _interestSmokeAssertionCount == 1 ? 9f : _interestSmokeAssertionCount == 2 ? 15f : float.MaxValue;
            if (elapsed < scheduled) return;

            var visiblePlayers = 0;
            foreach (var player in FindObjectsByType<HarborlightNetworkPlayer>())
                if (player != null && player.IsSpawned) visiblePlayers++;

            var phase = _interestSmokeAssertionCount == 0 ? "initial"
                : _interestSmokeAssertionCount == 1 ? "hidden" : "reshown";
            var expectedPlayers = _interestSmokeAssertionCount == 1 ? 1 : 2;
            var result = visiblePlayers == expectedPlayers ? "PASS" : "FAIL";
            Debug.Log("HL_NET_INTEREST_ASSERT client=" + _manager.LocalClientId +
                " phase=" + phase + " visible=" + visiblePlayers +
                " expected=" + expectedPlayers + " result=" + result +
                " elapsed=" + elapsed.ToString("F1"));
            _interestSmokeAssertionCount++;
        }

        private static void ShowPlayerTo(NetworkObject networkObject, ulong clientId)
        {
            if (networkObject != null && networkObject.IsSpawned && !networkObject.IsNetworkVisibleTo(clientId))
                networkObject.NetworkShow(clientId);
        }

        private static Vector2Int InterestCell(Vector3 position)
            => new Vector2Int(Mathf.FloorToInt(position.x / PlayerInterestCellSize),
                Mathf.FloorToInt(position.z / PlayerInterestCellSize));

        private static float PlanarDistanceSquared(Vector3 left, Vector3 right)
        {
            var x = left.x - right.x;
            var z = left.z - right.z;
            return x * x + z * z;
        }

        public void SubmitMissionAction(HarborlightNetworkPlayer player, WorldAction action)
        {
            if (_manager == null || !_manager.IsServer || player == null ||
                !player.TryGetAuthenticatedPlayerId(out var playerId))
                return;

            if (!IsMissionAction(action))
            {
                player.ReportServerActionMessage("This activity is not connected to online progression yet.");
                return;
            }

            if (!_pendingMissionActions.Add(player.OwnerClientId))
            {
                player.ReportServerActionMessage("Your previous mission action is still syncing. Try again shortly.");
                return;
            }

            StartCoroutine(SubmitMissionActionRoutine(player, player.OwnerClientId, playerId, action, Guid.NewGuid()));
        }

        private IEnumerator SubmitMissionActionRoutine(HarborlightNetworkPlayer player, ulong clientId,
            Guid playerId, WorldAction action, Guid requestId)
        {
            var payload = JsonUtility.ToJson(new MissionActionRequest
            {
                requestId = requestId.ToString("D"),
                action = action.ToString()
            });
            MissionProgressResponse response = null;
            using (var request = new UnityWebRequest(_serverApiBaseUrl + "/api/v1/server/players/" + playerId.ToString("D") + "/mission-actions", "POST"))
            {
                request.timeout = 12;
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("X-Harborlight-Server-Key", _serverSecret);
                yield return request.SendWebRequest();
                if (request.responseCode >= 200 && request.responseCode < 300)
                    response = JsonUtility.FromJson<MissionProgressResponse>(request.downloadHandler.text);
                else
                    Debug.LogWarning("Harborlight mission action failed (HTTP " + request.responseCode + ").");
            }

            _pendingMissionActions.Remove(clientId);
            if (!IsValidMissionProgress(response, playerId) || response.requestId != requestId.ToString("D") ||
                !string.Equals(response.action, action.ToString(), StringComparison.Ordinal))
            {
                if (player != null && player.IsSpawned)
                    player.ReportServerActionMessage("Could not save that mission action. Please try again.");
                yield break;
            }

            if (player != null && player.IsSpawned)
                player.ApplyAuthoritativeMissionSnapshot(response.beaconStage, response.courierStage,
                    response.inspectionStage, response.credits, response.reputation, response.deliveriesCompleted,
                    response.action, response.message);
            if (AutomatedSmokeTest)
                Debug.Log("HL_NET_MISSION_ACTION player=" + playerId.ToString("D") + " action=" + response.action +
                    " accepted=" + response.accepted + " replayed=" + response.replayed + " credits=" + response.credits);
        }

        public void SubmitPoliceDispatchResolution(HarborlightNetworkPlayer player, string dispatchCode)
        {
            if (_manager == null || !_manager.IsServer || player == null ||
                !player.TryGetAuthenticatedPlayerId(out var playerId) || !IsPoliceDispatchCode(dispatchCode))
                return;
            if (!_pendingMissionActions.Add(player.OwnerClientId))
            {
                player.ReportServerActionMessage("Your previous activity is still syncing. Try again shortly.");
                return;
            }
            StartCoroutine(SubmitPoliceDispatchResolutionRoutine(player, player.OwnerClientId, playerId, dispatchCode,
                Guid.NewGuid()));
        }

        private IEnumerator SubmitPoliceDispatchResolutionRoutine(HarborlightNetworkPlayer player, ulong clientId,
            Guid playerId, string dispatchCode, Guid requestId)
        {
            var payload = JsonUtility.ToJson(new PoliceDispatchResolutionRequest
            {
                requestId = requestId.ToString("D"),
                dispatchCode = dispatchCode
            });
            PoliceDispatchResolutionResponse response = null;
            using (var request = new UnityWebRequest(_serverApiBaseUrl + "/api/v1/server/players/" +
                playerId.ToString("D") + "/police-dispatches/resolve", "POST"))
            {
                request.timeout = 12;
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("X-Harborlight-Server-Key", _serverSecret);
                yield return request.SendWebRequest();
                if (request.responseCode >= 200 && request.responseCode < 300)
                    response = JsonUtility.FromJson<PoliceDispatchResolutionResponse>(request.downloadHandler.text);
                else
                    Debug.LogWarning("Harborlight police dispatch failed (HTTP " + request.responseCode + ").");
            }

            _pendingMissionActions.Remove(clientId);
            if (!IsValidPoliceDispatchResolution(response, playerId, requestId, dispatchCode))
            {
                if (player != null && player.IsSpawned)
                    player.ReportServerActionMessage("Could not save that dispatch. Confirm it is still active and try again.");
                yield break;
            }

            if (player != null && player.IsSpawned)
                player.ApplyPoliceDispatchResult(response.credits, response.message);
        }

        private static bool IsValidPoliceDispatchResolution(PoliceDispatchResolutionResponse response, Guid playerId,
            Guid requestId, string dispatchCode)
            => response != null && response.accepted && response.credits >= 0 && response.credits <= 999999 &&
                response.callsCompleted >= 0 && response.callsCompleted <= 999999 &&
                Guid.TryParse(response.playerId, out var responsePlayerId) && responsePlayerId == playerId &&
                Guid.TryParse(response.requestId, out var responseRequestId) && responseRequestId == requestId &&
                response.dispatchCode == dispatchCode && response.career != null &&
                response.career.playerId == playerId.ToString("D") &&
                response.career.callsCompleted == response.callsCompleted &&
                string.IsNullOrEmpty(response.career.activeDispatchCode);

        private static bool IsPoliceDispatchCode(string dispatchCode)
            => dispatchCode is "beacon-safety" or "market-access" or "pier-circuit";

        private static bool IsValidMissionProgress(MissionProgressResponse progress, Guid playerId)
            => progress != null && Guid.TryParse(progress.playerId, out var responsePlayerId) && responsePlayerId == playerId &&
                progress.beaconStage >= 0 && progress.beaconStage <= 2 &&
                progress.courierStage >= 0 && progress.courierStage <= 3 &&
                progress.inspectionStage >= 0 && progress.inspectionStage <= 3 &&
                progress.credits >= 0 && progress.credits <= 999999 &&
                progress.reputation >= 0 && progress.reputation <= 999 &&
                progress.deliveriesCompleted >= 0 && progress.deliveriesCompleted <= 999999;

        private static bool IsMissionAction(WorldAction action)
            => action is WorldAction.AcceptBeaconRepair or WorldAction.RepairBeacon
                or WorldAction.AcceptCourierDelivery or WorldAction.CollectCourierParcel or WorldAction.DeliverCourierParcel
                or WorldAction.AcceptDockInspection or WorldAction.InspectDock or WorldAction.ReportDockInspection;

        private static bool TryNormalizeApiBaseUrl(string value, out string baseUrl)
        {
            baseUrl = "";
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
                !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) ||
                !string.IsNullOrEmpty(uri.Fragment) ||
                (uri.Scheme != Uri.UriSchemeHttps && !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)))
                return false;
            baseUrl = uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
            return true;
        }

        private static bool IsLoopbackHost(string value)
            => string.Equals(value, "localhost", StringComparison.OrdinalIgnoreCase) || IsLoopbackAddress(value);

        private static bool IsLoopbackAddress(string value)
            => IPAddress.TryParse(value, out var address) && IPAddress.IsLoopback(address);

        [Serializable] private sealed class TicketConsumptionRequest { public string ticket; }
        [Serializable] private sealed class TicketConsumptionResponse { public string playerId; }
        [Serializable] private sealed class MissionActionRequest { public string requestId; public string action; }
        [Serializable] private sealed class PoliceDispatchResolutionRequest
        {
            public string requestId;
            public string dispatchCode;
        }
        [Serializable] private sealed class PoliceDispatchResolutionResponse
        {
            public string playerId;
            public string requestId;
            public string dispatchCode;
            public bool accepted;
            public bool replayed;
            public string message;
            public int credits;
            public int callsCompleted;
            public HarborlightBackendClient.PoliceCareerSnapshot career;
        }
        [Serializable] private sealed class MissionProgressResponse
        {
            public string playerId;
            public string requestId;
            public string action;
            public bool accepted;
            public bool replayed;
            public string message;
            public int beaconStage;
            public int courierStage;
            public int inspectionStage;
            public int credits;
            public int reputation;
            public int deliveriesCompleted;
            public string updatedAtUtc;
        }

        private void ScheduleReconnect()
        {
            if (!_isClientSession || _intentionalShutdown || _reconnectPending) return;

            _reconnectAttempt++;
            var delay = Mathf.Min(MaximumReconnectDelaySeconds, Mathf.Pow(2f, _reconnectAttempt - 1));
            _reconnectAt = Time.realtimeSinceStartup + delay;
            _reconnectPending = true;
            var message = "Harborlight client will retry connection " + _reconnectAttempt + " in " + delay + " seconds.";
            Debug.LogWarning(message);
            if (AutomatedSmokeTest) Debug.Log("HL_NET_RECONNECT_SCHEDULED attempt=" + _reconnectAttempt + " delay=" + delay);
        }

        private void OnDestroy()
        {
            _intentionalShutdown = true;
            if (_manager != null && _manager.IsListening) _manager.Shutdown();
            _serverTicketPrivateKey?.Dispose();
        }

        private static void SetInvalid(LaunchOptions options, string message)
        {
            options.IsValid = false;
            options.Error = message;
        }
    }
}
