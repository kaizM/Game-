using Meridian.GameAdmin;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Harborlight
{
    public sealed class HarborlightBootstrap : MonoBehaviour
    {
        public static HarborlightBootstrap Current { get; private set; }

        private Material _ground;
        private Material _road;
        private Material _marking;
        private Material _sidewalk;
        private Material _shore;
        private Material _pavement;
        private Material _brick;
        private Material _paint;
        private Material _window;
        private Material _glass;
        private Material _accent;
        private Material _water;
        private Material _foliage;
        private Material _dark;
        private Material _alloy;
        private Material _headlamp;
        private Material _taillamp;
        private Material _skin;
        private Material _jacket;
        private Material _trousers;
        private Material _bark;
        private Material _parcel;
        private Material _rubber;
        private readonly List<HarborlightVehicle> _vehicles = new List<HarborlightVehicle>();
        private readonly List<OuterBuildingStreamSite> _outerBuildingSites = new List<OuterBuildingStreamSite>();
        private HarborlightGameState _state;
        private HarborlightPlayer _player;
        private HarborlightPlayer _offlinePlayer;
        private HarborlightCamera _cameraFollow;
        private HarborlightHud _hud;
        private HarborlightGarageService _garage;
        private Transform _localStreamingTarget;
        private string _qaGameCapturePath;
        private float _nextOuterBuildingStreamUpdate;
        private int _loadedOuterBuildingCount;
        private bool _networkClientMode;
        private HarborlightNetworkSession.LaunchOptions _networkLaunchOptions;
        private static readonly string[] PackVehicleModels =
        {
            "Taxi", "NormalCar1", "", "NormalCar2", "SUV", "SportsCar", "SportsCar2", ""
        };

        private static Material _runtimeMaterialTemplate;
        private static Mesh _alloyWheelFaceMesh;
        private const float OuterBuildingLoadRadius = 450f;
        private const float OuterBuildingUnloadRadius = 560f;
        private const float BuildingFoundationOverhang = 0.12f;

        private sealed class OuterBuildingStreamSite
        {
            public string ResourcePath;
            public string Name;
            public Vector3 GroundPosition;
            public Vector3 Size;
            public Material FallbackMaterial;
            public float Yaw;
            public GameObject VisualRoot;
        }

        private void Awake()
        {
            Current = this;
            if (Application.isMobilePlatform)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 60;
            }
            CreateMaterials();
            BuildDistrict();

            _networkClientMode = HarborlightNetworkSession.TryReadLaunchOptions(out _networkLaunchOptions);
            if (_networkClientMode && _networkLaunchOptions.IsServer) return;

            BuildPlayerAndCamera();
        }

        private void Start()
        {
            if (_networkClientMode) HarborlightNetworkSession.Start(_networkLaunchOptions);
            if (!Debug.isDebugBuild) return;
            var arguments = Environment.GetCommandLineArgs();
            var mapLayoutSmokeRequested = Array.IndexOf(arguments, "-harborlight-smoke-test-map-layout") >= 0;
            var inventorySmokeRequested = Array.IndexOf(arguments, "-harborlight-smoke-test-inventory") >= 0;
            if (mapLayoutSmokeRequested)
            {
                if (_state != null && _state.TutorialOpen) _state.CompleteTutorial();
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 30;
                Application.runInBackground = true;
                StartCoroutine(RunMapLayoutSmoke());
            }

            if (inventorySmokeRequested)
            {
                if (_state != null && _state.TutorialOpen) _state.CompleteTutorial();
                _state?.OpenInventory();
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 30;
                Application.runInBackground = true;
                StartCoroutine(RunInventorySmoke());
            }

            var cityCaptureArgument = Array.IndexOf(arguments, "-harborlight-capture-loaded-city-only");
            var mapCaptureArgument = Array.IndexOf(arguments, "-harborlight-capture-expanded-map-only");
            var inventoryCaptureArgument = Array.IndexOf(arguments, "-harborlight-capture-inventory-ui-only");
            var captureArgument = inventoryCaptureArgument >= 0
                ? inventoryCaptureArgument
                : mapCaptureArgument >= 0
                    ? mapCaptureArgument
                    : cityCaptureArgument >= 0
                    ? cityCaptureArgument
                    : Array.IndexOf(arguments, "-harborlight-capture-game-only");
            if (captureArgument < 0) return;
            if (captureArgument + 1 >= arguments.Length || string.IsNullOrWhiteSpace(arguments[captureArgument + 1]))
            {
                Debug.LogWarning("HL_QA_GAME_CAPTURE_PATH_MISSING");
                return;
            }

            _qaGameCapturePath = arguments[captureArgument + 1];
            if (inventoryCaptureArgument >= 0)
            {
                if (!inventorySmokeRequested)
                    Debug.LogWarning("HL_QA_INVENTORY_CAPTURE_REQUIRES_SMOKE");
                return;
            }
            if (cityCaptureArgument >= 0)
            {
                if (_state != null && _state.TutorialOpen) _state.CompleteTutorial();
                Application.runInBackground = true;
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 30;
                if (_player != null)
                {
                    var controller = _player.GetComponent<CharacterController>();
                    if (controller != null) controller.enabled = false;
                    var qaPosition = new Vector3(-15f,
                        HarborlightDistrictSurfaceNetwork.TerrainHeight(-450f) + 0.02f, -450f);
                    _player.transform.SetPositionAndRotation(qaPosition, Quaternion.identity);
                    if (controller != null) controller.enabled = true;
                }
                Physics.SyncTransforms();
                StartCoroutine(CaptureLoadedCityAfterStreaming());
                return;
            }

            if (mapCaptureArgument >= 0)
            {
                if (_state != null && _state.TutorialOpen) _state.CompleteTutorial();
                _hud?.SetExpandedDistrictMapForQaCapture();
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 30;
                StartCoroutine(CaptureExpandedMapAfterRenderedFrame());
                return;
            }

            CaptureQaGameScreenshot();
        }

        private IEnumerator RunInventorySmoke()
        {
            var backend = GetComponent<HarborlightBackendClient>();
            var timeout = Time.realtimeSinceStartup + 25f;
            while ((backend == null || !backend.IsConnected) && Time.realtimeSinceStartup < timeout)
                yield return new WaitForSecondsRealtime(0.1f);
            if (backend == null || !backend.IsConnected)
            {
                Debug.LogError("HL_INVENTORY_SMOKE result=FAIL phase=connect status=" +
                    (backend != null ? backend.Status : "backend missing"));
                Application.Quit();
                yield break;
            }

            timeout = Time.realtimeSinceStartup + 20f;
            while (!backend.InventoryLoaded && Time.realtimeSinceStartup < timeout)
                yield return new WaitForSecondsRealtime(0.1f);
            if (!backend.InventoryLoaded || backend.InventoryCatalog.Count == 0 || backend.InventoryCashCredits < 35)
            {
                Debug.LogError("HL_INVENTORY_SMOKE result=FAIL phase=load catalog=" +
                    backend.InventoryCatalog.Count + " cash=" + backend.InventoryCashCredits +
                    " message=" + (backend.InventoryLoaded ? "invalid inventory state" : "request timed out"));
                Application.Quit();
                yield break;
            }
            Debug.Log("HL_INVENTORY_LOAD result=PASS catalog=" + backend.InventoryCatalog.Count +
                " cash=" + backend.InventoryCashCredits + " items=" + backend.InventoryItems.Count);

            const string smokeSku = "dockhand-gloves";
            var owned = FindInventoryItem(backend, smokeSku);
            if (owned != null)
            {
                Debug.LogError("HL_INVENTORY_SMOKE result=FAIL phase=clean-profile sku-already-owned");
                Application.Quit();
                yield break;
            }

            var completed = false;
            var succeeded = false;
            string resultMessage = null;
            backend.PurchaseInventoryItem(smokeSku, (success, message) =>
            {
                succeeded = success;
                resultMessage = message;
                completed = true;
            });
            timeout = Time.realtimeSinceStartup + 20f;
            while (!completed && Time.realtimeSinceStartup < timeout)
                yield return new WaitForSecondsRealtime(0.1f);
            if (!completed || !succeeded)
            {
                Debug.LogError("HL_INVENTORY_SMOKE result=FAIL phase=purchase message=" +
                    (completed ? resultMessage : "request timed out"));
                Application.Quit();
                yield break;
            }

            completed = false;
            succeeded = false;
            resultMessage = null;
            backend.SetInventoryEquipped(smokeSku, true, (success, message) =>
            {
                succeeded = success;
                resultMessage = message;
                completed = true;
            });
            timeout = Time.realtimeSinceStartup + 20f;
            while (!completed && Time.realtimeSinceStartup < timeout)
                yield return new WaitForSecondsRealtime(0.1f);
            owned = FindInventoryItem(backend, smokeSku);
            if (!completed || !succeeded || owned == null || !owned.isEquipped || owned.quantity != 1)
            {
                Debug.LogError("HL_INVENTORY_SMOKE result=FAIL phase=equip owned=" + (owned != null) +
                    " equipped=" + (owned != null && owned.isEquipped) + " message=" +
                    (completed ? resultMessage : "request timed out"));
                Application.Quit();
                yield break;
            }

            Debug.Log("HL_INVENTORY_SMOKE result=PASS catalog=" + backend.InventoryCatalog.Count +
                " cash=" + backend.InventoryCashCredits + " sku=" + owned.sku +
                " quantity=" + owned.quantity + " equipped=" + owned.isEquipped);
            if (!string.IsNullOrWhiteSpace(_qaGameCapturePath))
            {
                yield return null;
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(_qaGameCapturePath, 1);
                Debug.Log("HL_QA_INVENTORY_CAPTURE_REQUESTED");
                yield return new WaitForSecondsRealtime(1.5f);
            }
            Application.Quit();
        }

        private static HarborlightBackendClient.InventoryItem FindInventoryItem(
            HarborlightBackendClient backend, string sku)
        {
            foreach (var item in backend.InventoryItems)
                if (item != null && item.sku == sku) return item;
            return null;
        }

        private IEnumerator RunMapLayoutSmoke()
        {
            yield return new WaitForSecondsRealtime(0.25f);
            Physics.SyncTransforms();

            var passedRoadProbes = 0;
            var totalRoadProbes = 0;
            var failedRoadProbes = new List<string>();
            var outerEastWestRoads = HarborlightDistrictSurfaceNetwork.OuterEastWestRoads;
            var outerNorthSouthRoads = HarborlightDistrictSurfaceNetwork.OuterNorthSouthRoads;
            for (var row = 0; row < outerEastWestRoads.Length; row++)
            for (var column = 0; column < outerNorthSouthRoads.Length; column++)
            {
                var z = outerEastWestRoads[row];
                var x = outerNorthSouthRoads[column];
                if (!HarborlightDistrictSurfaceNetwork.IsOuterEastWestRoadActive(row) ||
                    !HarborlightDistrictSurfaceNetwork.IsOuterNorthSouthRoadActive(column, z)) continue;
                if (!HarborlightDistrictSurfaceNetwork.IsWithinPlayableLand(new Vector2(x, z), 8f)) continue;
                totalRoadProbes++;
                if (ProbeMapSurface(x, z, "Playable district road network", 0.055f))
                    passedRoadProbes++;
                else if (failedRoadProbes.Count < 8)
                    failedRoadProbes.Add(x.ToString("F0") + "," + z.ToString("F0"));
            }

            var passedConnectorRoadProbes = 0;
            var totalConnectorRoadProbes = 0;
            var connectorSamplesInLand = 0;
            var failedConnectorRoadProbes = new List<string>();
            foreach (var route in new[]
                     {
                         HarborlightDistrictSurfaceNetwork.SouthgateConnector,
                         HarborlightDistrictSurfaceNetwork.CedarValeScenicRoute
                     })
            for (var segmentIndex = 0; segmentIndex + 1 < route.Length; segmentIndex++)
            {
                var start = route[segmentIndex];
                var end = route[segmentIndex + 1];
                var sampleCount = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(start, end) / 30f));
                for (var sample = 0; sample <= sampleCount; sample++)
                {
                    var point = Vector2.Lerp(start, end, (float)sample / sampleCount);
                    totalConnectorRoadProbes++;
                    if (!HarborlightDistrictSurfaceNetwork.IsWithinPlayableLand(point, 8f))
                    {
                        if (failedConnectorRoadProbes.Count < 8)
                            failedConnectorRoadProbes.Add("outside land " + point.x.ToString("F0") + "," + point.y.ToString("F0"));
                        continue;
                    }
                    connectorSamplesInLand++;
                    if (ProbeMapSurfaceAmongOverlaps(point.x, point.y, "Playable district road network", 0.095f))
                    {
                        passedConnectorRoadProbes++;
                    }
                    else if (failedConnectorRoadProbes.Count < 8)
                        failedConnectorRoadProbes.Add(point.x.ToString("F0") + "," + point.y.ToString("F0") +
                            " hits=" + DescribeMapSurfaceHits(point.x, point.y));
                }
            }
            for (var column = 0; column < outerNorthSouthRoads.Length; column++)
            {
                if (column % 3 != 1) continue;
                var x = outerNorthSouthRoads[column];
                var startZ = HarborlightDistrictSurfaceNetwork.WorldMinZAtX(x) +
                    HarborlightDistrictSurfaceNetwork.OuterRoadBoundaryClearance;
                if (startZ >= HarborlightDistrictSurfaceNetwork.OuterCityGridSouthZ) continue;
                var sampleCount = Mathf.Max(1, Mathf.CeilToInt(
                    (HarborlightDistrictSurfaceNetwork.OuterCityGridSouthZ - startZ) / 100f));
                for (var sample = 0; sample <= sampleCount; sample++)
                {
                    var z = Mathf.Lerp(startZ, HarborlightDistrictSurfaceNetwork.OuterCityGridSouthZ,
                        (float)sample / sampleCount);
                    var point = new Vector2(x, z);
                    totalConnectorRoadProbes++;
                    if (!HarborlightDistrictSurfaceNetwork.IsWithinPlayableLand(point, 8f))
                    {
                        if (failedConnectorRoadProbes.Count < 8)
                            failedConnectorRoadProbes.Add("outside land " + x.ToString("F0") + "," + z.ToString("F0"));
                        continue;
                    }
                    connectorSamplesInLand++;
                    if (ProbeMapSurfaceAmongOverlaps(x, z, "Playable district road network", 0.055f))
                        passedConnectorRoadProbes++;
                    else if (failedConnectorRoadProbes.Count < 8)
                        failedConnectorRoadProbes.Add(x.ToString("F0") + "," + z.ToString("F0") +
                            " hits=" + DescribeMapSurfaceHits(x, z));
                }
            }
            var collectorStartX = HarborlightDistrictSurfaceNetwork.WorldMinXAtZ(
                HarborlightDistrictSurfaceNetwork.OuterCityGridSouthZ) + 15f;
            var collectorEndX = HarborlightDistrictSurfaceNetwork.WorldMaxXAtZ(
                HarborlightDistrictSurfaceNetwork.OuterCityGridSouthZ) - 15f;
            for (var x = collectorStartX; x <= collectorEndX; x += 50f)
            {
                var z = HarborlightDistrictSurfaceNetwork.OuterCityGridSouthZ;
                var point = new Vector2(x, z);
                totalConnectorRoadProbes++;
                if (!HarborlightDistrictSurfaceNetwork.IsWithinPlayableLand(point, 8f))
                {
                    if (failedConnectorRoadProbes.Count < 8)
                        failedConnectorRoadProbes.Add("outside land " + x.ToString("F0") + "," + z.ToString("F0"));
                    continue;
                }
                connectorSamplesInLand++;
                if (ProbeMapSurfaceAmongOverlaps(x, z, "Playable district road network", 0.055f))
                    passedConnectorRoadProbes++;
                else if (failedConnectorRoadProbes.Count < 8)
                    failedConnectorRoadProbes.Add(x.ToString("F0") + "," + z.ToString("F0") +
                        " hits=" + DescribeMapSurfaceHits(x, z));
            }

            var passedSidewalkProbes = 0;
            var totalSidewalkProbes = 0;
            var failedSidewalkProbes = new List<string>();
            for (var row = 0; row < outerEastWestRoads.Length; row += 3)
            for (var column = 2; column + 1 < outerNorthSouthRoads.Length; column += 5)
            {
                if (!HarborlightDistrictSurfaceNetwork.IsOuterEastWestRoadActive(row)) continue;
                var x = (outerNorthSouthRoads[column] + outerNorthSouthRoads[column + 1]) * 0.5f;
                var roadHalfWidth = row % 5 == 0 ? 8f : 5f;
                var z = outerEastWestRoads[row] + roadHalfWidth + 0.22f + 1.6f;
                if (!HarborlightDistrictSurfaceNetwork.IsWithinPlayableLand(new Vector2(x, z), 2f)) continue;
                totalSidewalkProbes++;
                if (ProbeMapSurface(x, z, "Playable district textured sidewalks", 0.18f))
                    passedSidewalkProbes++;
                else if (failedSidewalkProbes.Count < 8)
                    failedSidewalkProbes.Add(x.ToString("F1") + "," + z.ToString("F1"));
            }
            for (var column = 0; column < outerNorthSouthRoads.Length; column++)
            for (var row = 2; row + 1 < outerEastWestRoads.Length; row += 4)
            {
                var roadHalfWidth = column % 3 == 1 ? 7f : 4.25f;
                var x = outerNorthSouthRoads[column] + roadHalfWidth + 0.22f + 1.6f;
                var z = (outerEastWestRoads[row] + outerEastWestRoads[row + 1]) * 0.5f;
                if (!HarborlightDistrictSurfaceNetwork.IsOuterNorthSouthRoadActive(column, z)) continue;
                if (!HarborlightDistrictSurfaceNetwork.IsWithinPlayableLand(new Vector2(x, z), 2f)) continue;
                totalSidewalkProbes++;
                if (ProbeMapSurface(x, z, "Playable district textured sidewalks", 0.18f))
                    passedSidewalkProbes++;
                else if (failedSidewalkProbes.Count < 8)
                    failedSidewalkProbes.Add(x.ToString("F1") + "," + z.ToString("F1"));
            }

            var playerPosition = _player != null ? _player.transform.position : Vector3.zero;
            var loadedPosition = _state != null ? _state.LoadedPlayerPosition : Vector2.zero;
            var gameplayCamera = Camera.main;
            var cameraFarClip = gameplayCamera != null ? gameplayCamera.farClipPlane : 0f;
            var buildingVisibilityPassed = cameraFarClip >= OuterBuildingUnloadRadius;
            Debug.Log("HL_MAP_BUILDING_VISIBILITY result=" + (buildingVisibilityPassed ? "PASS" : "FAIL") +
                " farClip=" + cameraFarClip.ToString("F0") +
                " loadRadius=" + OuterBuildingLoadRadius.ToString("F0") +
                " unloadRadius=" + OuterBuildingUnloadRadius.ToString("F0"));
            var positionInBounds = _player != null &&
                HarborlightDistrictSurfaceNetwork.IsWithinPlayableLand(
                    new Vector2(playerPosition.x, playerPosition.z), 4f);
            var restoredPositionMatchesSave = _player != null && _state != null &&
                Mathf.Abs(playerPosition.x - loadedPosition.x) <= 0.5f &&
                Mathf.Abs(playerPosition.z - loadedPosition.y) <= 0.5f &&
                _state.LastMessage != "Saved position was obstructed. Returned to Lantern Square.";
            var saveBoundsPassed = positionInBounds && restoredPositionMatchesSave;
            Debug.Log("HL_MAP_SAVE_BOUNDS result=" + (saveBoundsPassed ? "PASS" : "FAIL") +
                " actual=" + playerPosition.x.ToString("F1") + "," + playerPosition.z.ToString("F1") +
                " loaded=" + loadedPosition.x.ToString("F1") + "," + loadedPosition.y.ToString("F1") +
                " message=" + (_state != null ? _state.LastMessage : "no-state"));
            Debug.Log("HL_MAP_ROAD_COLLIDERS result=" + (passedRoadProbes == totalRoadProbes ? "PASS" : "FAIL") +
                " passed=" + passedRoadProbes + "/" + totalRoadProbes +
                " misses=" + string.Join(";", failedRoadProbes));
            Debug.Log("HL_MAP_NEW_ROADS result=" +
                (passedConnectorRoadProbes == totalConnectorRoadProbes ? "PASS" : "FAIL") +
                " passed=" + passedConnectorRoadProbes + "/" + totalConnectorRoadProbes +
                " inLand=" + connectorSamplesInLand + "/" + totalConnectorRoadProbes +
                " misses=" + string.Join(";", failedConnectorRoadProbes));
            Debug.Log("HL_MAP_SIDEWALK_COLLIDERS result=" + (passedSidewalkProbes == totalSidewalkProbes ? "PASS" : "FAIL") +
                " passed=" + passedSidewalkProbes + "/" + totalSidewalkProbes +
                " misses=" + string.Join(";", failedSidewalkProbes));

            var entryPoints = HarborlightDistrictSurfaceNetwork.MultiplayerEntryPoints;
            var spawnSlots = new HashSet<Vector2Int>();
            var allSpawnSlotsInBounds = true;
            var distributedSpawnSlotCount = entryPoints.Length *
                HarborlightDistrictSurfaceNetwork.MultiplayerEntrySlotsPerZone;
            for (var clientOffset = 0; clientOffset < distributedSpawnSlotCount; clientOffset++)
            {
                var entryPoint = entryPoints[clientOffset % entryPoints.Length];
                var entrySlot = clientOffset / entryPoints.Length %
                    HarborlightDistrictSurfaceNetwork.MultiplayerEntrySlotsPerZone;
                var x = entryPoint.x + (entrySlot % 5 - 2) * 3f;
                var z = entryPoint.y + (entrySlot / 5 - 2) * 3f;
                allSpawnSlotsInBounds &= HarborlightDistrictSurfaceNetwork.IsWithinPlayableLand(
                    new Vector2(x, z), 4f);
                spawnSlots.Add(new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(z)));
            }
            var expectedEntryPointCount = HarborlightDistrictSurfaceNetwork.MultiplayerEntryColumns *
                HarborlightDistrictSurfaceNetwork.MultiplayerEntryRows;
            var distributedSpawnsPassed = entryPoints.Length == expectedEntryPointCount &&
                spawnSlots.Count == distributedSpawnSlotCount && allSpawnSlotsInBounds;
            Debug.Log("HL_MAP_MULTIPLAYER_SPAWNS result=" + (distributedSpawnsPassed ? "PASS" : "FAIL") +
                " zones=" + entryPoints.Length +
                " slots=" + distributedSpawnSlotCount +
                " unique=" + spawnSlots.Count +
                " clientRange=8-" + (7 + distributedSpawnSlotCount));

            var passedStreamingWaypoints = 0;
            var passedBuildingGroundChecks = 0;
            var entryColumns = HarborlightDistrictSurfaceNetwork.MultiplayerEntryColumns;
            var entryRows = HarborlightDistrictSurfaceNetwork.MultiplayerEntryRows;
            var waypointIndices = new[]
            {
                0,
                entryColumns - 1,
                (entryRows - 1) * entryColumns,
                entryRows * entryColumns - 1,
                (entryRows / 2) * entryColumns + entryColumns / 2
            };
            if (_player != null) _player.enabled = false;
            foreach (var index in waypointIndices)
            {
                var entry = HarborlightDistrictSurfaceNetwork.MultiplayerEntryPoints[index];
                var position = new Vector3(entry.x,
                    HarborlightDistrictSurfaceNetwork.TerrainHeight(entry.y) + 0.02f, entry.y);
                if (_player != null)
                {
                    var controller = _player.GetComponent<CharacterController>();
                    if (controller != null) controller.enabled = false;
                    _player.transform.SetPositionAndRotation(position, Quaternion.identity);
                    if (controller != null) controller.enabled = true;
                }
                Physics.SyncTransforms();
                yield return new WaitForSecondsRealtime(1.25f);

                var loaded = 0;
                var beyondUnloadRadius = 0;
                OuterBuildingStreamSite nearestLoadedSite = null;
                var nearestLoadedDistanceSquared = float.PositiveInfinity;
                foreach (var site in _outerBuildingSites)
                {
                    if (site.VisualRoot == null) continue;
                    loaded++;
                    var dx = position.x - site.GroundPosition.x;
                    var dz = position.z - site.GroundPosition.z;
                    var distanceSquared = dx * dx + dz * dz;
                    if (distanceSquared > OuterBuildingUnloadRadius * OuterBuildingUnloadRadius)
                        beyondUnloadRadius++;
                    if (distanceSquared < nearestLoadedDistanceSquared)
                    {
                        nearestLoadedSite = site;
                        nearestLoadedDistanceSquared = distanceSquared;
                    }
                }
                var waypointPassed = loaded > 0 && beyondUnloadRadius == 0;
                if (waypointPassed) passedStreamingWaypoints++;
                var foundationTop = 0f;
                var buildingBottom = 0f;
                var highestGroundPoint = 0f;
                var buildingGroundPassed = nearestLoadedSite != null &&
                    ProbeOuterBuildingGroundContact(nearestLoadedSite, out foundationTop, out buildingBottom,
                        out highestGroundPoint);
                if (buildingGroundPassed) passedBuildingGroundChecks++;
                Debug.Log("HL_MAP_BUILDING_GROUND index=" + index + " result=" +
                    (buildingGroundPassed ? "PASS" : "FAIL") +
                    " name=" + (nearestLoadedSite != null ? nearestLoadedSite.Name : "none") +
                    " foundationTop=" + foundationTop.ToString("F3") +
                    " buildingBottom=" + buildingBottom.ToString("F3") +
                    " groundTop=" + highestGroundPoint.ToString("F3"));
                Debug.Log("HL_MAP_STREAM_WAYPOINT index=" + index + " result=" +
                    (waypointPassed ? "PASS" : "FAIL") + " loaded=" + loaded +
                    " beyondUnload=" + beyondUnloadRadius);
            }
            var passed = saveBoundsPassed && passedRoadProbes == totalRoadProbes &&
                         passedConnectorRoadProbes == totalConnectorRoadProbes &&
                         passedSidewalkProbes == totalSidewalkProbes &&
                         passedStreamingWaypoints == waypointIndices.Length &&
                         passedBuildingGroundChecks == waypointIndices.Length &&
                         buildingVisibilityPassed && distributedSpawnsPassed;
            Debug.Log("HL_MAP_LAYOUT_SMOKE result=" + (passed ? "PASS" : "FAIL") +
                " roads=" + passedRoadProbes + "/" + totalRoadProbes +
                " sidewalks=" + passedSidewalkProbes + "/" + totalSidewalkProbes +
                " streamWaypoints=" + passedStreamingWaypoints + "/" + waypointIndices.Length +
                " groundedBuildings=" + passedBuildingGroundChecks + "/" + waypointIndices.Length +
                " multiplayerSpawns=" + spawnSlots.Count + "/" + distributedSpawnSlotCount +
                " buildingSites=" + _outerBuildingSites.Count);
            yield return new WaitForSecondsRealtime(0.25f);
            Application.Quit();
        }

        private static bool ProbeMapSurface(float x, float z, string expectedColliderName, float surfaceOffset)
        {
            var expectedY = HarborlightDistrictSurfaceNetwork.TerrainHeight(z) + surfaceOffset;
            var origin = new Vector3(x, expectedY + 2f, z);
            return Physics.Raycast(origin, Vector3.down, out var hit, 4f,
                       Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                   hit.collider.gameObject.name == expectedColliderName &&
                   Mathf.Abs(hit.point.y - expectedY) <= 0.06f;
        }

        private static bool ProbeMapSurfaceAmongOverlaps(float x, float z, string expectedColliderName,
            float surfaceOffset)
        {
            var expectedY = HarborlightDistrictSurfaceNetwork.TerrainHeight(z) + surfaceOffset;
            var origin = new Vector3(x, expectedY + 2f, z);
            var hits = Physics.RaycastAll(origin, Vector3.down, 4f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
                if (hit.collider.gameObject.name == expectedColliderName &&
                    Mathf.Abs(hit.point.y - expectedY) <= 0.06f)
                    return true;
            return false;
        }

        private static string DescribeMapSurfaceHits(float x, float z)
        {
            var origin = new Vector3(x, HarborlightDistrictSurfaceNetwork.TerrainHeight(z) + 2.1f, z);
            var hits = Physics.RaycastAll(origin, Vector3.down, 4f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0) return "none";
            var descriptions = new List<string>(Mathf.Min(hits.Length, 3));
            foreach (var hit in hits)
            {
                descriptions.Add(hit.collider.gameObject.name + "@" + hit.point.y.ToString("F2"));
                if (descriptions.Count == 3) break;
            }
            return string.Join(",", descriptions);
        }

        private static bool ProbeOuterBuildingGroundContact(OuterBuildingStreamSite site,
            out float foundationTop, out float buildingBottom, out float highestGroundPoint)
        {
            foundationTop = 0f;
            buildingBottom = 0f;
            highestGroundPoint = 0f;
            if (site == null || site.VisualRoot == null) return false;

            var foundation = site.VisualRoot.transform.Find(site.Name + " stone foundation");
            var building = site.VisualRoot.transform.Find(site.Name);
            var foundationRenderer = foundation != null ? foundation.GetComponent<Renderer>() : null;
            if (building == null || foundationRenderer == null) return false;

            var halfX = site.Size.x * 0.5f + BuildingFoundationOverhang;
            var halfZ = site.Size.z * 0.5f + BuildingFoundationOverhang;
            var rotation = Quaternion.Euler(0f, site.Yaw, 0f);
            var minGround = float.PositiveInfinity;
            highestGroundPoint = float.NegativeInfinity;
            foreach (var corner in new[]
                     {
                         new Vector3(-halfX, 0f, -halfZ), new Vector3(halfX, 0f, -halfZ),
                         new Vector3(-halfX, 0f, halfZ), new Vector3(halfX, 0f, halfZ)
                     })
            {
                var z = site.GroundPosition.z + (rotation * corner).z;
                var groundY = HarborlightDistrictSurfaceNetwork.TerrainHeight(z) + 0.02f;
                minGround = Mathf.Min(minGround, groundY);
                highestGroundPoint = Mathf.Max(highestGroundPoint, groundY);
            }

            var buildingRenderers = building.GetComponentsInChildren<Renderer>(true);
            if (buildingRenderers.Length == 0) return false;
            foundationTop = foundationRenderer.bounds.max.y;
            buildingBottom = float.PositiveInfinity;
            foreach (var renderer in buildingRenderers)
                buildingBottom = Mathf.Min(buildingBottom, renderer.bounds.min.y);

            return Mathf.Abs(foundationTop - (highestGroundPoint + 0.005f)) <= 0.01f &&
                   foundationRenderer.bounds.min.y <= minGround - 0.05f &&
                   Mathf.Abs(buildingBottom - foundationTop) <= 0.01f;
        }

        private IEnumerator CaptureExpandedMapAfterRenderedFrame()
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(_qaGameCapturePath, 1);
            Debug.Log("HL_QA_EXPANDED_MAP_CAPTURE_REQUESTED");
        }

        private IEnumerator CaptureLoadedCityAfterStreaming()
        {
            var timeout = Time.realtimeSinceStartup + 8f;
            while (_loadedOuterBuildingCount < 5 && Time.realtimeSinceStartup < timeout)
                yield return new WaitForSecondsRealtime(0.25f);
            yield return null;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(_qaGameCapturePath, 1);
            Debug.Log("HL_QA_LOADED_CITY_CAPTURE_REQUESTED loaded=" + _loadedOuterBuildingCount +
                " player=" + (_player != null ? _player.transform.position.ToString("F1") : "none"));
        }

        private void CaptureQaGameScreenshot()
        {
            ScreenCapture.CaptureScreenshot(_qaGameCapturePath, 1);
            Debug.Log("HL_QA_GAME_CAPTURE_REQUESTED");
        }

        private void Update()
        {
            if (_localStreamingTarget == null || Time.unscaledTime < _nextOuterBuildingStreamUpdate) return;
            _nextOuterBuildingStreamUpdate = Time.unscaledTime + 0.5f;

            var viewer = _localStreamingTarget.position;
            var loadDistanceSquared = OuterBuildingLoadRadius * OuterBuildingLoadRadius;
            var unloadDistanceSquared = OuterBuildingUnloadRadius * OuterBuildingUnloadRadius;

            var loadsRemaining = 2;
            while (loadsRemaining > 0)
            {
                OuterBuildingStreamSite nearest = null;
                var nearestDistanceSquared = loadDistanceSquared;
                foreach (var site in _outerBuildingSites)
                {
                    if (site.VisualRoot != null) continue;
                    var offsetX = viewer.x - site.GroundPosition.x;
                    var offsetZ = viewer.z - site.GroundPosition.z;
                    var distanceSquared = offsetX * offsetX + offsetZ * offsetZ;
                    if (distanceSquared < nearestDistanceSquared)
                    {
                        nearest = site;
                        nearestDistanceSquared = distanceSquared;
                    }
                }
                if (nearest == null) break;
                LoadOuterBuilding(nearest);
                loadsRemaining--;
            }

            foreach (var site in _outerBuildingSites)
            {
                var offsetX = viewer.x - site.GroundPosition.x;
                var offsetZ = viewer.z - site.GroundPosition.z;
                var distanceSquared = offsetX * offsetX + offsetZ * offsetZ;
                if (site.VisualRoot != null && distanceSquared > unloadDistanceSquared)
                    UnloadOuterBuilding(site);
            }
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private void CreateMaterials()
        {
            _ground = Material("Coastal park grass", new Color(0.70f, 0.75f, 0.63f));
            _road = Material("North Pier asphalt", Color.white);
            _marking = Material("Weathered road paint", new Color(0.92f, 0.84f, 0.66f));
            _sidewalk = Material("Warm concrete", Color.white);
            _shore = Material("Harbor sand and gravel", Color.white);
            _pavement = Material("North Pier stone pavers", Color.white);
            _brick = Material("Harbor brick", Color.white);
            _paint = Material("Market plaster", new Color(0.62f, 0.55f, 0.43f));
            _window = Material("Lit storefront glass", new Color(0.56f, 0.70f, 0.72f));
            _glass = Material("Vehicle glass", new Color(0.16f, 0.27f, 0.30f));
            _accent = Material("Beacon brass", new Color(0.83f, 0.52f, 0.24f));
            _water = Material("Harbor water", Color.white);
            _foliage = Material("Coastal trees", new Color(0.17f, 0.29f, 0.22f));
            _dark = Material("Graphite trim", new Color(0.075f, 0.09f, 0.10f));
            _alloy = Material("Satin alloy wheel faces", new Color(0.48f, 0.52f, 0.55f));
            _headlamp = Material("Warm white headlamp lenses", new Color(0.92f, 0.88f, 0.72f));
            _taillamp = Material("Ruby rear lamp lenses", new Color(0.58f, 0.055f, 0.035f));
            _skin = Material("Warm skin", new Color(0.68f, 0.46f, 0.34f));
            _jacket = Material("Deep teal jacket", new Color(0.10f, 0.32f, 0.34f));
            _trousers = Material("Charcoal trousers", new Color(0.10f, 0.12f, 0.15f));
            _bark = Material("Salt-weathered bark", new Color(0.26f, 0.20f, 0.15f));
            _parcel = Material("Kraft courier parcel", new Color(0.70f, 0.52f, 0.30f));
            _rubber = Material("Road tire rubber", new Color(0.045f, 0.05f, 0.055f));

            ApplyTexture(_ground, "Art/Textures/Harborlight-Grass", new Vector2(40f, 40f), 0.12f, 73);
            ApplyTexture(_road, "Art/Textures/Harborlight-Asphalt", new Vector2(24f, 3.5f), 0.14f, 74);
            ApplyTexture(_sidewalk, "Art/Textures/Harborlight-Concrete", new Vector2(18f, 3f), 0.11f, 41);
            ApplyTexture(_brick, "Art/Textures/Harborlight-Brick", new Vector2(6f, 4f), 0.08f, 91);
            ApplyTexture(_water, "Art/Textures/Harborlight-Water", Vector2.one, 0.10f, 17);
            _paint.mainTexture = NoiseTexture(0.06f, 27);
            _paint.mainTextureScale = new Vector2(3f, 3f);

            ApplyPbrTexture(_road, "asphalt_03_diff_4k", "asphalt_03_nor_dx_4k", new Vector2(42f, 5f), 0.55f);
            ApplyPbrTexture(_brick, "brick_wall_001_diffuse_4k", "brick_wall_001_nor_dx_4k", new Vector2(6f, 4f), 0.7f);
            ApplyPbrTexture(_sidewalk, "concrete_wall_009_diff_4k", "concrete_wall_009_nor_dx_4k", new Vector2(36f, 1.2f), 0.5f);
            ApplyPbrTexture(_paint, "white_stucco_02_diff_4k", "white_stucco_02_nor_dx_4k", new Vector2(8f, 5f), 0.35f);
            ApplyPbrTexture(_ground, "grass_ground_diff_2k", "grass_ground_nor_gl_2k", Vector2.one, 0.32f);
            ApplyPbrTexture(_shore, "coast_sand_05_diff_2k", "coast_sand_05_nor_gl_2k", Vector2.one, 0.45f);
            ApplyPbrTexture(_pavement, "patterned_paving_diff_4k", "patterned_paving_nor_gl_4k", new Vector2(1.25f, 1.25f), 0.3f);

            SetSurface(_ground, 0f, 0.08f);
            SetSurface(_road, 0f, 0.12f);
            SetSurface(_marking, 0f, 0.12f);
            SetSurface(_sidewalk, 0f, 0.16f);
            SetSurface(_shore, 0f, 0.04f);
            SetSurface(_pavement, 0f, 0.12f);
            SetSurface(_brick, 0f, 0.14f);
            SetSurface(_paint, 0f, 0.18f);
            SetSurface(_window, 0.08f, 0.82f);
            SetSurface(_glass, 0.08f, 0.9f);
            SetSurface(_water, 0.12f, 0.72f);
            SetSurface(_foliage, 0f, 0.12f);
            SetSurface(_dark, 0.12f, 0.24f);
            SetSurface(_alloy, 0.76f, 0.86f);
            SetSurface(_headlamp, 0.18f, 0.82f);
            SetSurface(_taillamp, 0.08f, 0.72f);
            SetEmission(_headlamp, new Color(1f, 0.82f, 0.49f), 0.45f);
            SetEmission(_taillamp, new Color(0.55f, 0.018f, 0.008f), 0.35f);
            SetSurface(_bark, 0f, 0.1f);
            SetSurface(_parcel, 0f, 0.12f);
            SetSurface(_rubber, 0f, 0.12f);
        }

        private void BuildDistrict()
        {
            BuildCoastAndTerrain();

            Block("Lantern Square road", new Vector3(0f, 0.015f, -1f), new Vector3(88f, 0.05f, 10f), _road);
            Block("Foundry Row road", new Vector3(18f, 0.025f, 2f), new Vector3(8f, 0.05f, 72f), _road);
            Block("West market road", new Vector3(-20f, 0.025f, -2f), new Vector3(7f, 0.05f, 61f), _road);
            BuildStreetDetails();
            HarborlightDistrictSurfaceNetwork.Build(transform, _road, _pavement, _marking,
                HarborlightDistrictSurfaceNetwork.TerrainHeight);
            var northPier = new Vector3(0f, 0.08f, 34f);
            var northPierSize = new Vector3(30f, 0.25f, 12f);
            Block("North pier walk", northPier, northPierSize, _sidewalk);
            CreateTiledTopSurface("North pier stone pavers", northPier, northPierSize, _pavement);
            var eastPier = new Vector3(13f, 0.08f, 38f);
            var eastPierSize = new Vector3(8f, 0.25f, 20f);
            Block("East pier walk", eastPier, eastPierSize, _sidewalk);
            CreateTiledTopSurface("East pier stone pavers", eastPier, eastPierSize, _pavement);
            CreatePierPiles();
            CreateNorthPierPromenade();

            PlaceModularBuilding("Art/Buildings/HarborlightApartmentBlock", "Tideglass House", new Vector3(-31f, 0f, -24f), new Vector3(14f, 8f, 13f), _paint, 90f);
            PlaceModularBuilding("Art/Buildings/HarborlightApartmentBlock", "The Net Loft", new Vector3(-10f, 0f, -25f), new Vector3(12f, 6.4f, 14f), _brick, 0f);
            PlaceModularBuilding("Art/Buildings/HarborlightFoundryBlock", "Foundry Workshop", new Vector3(30f, 0f, -22f), new Vector3(15f, 8.6f, 15f), _brick, 0f);
            PlaceModularBuilding("Art/Buildings/HarborlightApartmentBlock", "Beacon Cafe", new Vector3(-32f, 0f, 11f), new Vector3(14f, 6.4f, 13f), _brick, 90f);
            PlaceModularBuilding("Art/Buildings/HarborlightFoundryBlock", "Sailmaker Co-op", new Vector3(-9f, 0f, 15f), new Vector3(13f, 7.2f, 12f), _paint, 180f);
            PlaceModularBuilding("Art/Buildings/HarborlightApartmentBlock", "Signal House", new Vector3(32f, 0f, 16f), new Vector3(14f, 9.2f, 14f), _paint, 0f);
            PlaceModularBuilding("Art/Buildings/HarborlightFoundryBlock", "Old Ferry Office", new Vector3(-29f, 0f, 24f), new Vector3(13f, 6.4f, 10f), _brick, 0f);

            PlaceModularBuilding("Art/Buildings/HarborlightApartmentBlock", "West Quay Flats", new Vector3(-85f, HarborlightDistrictSurfaceNetwork.TerrainHeight(-66f), -66f), new Vector3(15f, 8f, 14f), _paint, 90f);
            PlaceModularBuilding("Art/Buildings/HarborlightFoundryBlock", "Alder Street Market Hall", new Vector3(-42f, HarborlightDistrictSurfaceNetwork.TerrainHeight(-66f), -66f), new Vector3(16f, 7f, 14f), _brick, 0f);
            PlaceModularBuilding("Art/Buildings/HarborlightFoundryBlock", "Switchyard Workshop", new Vector3(43f, HarborlightDistrictSurfaceNetwork.TerrainHeight(-66f), -66f), new Vector3(16f, 8f, 14f), _brick, 180f);
            PlaceModularBuilding("Art/Buildings/HarborlightApartmentBlock", "East Quay Residences", new Vector3(85f, HarborlightDistrictSurfaceNetwork.TerrainHeight(-66f), -66f), new Vector3(15f, 9f, 14f), _paint, 270f);
            PlaceModularBuilding("Art/Buildings/HarborlightApartmentBlock", "Ridgeview House", new Vector3(-85f, HarborlightDistrictSurfaceNetwork.TerrainHeight(-112f), -112f), new Vector3(15f, 8f, 14f), _paint, 180f);
            PlaceModularBuilding("Art/Buildings/HarborlightFoundryBlock", "Breakwater Depot", new Vector3(43f, HarborlightDistrictSurfaceNetwork.TerrainHeight(-112f), -112f), new Vector3(16f, 8f, 14f), _brick, 90f);
            PlaceModularBuilding("Art/Buildings/HarborlightApartmentBlock", "Lantern Ridge Apartments", new Vector3(85f, HarborlightDistrictSurfaceNetwork.TerrainHeight(-112f), -112f), new Vector3(15f, 9f, 14f), _paint, 0f);
            PlaceModularBuilding("Art/Buildings/HarborlightApartmentBlock", "Lantern Square Lofts", new Vector3(-1.25f, HarborlightDistrictSurfaceNetwork.TerrainHeight(-66f), -66f), new Vector3(12.6f, 8f, 13.4f), _paint, 0f);
            PlaceModularBuilding("Art/Buildings/HarborlightFoundryBlock", "Foundry Row Studios", new Vector3(-1.25f, HarborlightDistrictSurfaceNetwork.TerrainHeight(-112f), -112f), new Vector3(12.6f, 8.6f, 13.4f), _brick, 180f);
            CreateOuterDistrictBuildings();

            CreateNpc("Mara · harbor steward", new Vector3(-3f, 0f, 2f), _accent, WorldAction.AcceptBeaconRepair);
            CreateNpc("Oren · ferry keeper", new Vector3(23f, 0f, 8f), _paint, WorldAction.AcceptDockInspection);
            CreateNpc("Lio · market courier", HarborlightGameState.CourierContactPosition, _accent, WorldAction.AcceptCourierDelivery);
            CreateCourierParcel(HarborlightGameState.CourierParcelPosition);
            CreateCourierDropoff(HarborlightGameState.CourierDropoffPosition);
            CreateDockInspectionSite(HarborlightGameState.DockInspectionPosition);
            CreateGarage(HarborlightGameState.GaragePosition);
            CreateBankTerminal(new Vector3(-42f, HarborlightDistrictSurfaceNetwork.TerrainHeight(7f), 7f));
            CreateBeacon(HarborlightGameState.BeaconPosition);
            CreateStreetlight(new Vector3(-6f, 0f, -1f));
            CreateStreetlight(new Vector3(8f, 0f, -1f));
            CreateStreetlight(new Vector3(-8f, 0f, 27f));
            CreateStreetlight(new Vector3(9f, 0f, 27f));
            CreateVehicle("Harbor taxi", new Vector3(-6f, 0f, 0.4f), 90f, new Color(0.82f, 0.78f, 0.66f));
            CreateVehicle("Pier service sedan", new Vector3(7f, 0f, 0.4f), -90f, new Color(0.17f, 0.27f, 0.34f));
            CreateVehicle("Market delivery van", new Vector3(-20f, 0f, 17f), 0f, new Color(0.59f, 0.28f, 0.22f));
            CreateVehicle("Mistline Hatchback", new Vector3(22f, 0f, -9f), 180f, new Color(0.31f, 0.52f, 0.49f));
            CreateVehicle("Breakwater Utility SUV", new Vector3(30f, 0f, -9f), 180f, new Color(0.69f, 0.39f, 0.24f));
            CreateVehicle("Coastal Sport Coupe", new Vector3(38f, 0f, -9f), 180f, new Color(0.25f, 0.36f, 0.48f));
            CreateVehicle("Tideglass Atelier Serein R", new Vector3(-31f, 0f, -0.5f), 90f, new Color(0.68f, 0.24f, 0.12f));
            CreateVehicle("North Pier Motor Works Vela GT", new Vector3(34f, 0f, -0.5f), -90f, new Color(0.24f, 0.32f, 0.49f));
            CreateCoastalTrees();
            CreateDistrictParkTrees();
            CreateDistrictParkDetails();
            CreateDistrictStreetlights();
            CreateOuterDistrictDetails();
        }

        private void BuildCoastAndTerrain()
        {
            var worldWidth = HarborlightDistrictSurfaceNetwork.WorldMaxX - HarborlightDistrictSurfaceNetwork.WorldMinX;
            var terrainXSegments = Mathf.CeilToInt(worldWidth / 15f);
            var terrainZSegments = Mathf.CeilToInt((HarborlightDistrictSurfaceNetwork.WorldMaxZ -
                HarborlightDistrictSurfaceNetwork.WorldMinZ) / 15f);
            CreateCoastalSurface("Northshore playable ground", _ground, 0.02f,
                worldWidth, terrainXSegments, terrainZSegments,
                x => HarborlightDistrictSurfaceNetwork.WorldMinZ, HarborlightDistrictSurfaceNetwork.CoastlineZ,
                2.5f, true,
                HarborlightDistrictSurfaceNetwork.TerrainHeight, true);
            CreateCoastalSurface("Harbor beach sand", _shore, 0.035f,
                worldWidth, terrainXSegments, 5,
                x => HarborlightDistrictSurfaceNetwork.CoastlineZ(x) - 0.18f, WaterlineZ, 2.5f, true,
                null, true);
            CreateCoastalSurface("North harbor water", _water, -0.025f, worldWidth + 80f, terrainXSegments, 8,
                WaterlineZ, x => HarborlightDistrictSurfaceNetwork.WorldMaxZ + 4f, 8f, false);
            CreateCoastalSurface("Open water around Harborlight peninsula", _water, -0.05f,
                worldWidth + 1800f, 40, 40,
                x => HarborlightDistrictSurfaceNetwork.WorldMinZ - 700f,
                x => HarborlightDistrictSurfaceNetwork.WorldMaxZ + 700f, 8f, false);

            for (var x = HarborlightDistrictSurfaceNetwork.WorldMinX + 2.4f; x <= HarborlightDistrictSurfaceNetwork.WorldMaxX - 2.4f; x += 4.8f)
            {
                if (x >= -15.5f && x <= 22.5f) continue;
                var z = WaterlineZ(x) - 0.08f;
                Block("Harbor seawall coping", new Vector3(x, 0.37f, z), new Vector3(4.7f, 0.7f, 0.32f), _sidewalk);
            }
        }

        private void CreateOuterDistrictBuildings()
        {
            var sites = HarborlightDistrictSurfaceNetwork.OuterBuildingLocations;
            for (var i = 0; i < sites.Length; i++)
            {
                var site = sites[i];
                var district = site.y < -3100f ? "Cedar Vale"
                    : site.x < -2600f ? "West Bluffs"
                    : site.x > 2600f ? "East Bluffs"
                    : site.y < -2350f ? "Southport Fields"
                    : site.y < -1650f ? "Southport"
                    : site.y < -700f ? "Southgate"
                    : site.x < -1250f ? "West Ridge"
                    : site.x > 1250f ? "East Ridge"
                    : site.x < 0f ? "West Works" : "East Docks";
                var industrial = district == "West Works" || district == "East Docks" ||
                                 ((district == "Southport" || district == "Southport Fields") && Mathf.Abs(site.x) > 700f);
                var useSmallBuilding = i % 4 == 3;
                var resourcePath = useSmallBuilding
                    ? "Art/Buildings/HarborlightBuilding_Small_1"
                    : industrial
                        ? "Art/Buildings/HarborlightBuilding_Large_2"
                        : "Art/Buildings/HarborlightBuilding_Medium_2_001";
                var height = industrial ? 8f + (i % 3) * 0.6f : 9f + (i % 3) * 0.5f;
                var size = industrial ? new Vector3(18f, height, 16f) : new Vector3(16f, height, 15f);
                var material = industrial ? _brick : _paint;
                var name = district + (industrial ? " Works Building " : " Residences ") + (i + 1).ToString("00");
                var position = new Vector3(site.x, HarborlightDistrictSurfaceNetwork.TerrainHeight(site.y), site.y);
                var yaw = (i % 4) * 90f;
                var streamSite = new OuterBuildingStreamSite
                {
                    ResourcePath = resourcePath,
                    Name = name,
                    GroundPosition = position,
                    Size = size,
                    FallbackMaterial = material,
                    Yaw = yaw
                };
                _outerBuildingSites.Add(streamSite);

                var foundationTopY = position.y + 0.045f;
                var collision = new GameObject(name + " collision");
                collision.transform.SetParent(transform, false);
                collision.transform.SetPositionAndRotation(
                    new Vector3(position.x, foundationTopY + size.y * 0.5f, position.z),
                    Quaternion.Euler(0f, yaw, 0f));
                collision.AddComponent<BoxCollider>().size = size;
            }
        }

        private void LoadOuterBuilding(OuterBuildingStreamSite site)
        {
            var visualRoot = new GameObject(site.Name + " streamed visuals");
            visualRoot.transform.SetParent(transform, false);
            var foundation = CreateBuildingFoundation(site.Name, site.GroundPosition, site.Size,
                site.Yaw, out var foundationTopY, visualRoot.transform);
            var foundationCollider = foundation.GetComponent<Collider>();
            if (foundationCollider != null) foundationCollider.enabled = false;

            var prefab = Resources.Load<GameObject>(site.ResourcePath);
            if (prefab == null)
            {
                var fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fallback.name = site.Name;
                fallback.transform.SetParent(visualRoot.transform, false);
                fallback.transform.SetPositionAndRotation(
                    new Vector3(site.GroundPosition.x, foundationTopY + site.Size.y * 0.5f, site.GroundPosition.z),
                    Quaternion.Euler(0f, site.Yaw, 0f));
                fallback.transform.localScale = site.Size;
                fallback.GetComponent<Renderer>().sharedMaterial = site.FallbackMaterial;
                var fallbackCollider = fallback.GetComponent<Collider>();
                if (fallbackCollider != null) fallbackCollider.enabled = false;
                site.VisualRoot = visualRoot;
                _loadedOuterBuildingCount++;
                if (Debug.isDebugBuild)
                    Debug.Log("HL_WORLD_STREAM_LOAD name=" + site.Name + " loaded=" + _loadedOuterBuildingCount + "/" + _outerBuildingSites.Count);
                return;
            }

            var building = Instantiate(prefab, site.GroundPosition,
                Quaternion.Euler(0f, site.Yaw, 0f), visualRoot.transform);
            building.name = site.Name;
            building.transform.localScale = new Vector3(site.Size.x / 12.4f,
                site.Size.y / 9.12f, site.Size.z / 12.4f);

            var lowestRenderedPoint = float.PositiveInfinity;
            foreach (var renderer in building.GetComponentsInChildren<Renderer>(true))
                lowestRenderedPoint = Mathf.Min(lowestRenderedPoint, renderer.bounds.min.y);
            if (!float.IsPositiveInfinity(lowestRenderedPoint))
                building.transform.position += Vector3.up * (foundationTopY - lowestRenderedPoint);

            foreach (var collider in building.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            site.VisualRoot = visualRoot;
            _loadedOuterBuildingCount++;
            if (Debug.isDebugBuild)
                Debug.Log("HL_WORLD_STREAM_LOAD name=" + site.Name + " loaded=" + _loadedOuterBuildingCount + "/" + _outerBuildingSites.Count);
        }

        private void UnloadOuterBuilding(OuterBuildingStreamSite site)
        {
            if (site.VisualRoot != null)
            {
                Destroy(site.VisualRoot);
                _loadedOuterBuildingCount--;
                if (Debug.isDebugBuild)
                    Debug.Log("HL_WORLD_STREAM_UNLOAD name=" + site.Name + " loaded=" + _loadedOuterBuildingCount + "/" + _outerBuildingSites.Count);
            }
            site.VisualRoot = null;
        }

        private void CreateOuterDistrictDetails()
        {
            var parkPath = new Vector3(-75f, HarborlightDistrictSurfaceNetwork.TerrainHeight(-337f) + 0.06f, -337f);
            var parkPathSize = new Vector3(52f, 0.12f, 2.4f);
            Block("Ternwood Park promenade", parkPath, parkPathSize, _sidewalk);
            CreateTiledTopSurface("Ternwood Park promenade pavers", parkPath, parkPathSize, _pavement);

            foreach (var x in new[] { -111f, -91f, -59f, -39f })
            foreach (var z in new[] { -365f, -309f })
                CreateTree(new Vector3(x, HarborlightDistrictSurfaceNetwork.TerrainHeight(z), z));

            CreatePromenadeBench("Ternwood Park west bench", new Vector3(-96f, HarborlightDistrictSurfaceNetwork.TerrainHeight(-327f) + 0.18f, -327f));
            CreatePromenadeBench("Ternwood Park east bench", new Vector3(-54f, HarborlightDistrictSurfaceNetwork.TerrainHeight(-347f) + 0.18f, -347f));

            for (var z = -225f; z >= HarborlightDistrictSurfaceNetwork.WorldMinZ + 105f; z -= 300f)
            for (var column = 0; column < HarborlightDistrictSurfaceNetwork.OuterNorthSouthRoads.Length; column++)
            {
                var x = HarborlightDistrictSurfaceNetwork.OuterNorthSouthRoads[column];
                if (HarborlightDistrictSurfaceNetwork.IsOuterNorthSouthRoadActive(column, z) &&
                    HarborlightDistrictSurfaceNetwork.IsWithinPlayableLand(new Vector2(x - 8f, z), 4f))
                    CreateStreetlight(new Vector3(x - 8f, HarborlightDistrictSurfaceNetwork.TerrainHeight(z), z));
            }

            var southgatePark = new Vector3(0f,
                HarborlightDistrictSurfaceNetwork.TerrainHeight(-975f) + 0.06f, -975f);
            var southgateParkSize = new Vector3(170f, 0.12f, 2.4f);
            Block("Southgate Commons promenade", southgatePark, southgateParkSize, _sidewalk);
            CreateTiledTopSurface("Southgate Commons stone promenade", southgatePark, southgateParkSize, _pavement);
            foreach (var x in new[] { -85f, -55f, 55f, 85f })
            foreach (var z in new[] { -1020f, -930f })
                CreateTree(new Vector3(x, HarborlightDistrictSurfaceNetwork.TerrainHeight(z), z));
            CreatePromenadeBench("Southgate Commons west bench", new Vector3(-36f,
                HarborlightDistrictSurfaceNetwork.TerrainHeight(-988f) + 0.18f, -988f));
            CreatePromenadeBench("Southgate Commons east bench", new Vector3(36f,
                HarborlightDistrictSurfaceNetwork.TerrainHeight(-962f) + 0.18f, -962f));

            var southportPark = new Vector3(0f,
                HarborlightDistrictSurfaceNetwork.TerrainHeight(-1875f) + 0.06f, -1875f);
            var southportParkSize = new Vector3(170f, 0.12f, 48f);
            Block("Southport Commons promenade", southportPark, southportParkSize, _sidewalk);
            CreateTiledTopSurface("Southport Commons stone promenade", southportPark,
                southportParkSize, _pavement);
            foreach (var x in new[] { -80f, -55f, 55f, 80f })
            foreach (var z in new[] { -1892f, -1858f })
                CreateTree(new Vector3(x, HarborlightDistrictSurfaceNetwork.TerrainHeight(z), z));
            CreatePromenadeBench("Southport Commons west bench", new Vector3(-35f,
                HarborlightDistrictSurfaceNetwork.TerrainHeight(-1875f) + 0.18f, -1875f));
            CreatePromenadeBench("Southport Commons east bench", new Vector3(35f,
                HarborlightDistrictSurfaceNetwork.TerrainHeight(-1875f) + 0.18f, -1875f));
        }

        private static float WaterlineZ(float x)
        {
            return HarborlightDistrictSurfaceNetwork.CoastlineZ(x) + 4.3f + Mathf.Sin(x * 0.075f + 0.4f) * 0.28f;
        }

        private void CreateDistrictParkTrees()
        {
            foreach (var x in new[] { -54f, -34f })
            foreach (var z in new[] { -119f, -107f })
                CreateTree(new Vector3(x, HarborlightDistrictSurfaceNetwork.TerrainHeight(z), z));
        }

        private void CreateDistrictParkDetails()
        {
            foreach (var x in new[] { -50f, -34f })
            {
                const float z = -112f;
                var terrain = HarborlightDistrictSurfaceNetwork.TerrainHeight(z);
                var pad = new Vector3(x, terrain + 0.108f, z);
                var padSize = new Vector3(3.2f, 0.12f, 2.6f);
                Block("Lantern Park seating pad", pad, padSize, _sidewalk);
                CreateTiledTopSurface("Lantern Park stone seating pad", pad, padSize, _pavement);
                CreatePromenadeBench(x < -42f ? "Lantern Park west bench" : "Lantern Park east bench",
                    new Vector3(x, terrain + 0.18f, z));
            }

            foreach (var position in new[] { new Vector3(-50f, 0f, -119f), new Vector3(-30f, 0f, -112f) })
            {
                var planter = position;
                planter.y = HarborlightDistrictSurfaceNetwork.TerrainHeight(planter.z) + 0.12f;
                CreatePromenadePlanter("Lantern Park stone planter", planter);
            }
        }

        private void CreateDistrictStreetlights()
        {
            foreach (var z in new[] { -36f, -82f, -128f })
            foreach (var x in new[] { -91f, 0f, 91f })
                CreateStreetlight(new Vector3(x, HarborlightDistrictSurfaceNetwork.TerrainHeight(z), z));
        }

        private static GameObject CreateCoastalSurface(string name, Material material, float y, float width,
            int xSegments, int zSegments, Func<float, float> nearEdge, Func<float, float> farEdge,
            float textureWorldSize, bool addCollider, Func<float, float> heightAtZ = null,
            bool shapeToDistrictBoundary = false)
        {
            var vertices = new Vector3[(xSegments + 1) * (zSegments + 1)];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[xSegments * zSegments * 6];
            var vertex = 0;
            for (var row = 0; row <= zSegments; row++)
            {
                var blend = (float)row / zSegments;
                for (var column = 0; column <= xSegments; column++)
                {
                    var lateral = Mathf.Lerp(-1f, 1f, (float)column / xSegments);
                    var x = lateral * width * 0.5f;
                    var z = Mathf.Lerp(nearEdge(x), farEdge(x), blend);
                    if (shapeToDistrictBoundary)
                    {
                        for (var iteration = 0; iteration < 3; iteration++)
                        {
                            x = HarborlightDistrictSurfaceNetwork.WorldCenterXAtZ(z) + lateral *
                                (width * 0.5f / (HarborlightDistrictSurfaceNetwork.WorldMaxX -
                                                 HarborlightDistrictSurfaceNetwork.WorldMinX) * 2f) *
                                HarborlightDistrictSurfaceNetwork.WorldHalfWidthAtZ(z);
                            z = Mathf.Lerp(nearEdge(x), farEdge(x), blend);
                        }
                    }
                    vertices[vertex] = new Vector3(x, y + (heightAtZ != null ? heightAtZ(z) : 0f), z);
                    uvs[vertex] = new Vector2(x / textureWorldSize, z / textureWorldSize);
                    vertex++;
                }
            }

            var triangle = 0;
            for (var row = 0; row < zSegments; row++)
            for (var column = 0; column < xSegments; column++)
            {
                var southwest = row * (xSegments + 1) + column;
                var southeast = southwest + 1;
                var northwest = southwest + xSegments + 1;
                var northeast = northwest + 1;
                triangles[triangle++] = southwest;
                triangles[triangle++] = northwest;
                triangles[triangle++] = southeast;
                triangles[triangle++] = southeast;
                triangles[triangle++] = northwest;
                triangles[triangle++] = northeast;
            }

            var mesh = new Mesh { name = name + " mesh" };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var surface = new GameObject(name);
            surface.AddComponent<MeshFilter>().sharedMesh = mesh;
            surface.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (addCollider) surface.AddComponent<MeshCollider>().sharedMesh = mesh;
            return surface;
        }

        private void CreatePierPiles()
        {
            foreach (var x in new[] { -12f, -4f, 4f, 12f })
            foreach (var z in new[] { 37.4f, 39.4f })
                CreatePierPile(new Vector3(x, -0.55f, z));

            foreach (var x in new[] { 9.5f, 16.5f })
            foreach (var z in new[] { 41f, 46f })
                CreatePierPile(new Vector3(x, -0.55f, z));
        }

        private void CreatePierPile(Vector3 position)
        {
            var pile = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pile.name = "Weathered timber pier pile";
            pile.transform.position = position;
            pile.transform.localScale = new Vector3(0.28f, 0.9f, 0.28f);
            pile.GetComponent<Renderer>().sharedMaterial = _bark;
            Destroy(pile.GetComponent<Collider>());
        }

        private void CreateNorthPierPromenade()
        {
            var plaza = new Vector3(10.25f, 0.06f, 17.5f);
            var plazaSize = new Vector3(24.5f, 0.12f, 21f);
            Block("Lantern Square to North Pier plaza base", plaza, plazaSize, _sidewalk);
            CreateTiledTopSurface("Lantern Square to North Pier textured paving", plaza, plazaSize, _pavement);

            CreatePromenadeBench("Promenade west bench", new Vector3(4.5f, 0.132f, 10.4f));
            CreatePromenadeBench("Promenade east bench", new Vector3(20.8f, 0.132f, 10.4f));
            CreatePromenadeBench("Promenade west overlook bench", new Vector3(4.5f, 0.132f, 23.6f));
            CreatePromenadeBench("Promenade east overlook bench", new Vector3(20.8f, 0.132f, 23.6f));
            CreatePromenadePlanter("Promenade west planter", new Vector3(4.5f, 0.132f, 26.2f));
            CreatePromenadePlanter("Promenade east planter", new Vector3(20.8f, 0.132f, 26.2f));
            CreatePromenadeMarketStall(new Vector3(17f, 0.132f, 17.5f));
            CreateNorthPierRailing();
        }

        private void CreatePromenadeBench(string name, Vector3 ground)
        {
            DecorationBlock(name + " seat", ground + new Vector3(0f, 0.52f, 0f), new Vector3(1.9f, 0.13f, 0.48f), _bark);
            DecorationBlock(name + " back", ground + new Vector3(0f, 0.91f, -0.18f), new Vector3(1.9f, 0.66f, 0.12f), _bark);
            foreach (var side in new[] { -0.68f, 0.68f })
                DecorationBlock(name + " support", ground + new Vector3(side, 0.28f, 0f), new Vector3(0.12f, 0.55f, 0.34f), _dark);
        }

        private void CreatePromenadePlanter(string name, Vector3 ground)
        {
            Block(name + " stone trough", ground + new Vector3(0f, 0.34f, 0f), new Vector3(2.1f, 0.56f, 1.45f), _sidewalk);
            foreach (var offset in new[]
            {
                new Vector3(-0.48f, 0.89f, 0f), Vector3.up * 0.94f, new Vector3(0.48f, 0.89f, 0.08f)
            })
            {
                var shrub = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                shrub.name = name + " coastal shrub";
                shrub.transform.position = ground + offset;
                shrub.transform.localScale = new Vector3(0.78f, 0.7f, 0.72f);
                shrub.GetComponent<Renderer>().sharedMaterial = _foliage;
                shrub.GetComponent<Collider>().enabled = false;
            }
        }

        private void CreatePromenadeMarketStall(Vector3 ground)
        {
            const float halfWidth = 1.85f;
            const float halfDepth = 1.35f;
            foreach (var x in new[] { -halfWidth, halfWidth })
            foreach (var z in new[] { -halfDepth, halfDepth })
                DecorationBlock("North Pier market stall timber post", ground + new Vector3(x, 1.45f, z), new Vector3(0.14f, 2.9f, 0.14f), _bark);

            DecorationBlock("North Pier market stall canopy", ground + new Vector3(0f, 3.02f, 0f), new Vector3(4.5f, 0.2f, 3.4f), _accent);
            DecorationBlock("North Pier market stall counter", ground + new Vector3(0f, 0.72f, -1.1f), new Vector3(3.3f, 1.05f, 0.55f), _bark);
            foreach (var x in new[] { -0.82f, 0f, 0.82f })
                DecorationBlock("North Pier market produce crate", ground + new Vector3(x, 1.31f, -1.1f), new Vector3(0.66f, 0.16f, 0.42f), _parcel);
            DecorationBlock("North Pier market stall fascia", ground + new Vector3(0f, 2.73f, -1.39f), new Vector3(2.6f, 0.34f, 0.09f), _dark);
            FloatingLabel("PIER MARKET", ground + new Vector3(0f, 3.12f, -1.48f));
        }

        private void CreateNorthPierRailing()
        {
            foreach (var x in new[] { -14.3f, 14.3f })
            {
                for (var z = 29.4f; z <= 39.4f; z += 2f)
                    Block("North Pier dark-metal guard post", new Vector3(x, 0.88f, z), new Vector3(0.12f, 1.32f, 0.12f), _dark);

                Block("North Pier upper guard rail", new Vector3(x, 1.48f, 34f), new Vector3(0.12f, 0.11f, 11.4f), _dark);
                Block("North Pier lower guard rail", new Vector3(x, 0.94f, 34f), new Vector3(0.1f, 0.09f, 11.4f), _dark);
            }
        }

        private static GameObject DecorationBlock(string name, Vector3 position, Vector3 size, Material material)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.position = position;
            block.transform.localScale = size;
            block.GetComponent<Renderer>().sharedMaterial = material;
            return block;
        }

        private void BuildPlayerAndCamera()
        {
            _state = gameObject.AddComponent<HarborlightGameState>();

            var player = new GameObject("Player · Harborlight Resident");
            player.name = "Player · Harborlight Resident";
            var ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
            if (ignoreRaycastLayer >= 0) player.layer = ignoreRaycastLayer;
            player.transform.position = new Vector3(0f, 0f, -2f);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.34f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            CreateHumanoidVisual(player.transform, _jacket, _trousers, _skin);
            var movement = player.AddComponent<HarborlightPlayer>();
            movement.Bind(_state);
            _player = movement;
            _offlinePlayer = movement;
            _localStreamingTarget = player.transform;
            _state.BindPlayer(player.transform);
            if (!_networkClientMode) movement.RestoreSavedVehicle(_vehicles);

            var cameraObject = new GameObject("Harborlight Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 600f;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(0.55f, 0.61f, 0.62f);
            var cameraRendering = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraRendering.renderPostProcessing = true;
            cameraObject.AddComponent<AudioListener>();
            var follow = cameraObject.AddComponent<HarborlightCamera>();
            _cameraFollow = follow;
            follow.target = player.transform;
            movement.BindCamera(follow);
            cameraObject.transform.position = player.transform.TransformPoint(new Vector3(0f, 5.1f, -7.8f));
            cameraObject.transform.LookAt(player.transform.position + Vector3.up * 1.35f);

            var sunObject = new GameObject("Harborlight Key Light");
            sunObject.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.05f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.52f;
            RenderSettings.sun = sun;

            var adapter = gameObject.AddComponent<HarborlightAdminAdapter>();
            adapter.Bind(_state);
            gameObject.AddComponent<GameAdminOverlay>();
            var backend = gameObject.AddComponent<HarborlightBackendClient>();
            backend.Connect(Resources.Load<HarborlightBackendSettings>("HarborlightBackendSettings"));
            var garage = gameObject.AddComponent<HarborlightGarageService>();
            garage.Bind(_state, backend, _vehicles);
            _garage = garage;
            movement.BindGarage(garage);
            movement.BindBackend(backend);
            var hud = gameObject.AddComponent<HarborlightHud>();
            _hud = hud;
            hud.Bind(_state, player.transform);
            hud.BindBackend(backend);
            hud.BindGarage(garage);
            gameObject.AddComponent<HarborlightTouchControls>();

            RenderSettings.ambientLight = new Color(0.50f, 0.53f, 0.56f);
            ConfigureVisualEnvironment();
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.72f, 0.78f, 0.86f);
            RenderSettings.ambientEquatorColor = new Color(0.56f, 0.60f, 0.66f);
            RenderSettings.ambientGroundColor = new Color(0.38f, 0.40f, 0.43f);
        }

        public void AttachNetworkPlayer(HarborlightPlayer player, HarborlightNetworkPlayer networkPlayer)
        {
            if (!_networkClientMode || player == null || networkPlayer == null || !networkPlayer.IsOwner) return;

            if (_offlinePlayer != null && _offlinePlayer != player)
                Destroy(_offlinePlayer.gameObject);

            _player = player;
            _localStreamingTarget = player.transform;
            _player.Bind(_state);
            _player.BindCamera(_cameraFollow);
            _player.BindGarage(_garage);
            _cameraFollow.target = player.transform;
            _state.BindPlayer(player.transform, false);
            _hud.Bind(_state, player.transform);
        }

        private static void ConfigureVisualEnvironment()
        {
            var volumeObject = new GameObject("Harborlight Global Color Grade");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;

            var profile = volume.profile;
            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);

            var color = profile.Add<ColorAdjustments>(true);
            color.contrast.Override(5f);
            color.saturation.Override(2f);

            if (!Application.isMobilePlatform)
            {
                var bloom = profile.Add<Bloom>(true);
                bloom.threshold.Override(1.6f);
                bloom.intensity.Override(0.04f);
                bloom.scatter.Override(0.35f);
            }

            var skyboxShader = Shader.Find("Skybox/Procedural");
            if (skyboxShader != null)
            {
                var skybox = new Material(skyboxShader) { name = "Harborlight coastal sky" };
                skybox.SetFloat("_SunDisk", 0f);
                skybox.SetFloat("_AtmosphereThickness", 0.85f);
                skybox.SetColor("_SkyTint", new Color(0.58f, 0.68f, 0.86f));
                skybox.SetColor("_GroundColor", new Color(0.32f, 0.38f, 0.40f));
                skybox.SetFloat("_Exposure", 0.8f);
                RenderSettings.skybox = skybox;
            }

            var horizon = new Color(0.55f, 0.67f, 0.75f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = horizon;
            RenderSettings.fogDensity = 0.0009f;
        }

        private void Building(string name, Vector3 position, Vector3 size, Material material)
        {
            Block(name, position, size, material);
            Block(name + " roofline", position + new Vector3(0f, size.y * 0.5f + 0.22f, 0f), new Vector3(size.x + 0.45f, 0.35f, size.z + 0.45f), _sidewalk);
            Block(name + " parapet", position + new Vector3(0f, size.y * 0.5f + 0.55f, 0f), new Vector3(size.x * 0.72f, 0.35f, size.z * 0.72f), _dark);
            var windowRows = Mathf.Max(1, Mathf.FloorToInt((size.y - 1.4f) / 2.4f));
            for (var row = 0; row < windowRows; row++)
            {
                var y = -size.y * 0.5f + 1.8f + row * 2.25f;
                for (var x = -1; x <= 1; x++)
                {
                    Block(name + " window", position + new Vector3(x * 3.2f, y, -size.z * 0.5f - 0.035f), new Vector3(1.2f, 1.05f, 0.08f), _window);
                    Block(name + " rear window", position + new Vector3(x * 3.2f, y, size.z * 0.5f + 0.035f), new Vector3(1.2f, 1.05f, 0.08f), _window);
                }
            }

            var storefrontY = -size.y * 0.5f + 1.25f;
            foreach (var side in new[] { -1f, 1f })
            {
                var frontZ = side * (size.z * 0.5f + 0.04f);
                Block(name + " shopfront", position + new Vector3(0f, storefrontY, frontZ), new Vector3(2.3f, 2.35f, 0.1f), _window);
                Block(name + " entrance", position + new Vector3(2.1f, storefrontY - 0.1f, frontZ), new Vector3(0.95f, 2.25f, 0.12f), _dark);
                Block(name + " awning", position + new Vector3(0f, storefrontY + 1.45f, frontZ), new Vector3(4.2f, 0.26f, 0.9f), _accent);
            }

            Block(name + " rooftop vent", position + new Vector3(size.x * 0.27f, size.y * 0.5f + 0.78f, 0f), new Vector3(1.4f, 0.75f, 1.2f), _dark);
        }

        private void PlaceModularBuilding(string resourcePath, string name, Vector3 groundPosition, Vector3 fallbackSize, Material fallbackMaterial, float yaw)
        {
            CreateBuildingFoundation(name, groundPosition, fallbackSize, yaw, out var foundationTopY);
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                Building(name, new Vector3(groundPosition.x, foundationTopY + fallbackSize.y * 0.5f, groundPosition.z), fallbackSize, fallbackMaterial);
                return;
            }

            var building = Instantiate(prefab, groundPosition, Quaternion.Euler(0f, yaw, 0f));
            building.name = name;
            building.transform.localScale = new Vector3(fallbackSize.x / 12.4f,
                fallbackSize.y / 9.12f, fallbackSize.z / 12.4f);

            var lowestRenderedPoint = float.PositiveInfinity;
            foreach (var renderer in building.GetComponentsInChildren<Renderer>(true))
                lowestRenderedPoint = Mathf.Min(lowestRenderedPoint, renderer.bounds.min.y);
            if (!float.IsPositiveInfinity(lowestRenderedPoint))
                building.transform.position += Vector3.up * (foundationTopY - lowestRenderedPoint);

        }

        private GameObject CreateBuildingFoundation(string name, Vector3 center, Vector3 size, float yaw,
            out float topY, Transform parent = null)
        {
            var halfX = size.x * 0.5f + BuildingFoundationOverhang;
            var halfZ = size.z * 0.5f + BuildingFoundationOverhang;
            var rotation = Quaternion.Euler(0f, yaw, 0f);
            var groundCorners = new[]
            {
                new Vector3(-halfX, 0f, -halfZ), new Vector3(halfX, 0f, -halfZ),
                new Vector3(-halfX, 0f, halfZ), new Vector3(halfX, 0f, halfZ)
            };
            var lowestGroundY = float.PositiveInfinity;
            topY = float.NegativeInfinity;
            foreach (var localCorner in groundCorners)
            {
                var z = center.z + (rotation * localCorner).z;
                var groundY = HarborlightDistrictSurfaceNetwork.TerrainHeight(z) + 0.02f;
                lowestGroundY = Mathf.Min(lowestGroundY, groundY);
                topY = Mathf.Max(topY, groundY);
            }

            topY += 0.005f;
            const float groundBurial = 0.08f;
            var foundationBottomY = lowestGroundY - groundBurial;
            var foundationHeight = topY - foundationBottomY;
            var foundation = Block(name + " stone foundation",
                new Vector3(center.x, (topY + foundationBottomY) * 0.5f, center.z),
                new Vector3(size.x + BuildingFoundationOverhang * 2f, foundationHeight,
                    size.z + BuildingFoundationOverhang * 2f), _sidewalk);
            foundation.transform.rotation = rotation;
            if (parent != null) foundation.transform.SetParent(parent, true);
            return foundation;
        }

        private void CreateNpc(string label, Vector3 position, Material material, WorldAction action)
        {
            var npc = new GameObject(label);
            npc.name = label;
            npc.transform.position = position;
            CreateHumanoidVisual(npc.transform, material, _trousers, _skin);
            var interactable = npc.AddComponent<HarborlightInteractable>();
            interactable.Action = action;
            interactable.DisplayName = label;
            FloatingLabel(label, position + new Vector3(0f, 2.3f, 0f));
        }

        private void CreateCourierParcel(Vector3 position)
        {
            var parcel = Block("Market courier parcel", position + Vector3.up * 0.55f, new Vector3(1f, 1.1f, 0.9f), _parcel);
            Block("Parcel seal", position + Vector3.up * 0.57f, new Vector3(0.16f, 1.12f, 0.94f), _accent);
            var interactable = parcel.AddComponent<HarborlightInteractable>();
            interactable.Action = WorldAction.CollectCourierParcel;
            interactable.DisplayName = "Market parcel";
            parcel.AddComponent<HarborlightPoliceDispatchSite>().DispatchCode = "market-access";
            FloatingLabel("MARKET PARCEL", position + new Vector3(0f, 1.7f, 0f));
        }

        private void CreateCourierDropoff(Vector3 position)
        {
            var floor = Block("Old Ferry delivery bay", position + new Vector3(0f, 0.08f, 0f), new Vector3(3f, 0.16f, 3f), _sidewalk);
            Block("Delivery bay post west", position + new Vector3(-1.25f, 1.2f, 0f), new Vector3(0.22f, 2.4f, 0.22f), _accent);
            Block("Delivery bay post east", position + new Vector3(1.25f, 1.2f, 0f), new Vector3(0.22f, 2.4f, 0.22f), _accent);
            Block("Delivery bay header", position + new Vector3(0f, 2.32f, 0f), new Vector3(2.7f, 0.25f, 0.25f), _dark);
            var interactable = floor.AddComponent<HarborlightInteractable>();
            interactable.Action = WorldAction.DeliverCourierParcel;
            interactable.DisplayName = "Old Ferry delivery bay";
            FloatingLabel("DELIVERY BAY", position + new Vector3(0f, 2.8f, 0f));
        }

        private void CreateDockInspectionSite(Vector3 position)
        {
            var panel = Block("East Pier warning circuit", position + Vector3.up * 0.38f, new Vector3(1.6f, 0.76f, 1.2f), _accent);
            var interactable = panel.AddComponent<HarborlightInteractable>();
            interactable.Action = WorldAction.InspectDock;
            interactable.DisplayName = "East Pier warning circuit";
            panel.AddComponent<HarborlightPoliceDispatchSite>().DispatchCode = "pier-circuit";
            FloatingLabel("PIER CIRCUIT", position + Vector3.up * 1.6f);
        }

        private void CreateGarage(Vector3 position)
        {
            Block("Harborlight Motor Works", position + new Vector3(0f, 2.8f, 0f), new Vector3(10f, 5.6f, 8f), _brick);
            Block("Garage roof sign", position + new Vector3(0f, 5.8f, 0f), new Vector3(10.8f, 0.35f, 8.8f), _dark);
            Block("Garage service bay", position + new Vector3(0f, 1.55f, -4.05f), new Vector3(4f, 3.1f, 0.14f), _dark);
            var marker = new GameObject("Harborlight Motor Works entrance");
            marker.transform.position = position + new Vector3(-5.2f, 0f, -3f);
            var interactable = marker.AddComponent<HarborlightInteractable>();
            interactable.Action = WorldAction.OpenGarage;
            interactable.DisplayName = "Harborlight Motor Works";
            FloatingLabel("MOTOR WORKS", position + new Vector3(0f, 6.4f, -4f));
        }

        private void CreateBankTerminal(Vector3 ground)
        {
            Block("Harbor Bank ATM plinth", ground + new Vector3(0f, 0.12f, 0f), new Vector3(1.6f, 0.24f, 1.05f), _sidewalk);
            Block("Harbor Bank ATM cabinet", ground + new Vector3(0f, 1.05f, 0f), new Vector3(1.05f, 1.75f, 0.8f), _dark);
            Block("Harbor Bank ATM display", ground + new Vector3(0f, 1.35f, -0.42f), new Vector3(0.62f, 0.42f, 0.035f), _accent);
            Block("Harbor Bank ATM keypad", ground + new Vector3(0f, 0.88f, -0.425f), new Vector3(0.34f, 0.22f, 0.035f), _marking);
            var terminal = new GameObject("Harbor Bank ATM interaction");
            terminal.transform.position = ground + new Vector3(0f, 0.25f, -0.7f);
            var interactable = terminal.AddComponent<HarborlightInteractable>();
            interactable.Action = WorldAction.OpenBank;
            interactable.DisplayName = "Harbor Bank ATM";
            FloatingLabel("HARBOR BANK · ATM", ground + new Vector3(0f, 2.1f, 0f));
        }

        private void BuildStreetDetails()
        {
            var southSidewalk = new Vector3(0f, 0.08f, -7.15f);
            var sidewalkSize = new Vector3(88f, 0.16f, 2.6f);
            Block("South sidewalk", southSidewalk, sidewalkSize, _sidewalk);
            CreateTiledTopSurface("South sidewalk stone pavers", southSidewalk, sidewalkSize, _pavement);
            var northSidewalk = new Vector3(0f, 0.08f, 5.15f);
            Block("North sidewalk", northSidewalk, sidewalkSize, _sidewalk);
            CreateTiledTopSurface("North sidewalk stone pavers", northSidewalk, sidewalkSize, _pavement);

            for (var x = -39f; x <= 39f; x += 7f)
            {
                var dash = Block("Lantern Square lane dash", new Vector3(x, 0.055f, -1f), new Vector3(3.2f, 0.018f, 0.16f), _marking);
                Destroy(dash.GetComponent<Collider>());
            }

            foreach (var crossingX in new[] { -20f, 18f })
            {
                for (var z = -4.7f; z <= 2.7f; z += 0.9f)
                {
                    var stripe = Block("Crosswalk stripe", new Vector3(crossingX, 0.06f, z), new Vector3(0.42f, 0.02f, 0.58f), _marking);
                    Destroy(stripe.GetComponent<Collider>());
                }
            }
        }

        private static GameObject CreateTiledTopSurface(string name, Vector3 center, Vector3 size, Material material)
        {
            const float tileWorldSize = 2.5f;
            var top = size.y * 0.5f + 0.012f;
            var halfWidth = size.x * 0.5f;
            var halfDepth = size.z * 0.5f;
            var mesh = new Mesh { name = name + " mesh" };
            mesh.vertices = new[]
            {
                new Vector3(-halfWidth, top, -halfDepth), new Vector3(halfWidth, top, -halfDepth),
                new Vector3(-halfWidth, top, halfDepth), new Vector3(halfWidth, top, halfDepth)
            };
            mesh.uv = new[]
            {
                new Vector2((center.x - halfWidth) / tileWorldSize, (center.z - halfDepth) / tileWorldSize),
                new Vector2((center.x + halfWidth) / tileWorldSize, (center.z - halfDepth) / tileWorldSize),
                new Vector2((center.x - halfWidth) / tileWorldSize, (center.z + halfDepth) / tileWorldSize),
                new Vector2((center.x + halfWidth) / tileWorldSize, (center.z + halfDepth) / tileWorldSize)
            };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var surface = new GameObject(name);
            surface.transform.position = center;
            surface.AddComponent<MeshFilter>().sharedMesh = mesh;
            surface.AddComponent<MeshRenderer>().sharedMaterial = material;
            return surface;
        }

        private void CreateCoastalTrees()
        {
            foreach (var x in new[] { -36f, -26f, -15f, 15f, 26f, 36f })
            {
                CreateTree(new Vector3(x, 0f, 6.3f));
                if (x > -26f && x < 26f) CreateTree(new Vector3(x, 0f, -8.2f));
            }
        }

        private void CreateTree(Vector3 position)
        {
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Coastal street tree trunk";
            trunk.transform.position = position + new Vector3(0f, 1.55f, 0f);
            trunk.transform.localScale = new Vector3(0.22f, 1.55f, 0.22f);
            trunk.GetComponent<Renderer>().sharedMaterial = _bark;

            foreach (var offset in new[] { Vector3.zero, new Vector3(-0.45f, 0.35f, 0.12f), new Vector3(0.42f, 0.28f, -0.1f) })
            {
                var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crown.name = "Coastal tree canopy";
                crown.transform.position = position + new Vector3(0f, 3.15f, 0f) + offset;
                crown.transform.localScale = Vector3.one * 1.65f;
                crown.GetComponent<Renderer>().sharedMaterial = _foliage;
            }
        }

        private void CreateVehicle(string name, Vector3 position, float heading, Color bodyColor)
        {
            var vehicleIndex = _vehicles.Count;
            var isVan = name.IndexOf("van", System.StringComparison.OrdinalIgnoreCase) >= 0;
            var isTaxi = name.IndexOf("taxi", System.StringComparison.OrdinalIgnoreCase) >= 0;
            var isHatchback = vehicleIndex == 3;
            var isPickup = vehicleIndex == 4;
            var isExotic = vehicleIndex == 6;
            var isSportCoupe = vehicleIndex == 5 || vehicleIndex == 7;
            var bodyLength = isVan ? 4.35f : isPickup ? 4.65f : isHatchback ? 3.82f : isExotic ? 4.52f : isSportCoupe ? 4.35f : 4.2f;
            var vehicle = new GameObject(name);
            vehicle.transform.position = position;
            vehicle.transform.rotation = Quaternion.Euler(0f, heading, 0f);
            var collider = vehicle.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, isVan || isPickup ? 0.9f : 0.78f, 0f);
            collider.size = new Vector3(isPickup ? 2.05f : 1.95f, isVan ? 1.76f : isPickup ? 1.9f : 1.5f, bodyLength);
            var rigidbody = vehicle.AddComponent<Rigidbody>();
            rigidbody.mass = 1250f;
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            var driving = vehicle.AddComponent<HarborlightVehicle>();
            driving.Initialize(name, bodyColor, vehicleIndex);
            var interactable = vehicle.AddComponent<HarborlightInteractable>();
            interactable.Action = WorldAction.EnterVehicle;
            interactable.DisplayName = name;
            interactable.Vehicle = driving;
            if (TryCreatePackVehicleVisual(vehicle.transform, vehicleIndex, name, bodyLength, collider))
            {
                driving.CachePaintableParts();
                driving.RestoreFactoryPaint();
                _vehicles.Add(driving);
                return;
            }

            var body = Material(name + " paint", bodyColor);
            SetSurface(body, 0.24f, 0.78f);

            var bodyWidth = isPickup ? 2f : isExotic ? 2.04f : 1.9f;
            var wheelBase = isPickup ? 1.48f : isHatchback ? 1.22f : isSportCoupe || isExotic ? 1.42f : 1.35f;
            CreateVehicleBodyShell(vehicle.transform, name, body, bodyWidth, bodyLength, wheelBase, isVan);

            if (isVan)
            {
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " cargo shell paintable", new Vector3(0f, 1.2f, -0.72f), new Vector3(1.76f, 1.0f, 2.25f), body);
                CreateVanCabin(vehicle.transform, name, body);
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " windshield", new Vector3(0f, 1.36f, 1.25f), new Vector3(1.36f, 0.56f, 0.04f), _glass, Quaternion.Euler(-24f, 0f, 0f));
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " left front side glass", new Vector3(-0.83f, 1.38f, 0.9f), new Vector3(0.035f, 0.42f, 0.65f), _glass);
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " right front side glass", new Vector3(0.83f, 1.38f, 0.9f), new Vector3(0.035f, 0.42f, 0.65f), _glass);
                foreach (var side in new[] { -1f, 1f })
                {
                    CreateCabinFrameBar(vehicle.transform, name + " A pillar paintable", new Vector3(side * 0.84f, 0.93f, 1.43f), new Vector3(side * 0.76f, 1.68f, 1.04f), 0.075f, body);
                    CreateCabinFrameBar(vehicle.transform, name + " window rear frame paintable", new Vector3(side * 0.81f, 0.95f, 0.57f), new Vector3(side * 0.77f, 1.65f, 0.57f), 0.055f, body);
                }
            }
            else if (isPickup)
            {
                CreatePickupCab(vehicle.transform, name, body);
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " windshield", new Vector3(0f, 1.38f, 1.32f), new Vector3(1.42f, 0.58f, 0.04f), _glass, Quaternion.Euler(-23f, 0f, 0f));
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " left front side glass", new Vector3(-0.9f, 1.4f, 0.67f), new Vector3(0.035f, 0.43f, 0.78f), _glass);
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " right front side glass", new Vector3(0.9f, 1.4f, 0.67f), new Vector3(0.035f, 0.43f, 0.78f), _glass);
                foreach (var side in new[] { -1f, 1f })
                {
                    CreateCabinFrameBar(vehicle.transform, name + " A pillar paintable", new Vector3(side * 0.89f, 0.96f, 1.35f), new Vector3(side * 0.82f, 1.76f, 1.13f), 0.08f, body);
                    CreateCabinFrameBar(vehicle.transform, name + " B pillar paintable", new Vector3(side * 0.88f, 0.97f, 0.26f), new Vector3(side * 0.83f, 1.72f, 0.26f), 0.06f, body);
                }
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " open bed floor", new Vector3(0f, 0.96f, -1.06f), new Vector3(1.56f, 0.06f, 1.56f), _dark);
                foreach (var side in new[] { -0.86f, 0.86f })
                    VisualPart(vehicle.transform, PrimitiveType.Cube, name + " bed rail", new Vector3(side, 1.12f, -1.06f), new Vector3(0.12f, 0.38f, 1.56f), body);
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " tailgate", new Vector3(0f, 1.08f, -1.91f), new Vector3(1.72f, 0.38f, 0.12f), body);
                foreach (var side in new[] { -1f, 1f })
                    VisualPart(vehicle.transform, PrimitiveType.Cube, name + " running board", new Vector3(side, 0.45f, 0f), new Vector3(0.16f, 0.12f, 1.38f), _dark);
            }
            else if (isHatchback)
            {
                CreateHatchbackCabin(vehicle.transform, name, body);
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " windshield", new Vector3(0f, 1.29f, 0.72f), new Vector3(1.3f, 0.6f, 0.04f), _glass, Quaternion.Euler(-31f, 0f, 0f));
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " rear hatch glass", new Vector3(0f, 1.33f, -1.02f), new Vector3(1.3f, 0.58f, 0.04f), _glass, Quaternion.Euler(22f, 0f, 0f));
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " left side glass", new Vector3(-0.85f, 1.37f, -0.04f), new Vector3(0.035f, 0.4f, 1.22f), _glass);
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " right side glass", new Vector3(0.85f, 1.37f, -0.04f), new Vector3(0.035f, 0.4f, 1.22f), _glass);
                foreach (var side in new[] { -1f, 1f })
                {
                    CreateCabinFrameBar(vehicle.transform, name + " A pillar paintable", new Vector3(side * 0.84f, 0.96f, 0.82f), new Vector3(side * 0.73f, 1.67f, 0.5f), 0.07f, body);
                    CreateCabinFrameBar(vehicle.transform, name + " B pillar", new Vector3(side * 0.84f, 0.97f, -0.06f), new Vector3(side * 0.73f, 1.65f, -0.06f), 0.06f, _dark);
                    CreateCabinFrameBar(vehicle.transform, name + " C pillar paintable", new Vector3(side * 0.84f, 0.96f, -0.94f), new Vector3(side * 0.73f, 1.67f, -0.67f), 0.07f, body);
                }
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " rear roof spoiler", new Vector3(0f, 1.72f, -1.08f), new Vector3(1.08f, 0.09f, 0.24f), _dark);
            }
            else if (isExotic)
            {
                CreateExoticCabin(vehicle.transform, name, body);
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " panoramic windshield", new Vector3(0f, 1.14f, 0.64f), new Vector3(1.12f, 0.48f, 0.035f), _glass, Quaternion.Euler(-31f, 0f, 0f));
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " rear engine glass", new Vector3(0f, 1.14f, -0.78f), new Vector3(1.08f, 0.4f, 0.035f), _glass, Quaternion.Euler(24f, 0f, 0f));
                foreach (var side in new[] { -1f, 1f })
                {
                    VisualPart(vehicle.transform, PrimitiveType.Cube, name + " frameless side glass", new Vector3(side * 0.63f, 1.19f, -0.04f), new Vector3(0.032f, 0.3f, 0.94f), _glass);
                    CreateCabinFrameBar(vehicle.transform, name + " A pillar paintable", new Vector3(side * 0.78f, 0.96f, 0.98f), new Vector3(side * 0.56f, 1.34f, 0.38f), 0.055f, body);
                    CreateCabinFrameBar(vehicle.transform, name + " rear roof strut paintable", new Vector3(side * 0.78f, 0.96f, -1.12f), new Vector3(side * 0.56f, 1.34f, -0.72f), 0.055f, body);
                    VisualPart(vehicle.transform, PrimitiveType.Cube, name + " side air intake", new Vector3(side * 0.99f, 0.74f, -0.38f), new Vector3(0.035f, 0.16f, 0.52f), _dark);
                    VisualPart(vehicle.transform, PrimitiveType.Cube, name + " side sill", new Vector3(side * 0.97f, 0.4f, -0.04f), new Vector3(0.1f, 0.12f, 1.72f), _dark);
                }
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " front aero splitter", new Vector3(0f, 0.28f, bodyLength * 0.5f + 0.12f), new Vector3(1.82f, 0.07f, 0.24f), _dark);
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " rear diffuser", new Vector3(0f, 0.28f, -bodyLength * 0.5f - 0.1f), new Vector3(1.64f, 0.12f, 0.3f), _dark);
                foreach (var side in new[] { -0.56f, 0.56f })
                    VisualPart(vehicle.transform, PrimitiveType.Cube, name + " rear wing mount", new Vector3(side, 1.24f, -1.72f), new Vector3(0.065f, 0.34f, 0.08f), _dark);
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " rear wing", new Vector3(0f, 1.42f, -1.72f), new Vector3(1.46f, 0.08f, 0.28f), body);
            }
            else
            {
                CreateSedanCabin(vehicle.transform, name, body, isSportCoupe);
                var roofHeight = isSportCoupe ? 1.49f : 1.65f;
                var glassY = isSportCoupe ? 1.22f : 1.3f;
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " windshield", new Vector3(0f, glassY, 0.72f), new Vector3(isSportCoupe ? 1.18f : 1.27f, isSportCoupe ? 0.58f : 0.82f, 0.04f), _glass, Quaternion.Euler(isSportCoupe ? -31f : -38f, 0f, 0f));
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " rear glass", new Vector3(0f, glassY, -0.98f), new Vector3(isSportCoupe ? 1.16f : 1.25f, isSportCoupe ? 0.56f : 0.76f, 0.04f), _glass, Quaternion.Euler(24f, 0f, 0f));
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " left front side glass", new Vector3(isSportCoupe ? -0.66f : -0.75f, glassY + 0.05f, 0.22f), new Vector3(0.035f, 0.36f, 0.76f), _glass);
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " right front side glass", new Vector3(isSportCoupe ? 0.66f : 0.75f, glassY + 0.05f, 0.22f), new Vector3(0.035f, 0.36f, 0.76f), _glass);
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " left rear side glass", new Vector3(isSportCoupe ? -0.65f : -0.72f, glassY + 0.05f, -0.66f), new Vector3(0.035f, 0.36f, 0.62f), _glass);
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " right rear side glass", new Vector3(isSportCoupe ? 0.65f : 0.72f, glassY + 0.05f, -0.66f), new Vector3(0.035f, 0.36f, 0.62f), _glass);
                foreach (var side in new[] { -1f, 1f })
                {
                    var roofSide = isSportCoupe ? 0.58f : 0.64f;
                    var roofRear = isSportCoupe ? -0.72f : -0.82f;
                    var roofFront = isSportCoupe ? 0.36f : 0.46f;
                    CreateCabinFrameBar(vehicle.transform, name + " A pillar paintable", new Vector3(side * 0.78f, 0.96f, 0.98f), new Vector3(side * roofSide, roofHeight, roofFront), 0.06f, body);
                    CreateCabinFrameBar(vehicle.transform, name + " B pillar", new Vector3(side * 0.76f, 0.98f, -0.2f), new Vector3(side * roofSide, roofHeight, -0.2f), 0.055f, _dark);
                    CreateCabinFrameBar(vehicle.transform, name + " C pillar paintable", new Vector3(side * 0.78f, 0.96f, -1.12f), new Vector3(side * roofSide, roofHeight, roofRear), 0.06f, body);
                }
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " roof paintable", new Vector3(0f, roofHeight, -0.13f), new Vector3(isSportCoupe ? 1.18f : 1.32f, 0.08f, isSportCoupe ? 1.08f : 1.2f), body);
                if (isSportCoupe)
                {
                    VisualPart(vehicle.transform, PrimitiveType.Cube, name + " front aero lip", new Vector3(0f, 0.34f, bodyLength * 0.5f + 0.08f), new Vector3(1.7f, 0.08f, 0.2f), _dark);
                    foreach (var side in new[] { -1f, 1f })
                        VisualPart(vehicle.transform, PrimitiveType.Cube, name + " side vent", new Vector3(side * 0.966f, 0.72f, 0.75f), new Vector3(0.026f, 0.12f, 0.42f), _dark);
                }
            }

            VisualPart(vehicle.transform, PrimitiveType.Cube, name + " front bumper", new Vector3(0f, 0.43f, bodyLength * 0.5f + 0.015f), new Vector3(1.78f, 0.17f, 0.13f), _dark);
            VisualPart(vehicle.transform, PrimitiveType.Cube, name + " rear bumper", new Vector3(0f, 0.43f, -bodyLength * 0.5f - 0.015f), new Vector3(1.78f, 0.17f, 0.13f), _dark);
            VisualPart(vehicle.transform, PrimitiveType.Cube, name + " front grille", new Vector3(0f, 0.66f, bodyLength * 0.5f + 0.035f), new Vector3(0.68f, 0.2f, 0.035f), _dark);
            for (var slat = -1; slat <= 1; slat++)
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " grille slat", new Vector3(0f, 0.66f + slat * 0.05f, bodyLength * 0.5f + 0.06f), new Vector3(0.54f, 0.014f, 0.015f), _sidewalk);

            foreach (var side in new[] { -1f, 1f })
            {
                var sideX = side * (isPickup ? 1.01f : 0.956f);
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " door seam", new Vector3(sideX, 0.66f, -0.2f), new Vector3(0.018f, 0.42f, 0.018f), _dark);
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " door handle", new Vector3(sideX + side * 0.01f, 0.84f, -0.12f), new Vector3(0.04f, 0.05f, 0.2f), _accent);
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " side mirror", new Vector3(side * (isPickup ? 0.94f : 0.91f), isSportCoupe ? 1.23f : 1.38f, 0.66f), new Vector3(0.22f, 0.13f, 0.24f), body);
                if (isTaxi)
                    VisualPart(vehicle.transform, PrimitiveType.Cube, name + " teal side stripe", new Vector3(side * 0.958f, 0.48f, -0.12f), new Vector3(0.025f, 0.16f, 1.65f), _jacket);
            }
            if (isTaxi)
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " amber roof beacon", new Vector3(0f, 1.8f, -0.13f), new Vector3(0.38f, 0.12f, 0.24f), _accent);

            var wheelSide = isPickup ? 1.02f : isExotic ? 1.04f : 0.96f;
            var tireSize = isPickup ? 0.76f : isExotic ? 0.72f : 0.68f;
            foreach (var side in new[] { -1f, 1f })
            foreach (var axle in new[] { -wheelBase, wheelBase })
            {
                VisualPart(vehicle.transform, PrimitiveType.Cylinder, name + " tire", new Vector3(side * wheelSide, 0.36f, axle), new Vector3(tireSize, 0.2f, tireSize), _rubber, Quaternion.Euler(0f, 0f, 90f));
                CreateAlloyWheelFace(vehicle.transform, name, side, wheelSide, axle);
            }

            foreach (var side in new[] { -0.58f, 0.58f })
            {
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " headlight bezel", new Vector3(side, 0.72f, bodyLength * 0.5f + 0.02f), new Vector3(0.42f, 0.19f, 0.06f), _dark);
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " headlight", new Vector3(side, 0.72f, bodyLength * 0.5f + 0.055f), new Vector3(0.34f, 0.12f, 0.025f), _headlamp);
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " taillight bezel", new Vector3(side, 0.7f, -bodyLength * 0.5f - 0.015f), new Vector3(0.38f, 0.2f, 0.06f), _dark);
                VisualPart(vehicle.transform, PrimitiveType.Cube, name + " taillight", new Vector3(side, 0.7f, -bodyLength * 0.5f - 0.05f), new Vector3(0.3f, 0.14f, 0.025f), _taillamp);
            }
            driving.CachePaintableParts();
            _vehicles.Add(driving);
        }

        private static bool TryCreatePackVehicleVisual(Transform vehicleRoot, int vehicleIndex, string displayName, float targetLength, BoxCollider collider)
        {
            if (vehicleIndex < 0 || vehicleIndex >= PackVehicleModels.Length) return false;
            var modelName = PackVehicleModels[vehicleIndex];
            if (string.IsNullOrEmpty(modelName)) return false;

            var modelAsset = Resources.Load<GameObject>("VehicleModels/" + modelName);
            if (modelAsset == null) return false;

            var originalRotation = vehicleRoot.rotation;
            vehicleRoot.rotation = Quaternion.identity;

            var model = Instantiate(modelAsset, vehicleRoot, false);
            model.name = modelName + " model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Destroy(model);
                vehicleRoot.rotation = originalRotation;
                return false;
            }

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
                if (renderer != renderers[0]) bounds.Encapsulate(renderer.bounds);

            if (bounds.size.z <= 0.01f)
            {
                Destroy(model);
                vehicleRoot.rotation = originalRotation;
                return false;
            }

            var rootPosition = vehicleRoot.position;
            var localCenter = bounds.center - rootPosition;
            var localBottom = bounds.min.y - rootPosition.y;
            var scale = targetLength / bounds.size.z;
            model.transform.localScale = Vector3.one * scale;
            model.transform.localPosition = new Vector3(-localCenter.x * scale, -localBottom * scale, -localCenter.z * scale);

            bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
                if (renderer != renderers[0]) bounds.Encapsulate(renderer.bounds);

            collider.center = bounds.center - rootPosition;
            collider.size = bounds.size * 1.06f;

            var bodyRenderer = model.GetComponent<Renderer>();
            if (bodyRenderer == null)
            {
                foreach (var renderer in renderers)
                {
                    if (renderer.gameObject.name.Contains("Wheel")) continue;
                    bodyRenderer = renderer;
                    break;
                }
            }
            if (bodyRenderer != null) bodyRenderer.gameObject.name = displayName + " body paintable";

            vehicleRoot.rotation = originalRotation;
            return true;
        }

        private static void CreateVehicleBodyShell(Transform parent, string name, Material body, float bodyWidth, float bodyLength, float wheelBase, bool isVan)
        {
            var halfLength = bodyLength * 0.5f;
            var stations = new List<float> { -halfLength, -0.55f, 0f, 0.55f, halfLength };
            foreach (var direction in new[] { -1f, 1f })
            foreach (var offset in new[] { -0.58f, -0.42f, -0.25f, 0f, 0.25f, 0.42f, 0.58f })
                stations.Add(Mathf.Clamp(direction * wheelBase + offset, -halfLength, halfLength));
            stations.Sort();

            var uniqueStations = new List<float>(stations.Count);
            foreach (var station in stations)
                if (uniqueStations.Count == 0 || station - uniqueStations[uniqueStations.Count - 1] > 0.025f)
                    uniqueStations.Add(station);

            var vertices = new List<Vector3>(uniqueStations.Count * 6 + 2);
            var uv = new List<Vector2>(uniqueStations.Count * 6 + 2);
            var triangles = new List<int>(uniqueStations.Count * 36);
            var halfWidth = bodyWidth * 0.5f;
            for (var i = 0; i < uniqueStations.Count; i++)
            {
                var z = uniqueStations[i];
                var distanceToAxle = Mathf.Abs(Mathf.Abs(z) - wheelBase);
                var arch = 1f - Mathf.Clamp01(distanceToAxle / 0.58f);
                arch = arch * arch * (3f - 2f * arch);
                var bottomY = Mathf.Lerp(0.32f, 0.69f, arch);
                var endTaper = Mathf.Clamp01((Mathf.Abs(z) - (halfLength - 0.34f)) / 0.34f);
                bottomY = Mathf.Lerp(bottomY, 0.46f, endTaper);
                var shoulderY = Mathf.Max(0.72f, bottomY + 0.14f);
                var topY = 0.82f + 0.14f * Mathf.Clamp01((halfLength - Mathf.Abs(z)) / 0.65f);
                if (isVan) topY += 0.025f;

                var widthTaper = Mathf.Lerp(1f, 0.82f, endTaper);
                var sideWidth = halfWidth * widthTaper;
                var roofWidth = sideWidth * (isVan ? 0.94f : 0.89f);
                var section = new[]
                {
                    new Vector3(-sideWidth, bottomY, z),
                    new Vector3(sideWidth, bottomY, z),
                    new Vector3(sideWidth, shoulderY, z),
                    new Vector3(roofWidth, topY, z),
                    new Vector3(-roofWidth, topY, z),
                    new Vector3(-sideWidth, shoulderY, z)
                };
                foreach (var point in section)
                {
                    vertices.Add(point);
                    uv.Add(new Vector2(point.x / bodyWidth + 0.5f, point.z / bodyLength + 0.5f));
                }

                if (i == 0) continue;
                var previous = (i - 1) * 6;
                var current = i * 6;
                for (var edge = 0; edge < 6; edge++)
                {
                    var nextEdge = (edge + 1) % 6;
                    var a = previous + edge;
                    var b = previous + nextEdge;
                    var nextA = current + edge;
                    var nextB = current + nextEdge;
                    triangles.Add(a); triangles.Add(b); triangles.Add(nextB);
                    triangles.Add(a); triangles.Add(nextB); triangles.Add(nextA);
                }
            }

            var rearCenter = vertices.Count;
            vertices.Add(new Vector3(0f, 0.54f, -halfLength));
            uv.Add(new Vector2(0.5f, 0f));
            var rearStart = 0;
            for (var edge = 0; edge < 6; edge++)
            {
                triangles.Add(rearCenter);
                triangles.Add(rearStart + (edge + 1) % 6);
                triangles.Add(rearStart + edge);
            }

            var frontCenter = vertices.Count;
            vertices.Add(new Vector3(0f, 0.54f, halfLength));
            uv.Add(new Vector2(0.5f, 1f));
            var frontStart = (uniqueStations.Count - 1) * 6;
            for (var edge = 0; edge < 6; edge++)
            {
                triangles.Add(frontCenter);
                triangles.Add(frontStart + edge);
                triangles.Add(frontStart + (edge + 1) % 6);
            }

            var mesh = new Mesh { name = name + " chamfered body mesh" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var shell = new GameObject(name + " chamfered body paintable");
            shell.transform.SetParent(parent, false);
            shell.AddComponent<MeshFilter>().sharedMesh = mesh;
            shell.AddComponent<MeshRenderer>().sharedMaterial = body;
        }

        private static void CreateCabinFrameBar(Transform parent, string name, Vector3 start, Vector3 end, float width, Material material)
        {
            var direction = end - start;
            VisualPart(parent, PrimitiveType.Cube, name, (start + end) * 0.5f, new Vector3(width, direction.magnitude, width), material, Quaternion.FromToRotation(Vector3.up, direction.normalized));
        }

        private void CreateAlloyWheelFace(Transform parent, string vehicleName, float side, float wheelSide, float axle)
        {
            if (_alloyWheelFaceMesh == null)
                _alloyWheelFaceMesh = BuildAlloyWheelFaceMesh();

            var face = new GameObject(vehicleName + " alloy wheel face");
            face.transform.SetParent(parent, false);
            face.transform.localPosition = new Vector3(side * (wheelSide + 0.075f), 0.36f, axle);
            face.AddComponent<MeshFilter>().sharedMesh = _alloyWheelFaceMesh;
            face.AddComponent<MeshRenderer>().sharedMaterial = _alloy;
        }

        private static Mesh BuildAlloyWheelFaceMesh()
        {
            const int ringSegments = 24;
            const int spokeCount = 5;
            const float outerRadius = 0.17f;
            const float innerRingRadius = 0.132f;
            const float centerRadius = 0.044f;
            const float faceHalfThickness = 0.035f;
            var vertices = new List<Vector3>(220);
            var triangles = new List<int>(360);

            for (var face = 0; face < 2; face++)
            {
                var x = face == 0 ? -faceHalfThickness : faceHalfThickness;
                var winding = face == 0 ? 1 : -1;

                var outerStart = vertices.Count;
                for (var i = 0; i < ringSegments; i++)
                {
                    var angle = i * Mathf.PI * 2f / ringSegments;
                    vertices.Add(new Vector3(x, Mathf.Cos(angle) * outerRadius, Mathf.Sin(angle) * outerRadius));
                }
                var innerStart = vertices.Count;
                for (var i = 0; i < ringSegments; i++)
                {
                    var angle = i * Mathf.PI * 2f / ringSegments;
                    vertices.Add(new Vector3(x, Mathf.Cos(angle) * innerRingRadius, Mathf.Sin(angle) * innerRingRadius));
                }
                for (var i = 0; i < ringSegments; i++)
                {
                    var next = (i + 1) % ringSegments;
                    AddWheelFaceTriangle(triangles, outerStart + i, innerStart + i, outerStart + next, winding);
                    AddWheelFaceTriangle(triangles, outerStart + next, innerStart + i, innerStart + next, winding);
                }

                var center = vertices.Count;
                vertices.Add(new Vector3(x, 0f, 0f));
                var centerRingStart = vertices.Count;
                for (var i = 0; i < ringSegments; i++)
                {
                    var angle = i * Mathf.PI * 2f / ringSegments;
                    vertices.Add(new Vector3(x, Mathf.Cos(angle) * centerRadius, Mathf.Sin(angle) * centerRadius));
                }
                for (var i = 0; i < ringSegments; i++)
                    AddWheelFaceTriangle(triangles, center, centerRingStart + i, centerRingStart + (i + 1) % ringSegments, winding);

                for (var spoke = 0; spoke < spokeCount; spoke++)
                {
                    var angle = spoke * Mathf.PI * 2f / spokeCount;
                    var innerLeft = angle - 0.18f;
                    var innerRight = angle + 0.18f;
                    var outerLeft = angle - 0.105f;
                    var outerRight = angle + 0.105f;
                    var first = vertices.Count;
                    vertices.Add(new Vector3(x, Mathf.Cos(innerLeft) * centerRadius, Mathf.Sin(innerLeft) * centerRadius));
                    vertices.Add(new Vector3(x, Mathf.Cos(outerLeft) * 0.145f, Mathf.Sin(outerLeft) * 0.145f));
                    vertices.Add(new Vector3(x, Mathf.Cos(outerRight) * 0.145f, Mathf.Sin(outerRight) * 0.145f));
                    vertices.Add(new Vector3(x, Mathf.Cos(innerRight) * centerRadius, Mathf.Sin(innerRight) * centerRadius));
                    AddWheelFaceTriangle(triangles, first, first + 1, first + 2, winding);
                    AddWheelFaceTriangle(triangles, first, first + 2, first + 3, winding);
                }
            }

            var mesh = new Mesh { name = "Five spoke alloy wheel face" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddWheelFaceTriangle(List<int> triangles, int first, int second, int third, int winding)
        {
            triangles.Add(first);
            triangles.Add(winding > 0 ? second : third);
            triangles.Add(winding > 0 ? third : second);
        }

        private static void CreateSedanCabin(Transform parent, string name, Material body, bool sportCoupe)
        {
            var roofHeight = sportCoupe ? 1.46f : 1.63f;
            var vertices = new[]
            {
                new Vector3(-0.78f, 0.96f, -1.12f), new Vector3(0.78f, 0.96f, -1.12f),
                new Vector3(0.78f, 0.96f, 0.98f), new Vector3(-0.78f, 0.96f, 0.98f),
                new Vector3(sportCoupe ? -0.58f : -0.64f, roofHeight, sportCoupe ? -0.72f : -0.82f),
                new Vector3(sportCoupe ? 0.58f : 0.64f, roofHeight, sportCoupe ? -0.72f : -0.82f),
                new Vector3(sportCoupe ? 0.58f : 0.64f, roofHeight, sportCoupe ? 0.36f : 0.46f),
                new Vector3(sportCoupe ? -0.58f : -0.64f, roofHeight, sportCoupe ? 0.36f : 0.46f)
            };
            CreateCabinShellMesh(parent, name, body, vertices);
        }

        private static void CreateExoticCabin(Transform parent, string name, Material body)
        {
            CreateCabinShellMesh(parent, name, body, new[]
            {
                new Vector3(-0.78f, 0.96f, -1.12f), new Vector3(0.78f, 0.96f, -1.12f),
                new Vector3(0.78f, 0.96f, 0.98f), new Vector3(-0.78f, 0.96f, 0.98f),
                new Vector3(-0.56f, 1.34f, -0.72f), new Vector3(0.56f, 1.34f, -0.72f),
                new Vector3(0.56f, 1.34f, 0.38f), new Vector3(-0.56f, 1.34f, 0.38f)
            });
        }

        private static void CreateCabinShellMesh(Transform parent, string name, Material body, Vector3[] vertices)
        {
            var triangles = new[]
            {
                3, 2, 6, 3, 6, 7,
                1, 0, 4, 1, 4, 5,
                0, 3, 7, 0, 7, 4,
                2, 1, 5, 2, 5, 6,
                4, 6, 5, 4, 7, 6,
                0, 1, 2, 0, 2, 3
            };
            var mesh = new Mesh { name = name + " cabin shell mesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.2f), new Vector2(0.9f, 0.8f), new Vector2(0.1f, 0.8f)
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var shell = new GameObject(name + " cabin shell paintable");
            shell.transform.SetParent(parent, false);
            shell.AddComponent<MeshFilter>().sharedMesh = mesh;
            shell.AddComponent<MeshRenderer>().sharedMaterial = body;
        }

        private static void CreateVanCabin(Transform parent, string name, Material body)
        {
            CreateCabinShellMesh(parent, name, body, new[]
            {
                new Vector3(-0.84f, 0.93f, 0.22f), new Vector3(0.84f, 0.93f, 0.22f),
                new Vector3(0.84f, 0.93f, 1.43f), new Vector3(-0.84f, 0.93f, 1.43f),
                new Vector3(-0.76f, 1.68f, 0.28f), new Vector3(0.76f, 1.68f, 0.28f),
                new Vector3(0.76f, 1.68f, 1.04f), new Vector3(-0.76f, 1.68f, 1.04f)
            });
        }

        private static void CreatePickupCab(Transform parent, string name, Material body)
        {
            CreateCabinShellMesh(parent, name, body, new[]
            {
                new Vector3(-0.89f, 0.96f, -0.16f), new Vector3(0.89f, 0.96f, -0.16f),
                new Vector3(0.89f, 0.96f, 1.35f), new Vector3(-0.89f, 0.96f, 1.35f),
                new Vector3(-0.82f, 1.76f, -0.03f), new Vector3(0.82f, 1.76f, -0.03f),
                new Vector3(0.82f, 1.76f, 1.13f), new Vector3(-0.82f, 1.76f, 1.13f)
            });
        }

        private static void CreateHatchbackCabin(Transform parent, string name, Material body)
        {
            CreateCabinShellMesh(parent, name, body, new[]
            {
                new Vector3(-0.84f, 0.96f, -0.94f), new Vector3(0.84f, 0.96f, -0.94f),
                new Vector3(0.84f, 0.96f, 0.82f), new Vector3(-0.84f, 0.96f, 0.82f),
                new Vector3(-0.73f, 1.67f, -0.67f), new Vector3(0.73f, 1.67f, -0.67f),
                new Vector3(0.73f, 1.67f, 0.50f), new Vector3(-0.73f, 1.67f, 0.50f)
            });
        }

        private void CreateHumanoidVisual(Transform parent, Material top, Material trousers, Material skin)
        {
            var residentPrefab = Resources.Load<GameObject>("Characters/HarborlightResident");
            if (residentPrefab != null)
            {
                var resident = Instantiate(residentPrefab, parent, false);
                resident.name = "Harborlight resident model";
                foreach (var renderer in resident.GetComponentsInChildren<Renderer>(true))
                {
                    var rendererName = renderer.name;
                    var isSuit = rendererName.IndexOf("male_casualsuit01", StringComparison.OrdinalIgnoreCase) >= 0;
                    var isBody = rendererName.IndexOf("harborlightresident", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!isSuit && !isBody) continue;
                    var properties = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(properties);
                    var tint = isSuit
                        ? Color.Lerp(Color.white, top.color, 0.08f)
                        : Color.Lerp(Color.white, skin.color, 0.2f);
                    properties.SetColor(Shader.PropertyToID("_BaseColor"), tint);
                    properties.SetColor(Shader.PropertyToID("_Color"), tint);
                    renderer.SetPropertyBlock(properties);
                }
                return;
            }

            VisualPart(parent, PrimitiveType.Capsule, "Character torso", new Vector3(0f, 0.98f, 0f), new Vector3(0.36f, 0.45f, 0.23f), top);
            VisualPart(parent, PrimitiveType.Cube, "Character belt", new Vector3(0f, 0.57f, 0f), new Vector3(0.43f, 0.13f, 0.28f), _dark);
            VisualPart(parent, PrimitiveType.Sphere, "Character head", new Vector3(0f, 1.55f, 0f), new Vector3(0.34f, 0.4f, 0.33f), skin);
            VisualPart(parent, PrimitiveType.Sphere, "Character hair", new Vector3(0f, 1.71f, -0.01f), new Vector3(0.35f, 0.17f, 0.34f), _dark);

            foreach (var side in new[] { -1f, 1f })
            {
                VisualPart(parent, PrimitiveType.Capsule, "Character arm", new Vector3(side * 0.39f, 0.88f, 0f), new Vector3(0.13f, 0.34f, 0.13f), top, Quaternion.Euler(0f, 0f, -side * 7f));
                VisualPart(parent, PrimitiveType.Capsule, "Character hand", new Vector3(side * 0.42f, 0.56f, 0f), new Vector3(0.1f, 0.12f, 0.1f), skin);
                VisualPart(parent, PrimitiveType.Capsule, "Character leg", new Vector3(side * 0.16f, 0.34f, 0f), new Vector3(0.15f, 0.33f, 0.16f), trousers);
                VisualPart(parent, PrimitiveType.Cube, "Character shoe", new Vector3(side * 0.16f, 0.08f, 0.06f), new Vector3(0.26f, 0.15f, 0.38f), _dark);
            }
        }

        private static GameObject VisualPart(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material)
        {
            return VisualPart(parent, type, name, position, scale, material, Quaternion.identity);
        }

        private static GameObject VisualPart(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material, Quaternion rotation)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation = rotation;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(part.GetComponent<Collider>());
            return part;
        }

        private void CreateBeacon(Vector3 position)
        {
            Block("Tide beacon plinth", position + new Vector3(0f, 0.8f, 0f), new Vector3(2.8f, 1.6f, 2.8f), _sidewalk);
            var mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mast.name = "Tide beacon mast";
            mast.transform.position = position + new Vector3(0f, 5.4f, 0f);
            mast.transform.localScale = new Vector3(0.32f, 4.5f, 0.32f);
            mast.GetComponent<Renderer>().sharedMaterial = _accent;
            var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.name = "Beacon lamp";
            lamp.transform.position = position + new Vector3(0f, 9.8f, 0f);
            lamp.transform.localScale = Vector3.one * 1.15f;
            lamp.GetComponent<Renderer>().sharedMaterial = _window;
            var light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.75f, 0.41f);
            light.range = 10f;
            light.intensity = 0.85f;

            var trigger = new GameObject("Tide beacon controls");
            trigger.transform.position = position + new Vector3(0f, 1f, -2.7f);
            var interactable = trigger.AddComponent<HarborlightInteractable>();
            interactable.Action = WorldAction.RepairBeacon;
            interactable.DisplayName = "Tide beacon controls";
            trigger.AddComponent<HarborlightPoliceDispatchSite>().DispatchCode = "beacon-safety";
            FloatingLabel("TIDE BEACON", position + new Vector3(0f, 2f, 2.2f));
        }

        private void CreateStreetlight(Vector3 position)
        {
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Harbor streetlight";
            pole.transform.position = position + new Vector3(0f, 2.1f, 0f);
            pole.transform.localScale = new Vector3(0.12f, 2.1f, 0.12f);
            pole.GetComponent<Renderer>().sharedMaterial = _accent;
            var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.name = "Streetlight lantern";
            lamp.transform.position = position + new Vector3(0f, 4.3f, 0f);
            lamp.transform.localScale = Vector3.one * 0.38f;
            lamp.GetComponent<Renderer>().sharedMaterial = _window;
        }

        private void FloatingLabel(string text, Vector3 position)
        {
            var label = new GameObject(text + " label");
            label.transform.position = position;
            var mesh = label.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = 32;
            mesh.characterSize = 0.055f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = new Color(0.95f, 0.88f, 0.68f);
        }

        private static GameObject Block(string name, Vector3 position, Vector3 size, Material material)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.position = position;
            block.transform.localScale = size;
            block.GetComponent<Renderer>().sharedMaterial = material;
            return block;
        }

        private static Texture2D NoiseTexture(float variation, int seed)
        {
            const int resolution = 128;
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
            var random = new System.Random(seed);
            for (var y = 0; y < resolution; y++)
            for (var x = 0; x < resolution; x++)
            {
                var value = Mathf.Clamp01(0.94f + ((float)random.NextDouble() - 0.5f) * variation);
                texture.SetPixel(x, y, new Color(value, value, value, 1f));
            }
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            texture.Apply();
            return texture;
        }

        private static void ApplyTexture(Material material, string resourcePath, Vector2 scale, float fallbackVariation, int seed)
        {
            if (material == null) return;
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null) texture = NoiseTexture(fallbackVariation, seed);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = 4;
            material.mainTexture = texture;
            material.mainTextureScale = scale;
        }

        private static void ApplyPbrTexture(Material material, string albedoName, string normalName, Vector2 scale, float normalStrength)
        {
            if (material == null) return;
            var folder = "Art/Textures/PolyHaven/";
            var albedo = Resources.Load<Texture2D>(folder + albedoName);
            var normal = Resources.Load<Texture2D>(folder + normalName);
            if (albedo != null)
            {
                albedo.wrapMode = TextureWrapMode.Repeat;
                albedo.filterMode = FilterMode.Trilinear;
                albedo.anisoLevel = 8;
                material.mainTexture = albedo;
                material.mainTextureScale = scale;
            }
            if (normal != null && material.HasProperty("_BumpMap"))
            {
                normal.wrapMode = TextureWrapMode.Repeat;
                normal.filterMode = FilterMode.Trilinear;
                normal.anisoLevel = 8;
                material.SetTexture("_BumpMap", normal);
                material.SetFloat("_BumpScale", normalStrength);
                material.EnableKeyword("_NORMALMAP");
            }
        }

        private static void SetSurface(Material material, float metallic, float smoothness)
        {
            if (material == null) return;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        }

        private static void SetEmission(Material material, Color color, float intensity)
        {
            if (material == null || !material.HasProperty("_EmissionColor")) return;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * intensity);
        }

        private static Material Material(string name, Color color)
        {
            if (_runtimeMaterialTemplate == null)
                _runtimeMaterialTemplate = Resources.Load<Material>("HarborlightRuntimeMaterial");
            if (_runtimeMaterialTemplate == null)
            {
                Debug.LogError("Missing Assets/Resources/HarborlightRuntimeMaterial.mat.");
                return null;
            }

            var material = new Material(_runtimeMaterialTemplate) { name = name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else material.color = color;
            return material;
        }
    }
}
