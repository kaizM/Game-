using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Harborlight
{
    public enum ContractStage
    {
        Available,
        InProgress,
        Complete
    }

    public enum CourierStage
    {
        Available,
        AwaitingParcel,
        CarryingParcel,
        Complete
    }

    public enum InspectionStage
    {
        Available,
        Inspecting,
        ReadyToReport,
        Complete
    }

    public enum WorldAction
    {
        AcceptBeaconRepair,
        RepairBeacon,
        AcceptCourierDelivery,
        CollectCourierParcel,
        DeliverCourierParcel,
        AcceptDockInspection,
        InspectDock,
        ReportDockInspection,
        OpenGarage,
        EnterVehicle,
        Talk,
        OpenBank
    }

    [Serializable]
    public sealed class HarborlightBankTransaction
    {
        public string direction;
        public int amount;
        public int cashDelta;
        public int bankDelta;
        public int cashBalanceAfter;
        public int bankBalanceAfter;
        public string createdAtUtc;
    }

    public sealed class HarborlightGameState : MonoBehaviour
    {
        private static string LegacySaveKey => HarborlightLocalProfileStorage.PlayerPrefsKey("harborlight-save-v1");
        private const int SaveSchemaVersion = 10;
        public const int VehicleCatalogSize = 8;
        private static string SavePath => Path.Combine(HarborlightLocalProfileStorage.PersistentDataPath, "harborlight-save-v10.json");
        private static string PreviousSavePathV9 => Path.Combine(HarborlightLocalProfileStorage.PersistentDataPath, "harborlight-save-v9.json");
        private static string PreviousSavePathV8 => Path.Combine(HarborlightLocalProfileStorage.PersistentDataPath, "harborlight-save-v8.json");
        private static string PreviousSavePathV7 => Path.Combine(HarborlightLocalProfileStorage.PersistentDataPath, "harborlight-save-v7.json");
        private static string PreviousSavePathV6 => Path.Combine(HarborlightLocalProfileStorage.PersistentDataPath, "harborlight-save-v6.json");
        private static string PreviousSavePathV5 => Path.Combine(HarborlightLocalProfileStorage.PersistentDataPath, "harborlight-save-v5.json");
        private static string PreviousSavePathV4 => Path.Combine(HarborlightLocalProfileStorage.PersistentDataPath, "harborlight-save-v4.json");
        private static string PreviousSavePathV3 => Path.Combine(HarborlightLocalProfileStorage.PersistentDataPath, "harborlight-save-v3.json");
        private static string PreviousSavePathV2 => Path.Combine(HarborlightLocalProfileStorage.PersistentDataPath, "harborlight-save-v2.json");

        [Serializable]
        private sealed class SaveData
        {
            public int schemaVersion = SaveSchemaVersion;
            public int credits = 125;
            public int bankCredits;
            public HarborlightBankTransaction[] bankTransactions = new HarborlightBankTransaction[0];
            public int reputation;
            public ContractStage contractStage;
            public CourierStage courierStage;
            public InspectionStage inspectionStage;
            public int deliveriesCompleted;
            public int selectedVehicleIndex;
            public bool[] ownedVehicles = new[] { true, false, false, false, false, false, false, false };
            public string[] vehiclePaintSkus = new string[VehicleCatalogSize];
            public string taxiPaintSku = "";
            public string sedanPaintSku = "";
            public string vanPaintSku = "";
            public string[] ownedCosmeticSkus = new string[0];
            public bool tutorialCompleted;
            public float playerX;
            public float playerZ;
            public bool wasDriving;
            public int drivenVehicleIndex = -1;
            public float vehicleX;
            public float vehicleZ;
            public float vehicleHeading;
            public bool policeApplied;
            public bool policeOnDuty;
            public int policeCallsCompleted;
            public int policeCreditsEarned;
            public string policeActiveDispatchCode = "";
        }

        public static readonly Vector3 BeaconPosition = new Vector3(0f, 0f, 33f);
        public static readonly Vector3 CourierContactPosition = new Vector3(-32f, 0f, -12f);
        public static readonly Vector3 CourierParcelPosition = new Vector3(-18f, 0f, -12f);
        public static readonly Vector3 CourierDropoffPosition = new Vector3(-18f, 0f, 29f);
        public static readonly Vector3 DockInspectionPosition = new Vector3(31f, 0f, 27f);
        public static readonly Vector3 GaragePosition = new Vector3(30f, 0f, -2f);

        public int Credits { get; private set; } = 125;
        public int BankCredits { get; private set; }
        public int Reputation { get; private set; }
        public ContractStage CurrentContract { get; private set; }
        public CourierStage CurrentCourierStage { get; private set; }
        public InspectionStage CurrentInspectionStage { get; private set; }
        public int DeliveriesCompleted { get; private set; }
        public bool PoliceApplied { get; private set; }
        public bool PoliceOnDuty { get; private set; }
        public int PoliceCallsCompleted { get; private set; }
        public int PoliceCreditsEarned { get; private set; }
        public string PoliceActiveDispatchCode { get; private set; } = "";
        public bool IsEligibleForPoliceCareer => DisplayContract == ContractStage.Complete &&
            DisplayCourierStage == CourierStage.Complete && DisplayInspectionStage == InspectionStage.Complete &&
            DisplayReputation >= 4;
        public int DisplayCredits => _hasNetworkCashSnapshot ? _networkCredits : Credits;
        public int DisplayBankCredits => _hasNetworkBankSnapshot ? _networkBankCredits : BankCredits;
        public int DisplayReputation => _hasNetworkSnapshot ? _networkReputation : Reputation;
        public int DisplayDeliveriesCompleted => _hasNetworkSnapshot ? _networkDeliveriesCompleted : DeliveriesCompleted;
        public ContractStage DisplayContract => _hasNetworkSnapshot ? _networkContract : CurrentContract;
        public CourierStage DisplayCourierStage => _hasNetworkSnapshot ? _networkCourier : CurrentCourierStage;
        public InspectionStage DisplayInspectionStage => _hasNetworkSnapshot ? _networkInspection : CurrentInspectionStage;
        public int SelectedVehicleIndex { get; private set; }
        public bool[] OwnedVehicles { get; private set; } = new[] { true, false, false, false, false, false, false, false };
        public string[] OwnedCosmeticSkus { get; private set; } = new string[0];
        public bool GarageOpen { get; private set; }
        public bool BankOpen { get; private set; }
        public bool InventoryOpen { get; private set; }
        public bool PoliceCareerOpen { get; private set; }
        public bool TutorialOpen => !_tutorialCompleted;
        public Transform Player { get; private set; }
        public Vector2 LoadedPlayerPosition => new Vector2(_loadedX, _loadedZ);
        public string LastMessage { get; private set; } = "The tide beacons are dim. Check in with Mara at the pier.";
        public string CurrentLocation => Player == null ? "Unknown"
            : Player.position.z < -90f && Player.position.x < -20f ? "Cinder Park"
            : Player.position.z < -90f && Player.position.x > 18f ? "Breakwater Depot"
            : Player.position.z < -90f ? "Lantern Ridge"
            : Player.position.z < -45f && Player.position.x < -64f ? "West Quay"
            : Player.position.z < -45f && Player.position.x > 68f ? "East Quay"
            : Player.position.z < -45f && Player.position.x < -20f ? "Alder Row"
            : Player.position.z < -45f ? "Switchyard"
            : Player.position.z > 22 && Player.position.x < -10 ? "Old Ferry Office"
            : Player.position.z > 22 ? "North Pier"
            : Player.position.x < -24 && Player.position.z < -8 ? "South Market"
            : Player.position.x < -10 && Player.position.z < -8 ? "Net Loft Lane"
            : Player.position.x > 12 ? "Foundry Row"
            : "Lantern Square";
        public Vector3 CourierObjectivePosition => DisplayCourierStage == CourierStage.Available || DisplayCourierStage == CourierStage.Complete
            ? CourierContactPosition
            : DisplayCourierStage == CourierStage.AwaitingParcel ? CourierParcelPosition : CourierDropoffPosition;
        public Vector3 InspectionObjectivePosition => DisplayInspectionStage == InspectionStage.Inspecting
            ? DockInspectionPosition : new Vector3(23f, 0f, 8f);
        public string[] RecentAdminActions => _adminActions.ToArray();
        public IReadOnlyList<HarborlightBankTransaction> LocalBankTransactions => _bankTransactions;

        private float _saveTimer;
        private readonly List<string> _adminActions = new List<string>();
        private readonly List<HarborlightBankTransaction> _bankTransactions = new List<HarborlightBankTransaction>();
        private string[] _vehiclePaintSkus = new string[VehicleCatalogSize];
        private bool _tutorialCompleted;
        private bool _hasNetworkSnapshot;
        private bool _hasNetworkCashSnapshot;
        private bool _hasNetworkBankSnapshot;
        private int _networkBankCredits;
        private int _networkCredits;
        private int _networkReputation;
        private int _networkDeliveriesCompleted;
        private ContractStage _networkContract;
        private CourierStage _networkCourier;
        private InspectionStage _networkInspection;
        private bool _resumeInVehicle;
        private int _resumeVehicleIndex = -1;
        private Vector3 _resumeVehiclePosition;
        private float _resumeVehicleHeading;

        public void ApplyNetworkProgressSnapshot(int beaconStage, int courierStage, int inspectionStage,
            int credits, int reputation, int deliveriesCompleted, string message)
        {
            _networkContract = (ContractStage)Mathf.Clamp(beaconStage, 0, (int)ContractStage.Complete);
            _networkCourier = (CourierStage)Mathf.Clamp(courierStage, 0, (int)CourierStage.Complete);
            _networkInspection = (InspectionStage)Mathf.Clamp(inspectionStage, 0, (int)InspectionStage.Complete);
            _networkCredits = Mathf.Clamp(credits, 0, 999999);
            _hasNetworkCashSnapshot = true;
            _networkReputation = Mathf.Clamp(reputation, 0, 999);
            _networkDeliveriesCompleted = Mathf.Clamp(deliveriesCompleted, 0, 999999);
            _hasNetworkSnapshot = true;
            if (!string.IsNullOrWhiteSpace(message)) SetMessage(message);
        }

        public void ClearNetworkProgressSnapshot()
        {
            _hasNetworkSnapshot = false;
            _hasNetworkCashSnapshot = false;
            _hasNetworkBankSnapshot = false;
        }

        public void ApplyNetworkBankSnapshot(int cashCredits, int bankCredits)
        {
            _networkCredits = Mathf.Clamp(cashCredits, 0, 999999);
            _networkBankCredits = Mathf.Clamp(bankCredits, 0, 999999);
            _hasNetworkCashSnapshot = true;
            _hasNetworkSnapshot = true;
            _hasNetworkBankSnapshot = true;
        }

        public void ApplyNetworkCashSnapshot(int cashCredits)
        {
            _networkCredits = Mathf.Clamp(cashCredits, 0, 999999);
            _hasNetworkCashSnapshot = true;
        }

        private void Awake()
        {
            Load();
            HarborlightTouchControls.SetUiBlocked(TutorialOpen);
        }

        public void BindPlayer(Transform player, bool restoreSavedPosition = true)
        {
            Player = player;
            if (Player != null && restoreSavedPosition)
            {
                var savedPosition = new Vector3(_loadedX,
                    HarborlightDistrictSurfaceNetwork.TerrainHeight(_loadedZ) + 0.02f, _loadedZ);
                if (IsPlayerSpawnBlocked(savedPosition, Player.GetComponent<CharacterController>()))
                {
                    savedPosition = new Vector3(0f,
                        HarborlightDistrictSurfaceNetwork.TerrainHeight(-2f) + 0.02f, -2f);
                    LastMessage = "Saved position was obstructed. Returned to Lantern Square.";
                }
                Player.position = savedPosition;
            }
        }

        private static bool IsPlayerSpawnBlocked(Vector3 position, CharacterController controller)
        {
            Physics.SyncTransforms();
            var height = controller != null ? controller.height : 1.8f;
            var radius = controller != null ? controller.radius : 0.34f;
            var center = controller != null ? controller.center : new Vector3(0f, 0.9f, 0f);
            var capsuleOffset = Mathf.Max(0f, height * 0.5f - radius);
            var bottom = position + center - Vector3.up * capsuleOffset;
            var top = position + center + Vector3.up * capsuleOffset;
            var overlaps = Physics.OverlapCapsule(bottom, top, radius * 0.95f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            foreach (var overlap in overlaps)
            {
                if (overlap == null || overlap == controller) continue;
                if (IsDistrictWalkableSurface(overlap)) continue;
                if (overlap.bounds.max.y <= position.y + 0.2f) continue;
                return true;
            }
            return false;
        }

        private static bool IsDistrictWalkableSurface(Collider collider)
        {
            if (!(collider is MeshCollider)) return false;
            var name = collider.gameObject.name;
            return name == "Northshore playable ground"
                || name == "Playable district road grid"
                || name == "Playable district textured sidewalks";
        }

        public bool TryConsumeVehicleResume(out int vehicleIndex, out Vector3 position, out float heading)
        {
            vehicleIndex = _resumeVehicleIndex;
            position = _resumeVehiclePosition;
            heading = _resumeVehicleHeading;
            var shouldResume = _resumeInVehicle;
            _resumeInVehicle = false;
            _resumeVehicleIndex = -1;
            return shouldResume;
        }

        private float _loadedX;
        private float _loadedZ = -2f;

        private void Update()
        {
            _saveTimer += Time.deltaTime;
            if (_saveTimer < 8f) return;
            _saveTimer = 0f;
            Save();
        }

        public string PromptFor(WorldAction action)
        {
            switch (action)
            {
                case WorldAction.AcceptBeaconRepair:
                    return DisplayContract == ContractStage.Available ? "E  ·  Take the beacon repair contract" : "E  ·  Talk to Mara";
                case WorldAction.RepairBeacon:
                    if (DisplayContract == ContractStage.InProgress) return "E  ·  Restore the tide beacon";
                    return DisplayContract == ContractStage.Available ? "E  ·  Beacon controls are locked" : "E  ·  Inspect the restored beacon";
                case WorldAction.AcceptCourierDelivery:
                    return DisplayCourierStage == CourierStage.AwaitingParcel || DisplayCourierStage == CourierStage.CarryingParcel
                        ? "E  ·  Check in with the courier"
                        : "E  ·  Start a parcel run";
                case WorldAction.CollectCourierParcel:
                    return DisplayCourierStage == CourierStage.AwaitingParcel ? "E  ·  Collect the market parcel" : "E  ·  Inspect parcel crate";
                case WorldAction.DeliverCourierParcel:
                    return DisplayCourierStage == CourierStage.CarryingParcel ? "E  ·  Deliver the parcel" : "E  ·  Delivery bay";
                case WorldAction.AcceptDockInspection:
                    return DisplayInspectionStage == InspectionStage.Available ? "E  ·  Take the pier inspection"
                        : DisplayInspectionStage == InspectionStage.ReadyToReport ? "E  ·  Report pier conditions" : "E  ·  Check in with Oren";
                case WorldAction.InspectDock:
                    return DisplayInspectionStage == InspectionStage.Inspecting ? "E  ·  Inspect the pier circuit" : "E  ·  Pier inspection point";
                case WorldAction.ReportDockInspection:
                    return DisplayInspectionStage == InspectionStage.ReadyToReport ? "E  ·  Report pier conditions" : "E  ·  Inspect the pier circuit first";
                case WorldAction.EnterVehicle:
                    return "E  ·  Drive vehicle";
                case WorldAction.OpenGarage:
                    return "E  ·  Open the neighborhood garage";
                case WorldAction.OpenBank:
                    return "E  ·  Use the Harbor Bank ATM";
                default:
                    return "E  ·  Talk";
            }
        }

        public void Interact(WorldAction action)
        {
            if (action == WorldAction.AcceptBeaconRepair)
            {
                if (CurrentContract == ContractStage.Available)
                {
                    CurrentContract = ContractStage.InProgress;
                    SetMessage("Mara asked you to relight the old tide beacon at the end of North Pier.");
                }
                else
                {
                    SetMessage("Mara: The beacon keeps the night ferries clear of the outer rocks.");
                }
            }
            else if (action == WorldAction.RepairBeacon)
            {
                if (CurrentContract == ContractStage.InProgress)
                {
                    CurrentContract = ContractStage.Complete;
                    Credits += 85;
                    Reputation += 1;
                    SetMessage("Beacon restored. You earned 85 credits and a little trust around the harbor.");
                }
                else if (CurrentContract == ContractStage.Available)
                {
                    SetMessage("The beacon controls are sealed. Ask Mara for the repair contract first.");
                }
                else
                {
                    SetMessage("The tide beacon sweeps a steady line across the water.");
                }
            }
            else if (action == WorldAction.AcceptCourierDelivery)
            {
                if (CurrentCourierStage == CourierStage.Available || CurrentCourierStage == CourierStage.Complete)
                {
                    CurrentCourierStage = CourierStage.AwaitingParcel;
                    SetMessage("Lio needs a sealed market parcel taken to the Old Ferry Office.");
                }
                else
                {
                    SetMessage(CurrentCourierStage == CourierStage.CarryingParcel
                        ? "Lio: The Old Ferry Office is waiting for that parcel."
                        : "Lio: Pick up the parcel by Net Loft Lane first.");
                }
            }
            else if (action == WorldAction.CollectCourierParcel)
            {
                if (CurrentCourierStage == CourierStage.AwaitingParcel)
                {
                    CurrentCourierStage = CourierStage.CarryingParcel;
                    SetMessage("Parcel collected. Take it to the delivery bay beside the Old Ferry Office.");
                }
                else
                {
                    SetMessage(CurrentCourierStage == CourierStage.CarryingParcel
                        ? "You already have the parcel. Deliver it to the Old Ferry Office."
                        : "There is no parcel assigned to you. Talk to Lio to start a run.");
                }
            }
            else if (action == WorldAction.DeliverCourierParcel)
            {
                if (CurrentCourierStage == CourierStage.CarryingParcel)
                {
                    CurrentCourierStage = CourierStage.Complete;
                    DeliveriesCompleted = Mathf.Min(999999, DeliveriesCompleted + 1);
                    Credits = Mathf.Min(999999, Credits + 120);
                    Reputation += 2;
                    SetMessage("Delivery complete. You earned 120 credits and 2 reputation.");
                }
                else
                {
                    SetMessage("The delivery bay is ready when you have a parcel.");
                }
            }
            else if (action == WorldAction.AcceptDockInspection)
            {
                if (CurrentInspectionStage == InspectionStage.Available)
                {
                    CurrentInspectionStage = InspectionStage.Inspecting;
                    SetMessage("Oren asked you to check the warning circuit at the east end of the pier.");
                }
                else if (CurrentInspectionStage == InspectionStage.ReadyToReport)
                {
                    CurrentInspectionStage = InspectionStage.Complete;
                    Credits = Mathf.Min(999999, Credits + 95);
                    Reputation = Mathf.Min(999, Reputation + 1);
                    SetMessage("Pier circuit checked. Oren paid 95 credits and added 1 reputation.");
                }
                else
                {
                    SetMessage(CurrentInspectionStage == InspectionStage.Inspecting
                        ? "Oren: Check the warning circuit at the east end of the pier."
                        : "Oren: The east pier circuit is holding steady.");
                }
            }
            else if (action == WorldAction.InspectDock || action == WorldAction.ReportDockInspection)
            {
                if (CurrentInspectionStage == InspectionStage.Inspecting)
                {
                    CurrentInspectionStage = InspectionStage.ReadyToReport;
                    SetMessage("The warning circuit is clear. Report the inspection to Oren.");
                }
                else if (CurrentInspectionStage == InspectionStage.ReadyToReport)
                {
                    SetMessage("Return to Oren at Lantern Square and report the clear circuit.");
                }
                else
                {
                    SetMessage("Oren has not assigned a pier inspection.");
                }
            }
            else if (action == WorldAction.OpenGarage)
            {
                OpenGarage();
            }
            else if (action == WorldAction.OpenBank)
            {
                OpenBank();
            }
            else
            {
                SetMessage("The harbor is quiet tonight. Lantern Square is just waking up.");
            }
            Save();
        }

        public bool ExecuteAdminAction(string actionId, string payload, out string result)
        {
            switch (actionId)
            {
                case "credits":
                    if (!int.TryParse(payload, out var amount) || amount < 1 || amount > 5000)
                    {
                        result = "Credit amount must be from 1 to 5,000.";
                        return false;
                    }
                    Credits = Mathf.Min(999999, Credits + amount);
                    result = "Added " + amount + " credits.";
                    break;
                case "reputation":
                    Reputation = Mathf.Min(999, Reputation + 5);
                    result = "Added 5 reputation.";
                    break;
                case "teleport_square":
                    MovePlayer(new Vector3(0f, 0f, -2f));
                    result = "Moved player to Lantern Square.";
                    break;
                case "teleport_pier":
                    MovePlayer(new Vector3(0f, 0f, 28f));
                    result = "Moved player to North Pier.";
                    break;
                case "reset_contract":
                    CurrentContract = ContractStage.Available;
                    result = "Beacon repair contract reset.";
                    break;
                case "reset_courier":
                    CurrentCourierStage = CourierStage.Available;
                    result = "Courier job reset.";
                    break;
                case "reset_inspection":
                    CurrentInspectionStage = InspectionStage.Available;
                    result = "Pier inspection reset.";
                    break;
                case "reset_save":
                    PlayerPrefs.DeleteKey(LegacySaveKey);
                    if (File.Exists(SavePath)) File.Delete(SavePath);
                    if (File.Exists(PreviousSavePathV9)) File.Delete(PreviousSavePathV9);
                    if (File.Exists(PreviousSavePathV7)) File.Delete(PreviousSavePathV7);
                    if (File.Exists(PreviousSavePathV6)) File.Delete(PreviousSavePathV6);
                    if (File.Exists(PreviousSavePathV5)) File.Delete(PreviousSavePathV5);
                    if (File.Exists(PreviousSavePathV4)) File.Delete(PreviousSavePathV4);
                    if (File.Exists(PreviousSavePathV3)) File.Delete(PreviousSavePathV3);
                    if (File.Exists(PreviousSavePathV2)) File.Delete(PreviousSavePathV2);
                    Credits = 125;
                    Reputation = 0;
                    CurrentContract = ContractStage.Available;
                    CurrentCourierStage = CourierStage.Available;
                    CurrentInspectionStage = InspectionStage.Available;
                    DeliveriesCompleted = 0;
                    PoliceApplied = false;
                    PoliceOnDuty = false;
                    PoliceCallsCompleted = 0;
                    PoliceCreditsEarned = 0;
                    PoliceActiveDispatchCode = "";
                    SelectedVehicleIndex = 0;
                    OwnedVehicles = new[] { true, false, false, false, false, false, false, false };
                    _vehiclePaintSkus = new string[VehicleCatalogSize];
                    OwnedCosmeticSkus = new string[0];
                    _tutorialCompleted = false;
                    GarageOpen = false;
                    HarborlightTouchControls.SetUiBlocked(false);
                    MovePlayer(new Vector3(0f, 0f, -2f));
                    result = "Local game progress reset.";
                    break;
                default:
                    result = "This game has no admin action named " + actionId + ".";
                    return false;
            }

            LastMessage = "Admin: " + result;
            _adminActions.Insert(0, DateTime.Now.ToString("HH:mm:ss") + " · " + actionId + " · " + result);
            if (_adminActions.Count > 5) _adminActions.RemoveAt(_adminActions.Count - 1);
            Save();
            return true;
        }

        public string ContractStatus()
        {
            switch (DisplayContract)
            {
                case ContractStage.InProgress: return "Repair the tide beacon at North Pier.";
                case ContractStage.Complete: return "Beacon repaired · contract complete.";
                default: return "Meet Mara at Lantern Square for a harbor contract.";
            }
        }

        public string CourierStatus()
        {
            switch (DisplayCourierStage)
            {
                case CourierStage.AwaitingParcel: return "Pick up parcel by Net Loft Lane.";
                case CourierStage.CarryingParcel: return "Deliver parcel to Old Ferry Office.";
                case CourierStage.Complete: return "Run complete · talk to Lio for another.";
                default: return "Talk to Lio at the south market.";
            }
        }

        public string InspectionStatus()
        {
            switch (DisplayInspectionStage)
            {
                case InspectionStage.Inspecting: return "Check the warning circuit at East Pier.";
                case InspectionStage.ReadyToReport: return "Return to Oren and report the inspection.";
                case InspectionStage.Complete: return "Pier inspection complete.";
                default: return "Ask Oren about a pier inspection.";
            }
        }

        public string StatusLabel()
        {
            switch (DisplayContract)
            {
                case ContractStage.InProgress: return "CONTRACT ACTIVE";
                case ContractStage.Complete: return "CONTRACT COMPLETE";
                default: return "HARBOR OPEN";
            }
        }

        public void OpenGarage()
        {
            CloseInventory();
            PoliceCareerOpen = false;
            BankOpen = false;
            GarageOpen = true;
            HarborlightTouchControls.SetUiBlocked(true);
            SetMessage("Choose a vehicle and a finish for your harbor collection.");
        }

        public void CloseGarage()
        {
            GarageOpen = false;
            HarborlightTouchControls.SetUiBlocked(false);
        }

        public void OpenBank()
        {
            CloseInventory();
            PoliceCareerOpen = false;
            GarageOpen = false;
            BankOpen = true;
            HarborlightTouchControls.SetUiBlocked(true);
            SetMessage("Manage cash and bank credits at the Harbor Bank ATM.");
        }

        public void CloseBank()
        {
            BankOpen = false;
            HarborlightTouchControls.SetUiBlocked(false);
        }

        public void OpenInventory()
        {
            GarageOpen = false;
            BankOpen = false;
            PoliceCareerOpen = false;
            InventoryOpen = true;
            HarborlightTouchControls.SetUiBlocked(true);
        }

        public void CloseInventory()
        {
            InventoryOpen = false;
            HarborlightTouchControls.SetUiBlocked(false);
        }

        public void OpenPoliceCareer()
        {
            CloseInventory();
            GarageOpen = false;
            BankOpen = false;
            PoliceCareerOpen = true;
            HarborlightTouchControls.SetUiBlocked(true);
        }

        public void ClosePoliceCareer()
        {
            PoliceCareerOpen = false;
            HarborlightTouchControls.SetUiBlocked(false);
        }

        public bool ApplyLocalPoliceCareerAction(string action, out string message)
        {
            var accepted = false;
            message = "Complete the Beacon, Courier, and Pier jobs with at least 4 reputation before applying.";
            if (action == "Apply")
            {
                if (PoliceApplied) message = "Your Harbor Patrol application is already on file.";
                else if (IsEligibleForPoliceCareer)
                {
                    PoliceApplied = true;
                    accepted = true;
                    message = "Harbor Patrol application approved. Report for duty when you are ready.";
                }
            }
            else if (!PoliceApplied)
            {
                message = "Apply to Harbor Patrol first.";
            }
            else if (action == "StartDuty")
            {
                accepted = !PoliceOnDuty;
                PoliceOnDuty = true;
                message = accepted ? "You are on duty. Accept a dispatch when ready." : "You are already on duty.";
            }
            else if (action == "EndDuty")
            {
                accepted = PoliceOnDuty;
                PoliceOnDuty = false;
                PoliceActiveDispatchCode = "";
                message = accepted ? "You are off duty. Your active dispatch was cleared." : "You are already off duty.";
            }
            else if (action == "AcceptDispatch")
            {
                if (!PoliceOnDuty)
                    message = "Go on duty before requesting a dispatch.";
                else if (!string.IsNullOrEmpty(PoliceActiveDispatchCode))
                    message = "Finish your current dispatch first.";
                else
                {
                    PoliceActiveDispatchCode = PoliceDispatchCodeFor(PoliceCallsCompleted);
                    accepted = true;
                    message = "Dispatch assigned: " + PoliceDispatchName(PoliceActiveDispatchCode) + ". " +
                        PoliceDispatchTarget(PoliceActiveDispatchCode) + ".";
                }
            }
            else
            {
                message = "That Harbor Patrol action is unavailable.";
            }

            SetMessage(message);
            if (accepted) Save();
            return accepted;
        }

        public bool ResolveLocalPoliceDispatch(string dispatchCode, out string message)
        {
            var reward = PoliceDispatchReward(dispatchCode);
            if (!PoliceApplied || !PoliceOnDuty || PoliceActiveDispatchCode != dispatchCode || reward <= 0)
            {
                message = "That Harbor Patrol dispatch is not active.";
                return false;
            }

            var now = DateTimeOffset.UtcNow.ToString("O");
            var previousCredits = Credits;
            Credits = Mathf.Min(999999, Credits + reward);
            var earned = Credits - previousCredits;
            PoliceCallsCompleted = Mathf.Min(999999, PoliceCallsCompleted + 1);
            PoliceCreditsEarned = Mathf.Min(999999, PoliceCreditsEarned + earned);
            PoliceActiveDispatchCode = "";
            message = earned > 0 ? "Dispatch complete. You earned " + earned + " credits." : "Dispatch complete. Cash limit reached.";
            SetMessage(message);
            if (earned > 0)
                _bankTransactions.Insert(0, new HarborlightBankTransaction
                {
                    direction = "police_reward", amount = earned, cashDelta = earned, bankDelta = 0,
                    cashBalanceAfter = Credits, bankBalanceAfter = BankCredits, createdAtUtc = now
                });
            Save();
            return true;
        }

        public static string PoliceDispatchName(string code) => code switch
        {
            "beacon-safety" => "Tide Beacon Safety",
            "market-access" => "Market Access Check",
            "pier-circuit" => "East Pier Circuit Call",
            _ => ""
        };

        public static string PoliceDispatchTarget(string code) => code switch
        {
            "beacon-safety" => "Check the North Pier beacon controls",
            "market-access" => "Inspect the Net Loft Lane parcel area",
            "pier-circuit" => "Check the East Pier warning circuit",
            _ => ""
        };

        public static int PoliceDispatchReward(string code) => code switch
        {
            "beacon-safety" => 65,
            "market-access" => 75,
            "pier-circuit" => 85,
            _ => 0
        };

        private static string PoliceDispatchCodeFor(int callsCompleted)
            => (Mathf.Max(0, callsCompleted) % 3) switch
            {
                0 => "beacon-safety",
                1 => "market-access",
                _ => "pier-circuit"
            };

        public bool TryTransferLocalBank(string direction, int amount, out string result)
        {
            direction = direction != null ? direction.Trim().ToLowerInvariant() : "";
            if ((direction != "deposit" && direction != "withdraw") || amount < 1 || amount > 999999)
            {
                result = "Enter a valid transfer from 1 to 999,999 credits.";
                return false;
            }

            var cashDelta = direction == "deposit" ? -amount : amount;
            var bankDelta = -cashDelta;
            if (direction == "deposit" && Credits < amount)
            {
                result = "Not enough cash to deposit.";
                return false;
            }
            if (direction == "withdraw" && BankCredits < amount)
            {
                result = "Not enough bank credits to withdraw.";
                return false;
            }
            if (direction == "deposit" && BankCredits > 999999 - amount)
            {
                result = "Bank balance limit reached.";
                return false;
            }
            if (direction == "withdraw" && Credits > 999999 - amount)
            {
                result = "Cash balance limit reached.";
                return false;
            }

            var previousCash = Credits;
            var previousBank = BankCredits;
            Credits += cashDelta;
            BankCredits += bankDelta;
            _bankTransactions.Insert(0, new HarborlightBankTransaction
            {
                direction = direction,
                amount = amount,
                cashDelta = cashDelta,
                bankDelta = bankDelta,
                cashBalanceAfter = Credits,
                bankBalanceAfter = BankCredits,
                createdAtUtc = DateTimeOffset.UtcNow.ToString("O")
            });
            if (_bankTransactions.Count > 20) _bankTransactions.RemoveRange(20, _bankTransactions.Count - 20);

            if (!Save())
            {
                Credits = previousCash;
                BankCredits = previousBank;
                _bankTransactions.RemoveAt(0);
                result = "Transfer was not saved. Please try again.";
                return false;
            }

            result = direction == "deposit" ? "Deposit complete." : "Withdrawal complete.";
            SetMessage(result);
            return true;
        }

        public void CompleteTutorial()
        {
            if (_tutorialCompleted) return;
            _tutorialCompleted = true;
            HarborlightTouchControls.SetUiBlocked(false);
            Save();
        }

        public void ReportMessage(string message)
        {
            SetMessage(message);
        }

        public string GetVehiclePaintSku(int vehicleIndex)
        {
            return vehicleIndex >= 0 && vehicleIndex < _vehiclePaintSkus.Length
                ? _vehiclePaintSkus[vehicleIndex] ?? "" : "";
        }

        public bool OwnsCosmetic(string sku)
        {
            return !string.IsNullOrEmpty(sku) && Array.IndexOf(OwnedCosmeticSkus, sku) >= 0;
        }

        public void SetCosmeticOwnership(IEnumerable<string> ownedSkus)
        {
            OwnedCosmeticSkus = ownedSkus == null ? new string[0]
                : new List<string>(new HashSet<string>(ownedSkus)).ToArray();
            Save();
        }

        public void SelectGarageVehicle(int vehicleIndex)
        {
            if (!IsVehicleOwned(vehicleIndex)) return;
            SelectedVehicleIndex = Mathf.Clamp(vehicleIndex, 0, OwnedVehicles.Length - 1);
            Save();
        }

        public static int VehiclePrice(int vehicleIndex)
        {
            switch (vehicleIndex)
            {
                case 0: return 0;
                case 1: return 250;
                case 2: return 450;
                case 3: return 650;
                case 4: return 900;
                case 5: return 1200;
                case 6: return 7200;
                case 7: return 3200;
                default: return -1;
            }
        }

        public bool IsVehicleOwned(int vehicleIndex)
            => vehicleIndex >= 0 && vehicleIndex < OwnedVehicles.Length && OwnedVehicles[vehicleIndex];

        public bool TryPurchaseVehicle(int vehicleIndex, out string result)
        {
            var price = VehiclePrice(vehicleIndex);
            if (price < 0)
            {
                result = "That vehicle is not available.";
                return false;
            }
            if (IsVehicleOwned(vehicleIndex))
            {
                result = "You already own this vehicle.";
                return false;
            }
            if (Credits < price)
            {
                result = "This vehicle costs " + price + " credits. Finish jobs to earn more.";
                return false;
            }

            Credits -= price;
            OwnedVehicles[vehicleIndex] = true;
            SelectedVehicleIndex = vehicleIndex;
            result = "Vehicle added to your garage for " + price + " credits.";
            SetMessage(result);
            Save();
            return true;
        }

        public bool SetVehiclePaint(int vehicleIndex, string sku)
        {
            if (!string.IsNullOrEmpty(sku) && !OwnsCosmetic(sku)) return false;
            if (vehicleIndex < 0 || vehicleIndex >= _vehiclePaintSkus.Length) return false;
            _vehiclePaintSkus[vehicleIndex] = sku ?? "";
            Save();
            return true;
        }

        private void MovePlayer(Vector3 position)
        {
            if (Player == null) return;
            var controller = Player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            Player.position = position;
            if (controller != null) controller.enabled = true;
        }

        private void SetMessage(string message)
        {
            LastMessage = message;
        }

        private void Load()
        {
            string previousSavePath;
            bool fromLegacy;
            string json;
            try
            {
                previousSavePath = File.Exists(SavePath) ? ""
                    : File.Exists(PreviousSavePathV9) ? PreviousSavePathV9
                    : File.Exists(PreviousSavePathV8) ? PreviousSavePathV8
                    : File.Exists(PreviousSavePathV7) ? PreviousSavePathV7
                    : File.Exists(PreviousSavePathV6) ? PreviousSavePathV6
                    : File.Exists(PreviousSavePathV5) ? PreviousSavePathV5
                    : File.Exists(PreviousSavePathV4) ? PreviousSavePathV4
                    : File.Exists(PreviousSavePathV3) ? PreviousSavePathV3
                    : File.Exists(PreviousSavePathV2) ? PreviousSavePathV2 : "";
                fromLegacy = !File.Exists(SavePath) && string.IsNullOrEmpty(previousSavePath) && PlayerPrefs.HasKey(LegacySaveKey);
                json = File.Exists(SavePath) ? File.ReadAllText(SavePath)
                    : !string.IsNullOrEmpty(previousSavePath) ? File.ReadAllText(previousSavePath)
                    : fromLegacy ? PlayerPrefs.GetString(LegacySaveKey, "") : "";
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Harborlight save could not be read; the default game state will be used: " + exception.Message);
                return;
            }
            if (string.IsNullOrEmpty(json)) return;
            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Harborlight save could not be read and was ignored: " + exception.Message);
                return;
            }
            if (data == null || (!fromLegacy && data.schemaVersion != 2 && data.schemaVersion != 3 && data.schemaVersion != 4 && data.schemaVersion != 5 && data.schemaVersion != 6 && data.schemaVersion != 7 && data.schemaVersion != 8 && data.schemaVersion != 9 && data.schemaVersion != SaveSchemaVersion))
            {
                Debug.LogWarning("Harborlight save uses an unsupported schema and was ignored.");
                return;
            }
            Credits = Mathf.Clamp(data.credits, 0, 999999);
            BankCredits = Mathf.Clamp(data.bankCredits, 0, 999999);
            _bankTransactions.Clear();
            if (data.bankTransactions != null)
                foreach (var bankTransaction in data.bankTransactions)
                    if (bankTransaction != null && bankTransaction.amount > 0)
                        _bankTransactions.Add(bankTransaction);
            Reputation = Mathf.Clamp(data.reputation, 0, 999);
            CurrentContract = Enum.IsDefined(typeof(ContractStage), data.contractStage) ? data.contractStage : ContractStage.Available;
            CurrentCourierStage = Enum.IsDefined(typeof(CourierStage), data.courierStage) ? data.courierStage : CourierStage.Available;
            CurrentInspectionStage = Enum.IsDefined(typeof(InspectionStage), data.inspectionStage) ? data.inspectionStage : InspectionStage.Available;
            DeliveriesCompleted = Mathf.Clamp(data.deliveriesCompleted, 0, 999999);
            PoliceApplied = data.policeApplied;
            PoliceOnDuty = PoliceApplied && data.policeOnDuty;
            PoliceCallsCompleted = Mathf.Clamp(data.policeCallsCompleted, 0, 999999);
            PoliceCreditsEarned = Mathf.Clamp(data.policeCreditsEarned, 0, 999999);
            var savedDispatchCode = data.policeActiveDispatchCode ?? "";
            PoliceActiveDispatchCode = PoliceOnDuty && PoliceDispatchReward(savedDispatchCode) > 0 ? savedDispatchCode : "";
            SelectedVehicleIndex = Mathf.Clamp(data.selectedVehicleIndex, 0, VehicleCatalogSize - 1);
            OwnedVehicles = new[] { true, false, false, false, false, false, false, false };
            if (data.ownedVehicles != null)
                for (var i = 0; i < Mathf.Min(OwnedVehicles.Length, data.ownedVehicles.Length); i++)
                    OwnedVehicles[i] = data.ownedVehicles[i];
            OwnedVehicles[0] = true;
            if (!IsVehicleOwned(SelectedVehicleIndex)) SelectedVehicleIndex = 0;
            _vehiclePaintSkus = new string[VehicleCatalogSize];
            if (data.vehiclePaintSkus != null)
                for (var i = 0; i < Mathf.Min(_vehiclePaintSkus.Length, data.vehiclePaintSkus.Length); i++)
                    _vehiclePaintSkus[i] = data.vehiclePaintSkus[i] ?? "";
            // v1-v5 saves used named paint fields; carry them forward where the new array has no value.
            if (string.IsNullOrEmpty(_vehiclePaintSkus[0])) _vehiclePaintSkus[0] = data.taxiPaintSku ?? "";
            if (string.IsNullOrEmpty(_vehiclePaintSkus[1])) _vehiclePaintSkus[1] = data.sedanPaintSku ?? "";
            if (string.IsNullOrEmpty(_vehiclePaintSkus[2])) _vehiclePaintSkus[2] = data.vanPaintSku ?? "";
            _resumeInVehicle = data.wasDriving && data.drivenVehicleIndex >= 0
                && data.drivenVehicleIndex < VehicleCatalogSize && IsVehicleOwned(data.drivenVehicleIndex);
            _resumeVehicleIndex = _resumeInVehicle ? data.drivenVehicleIndex : -1;
            var vehiclePosition = HarborlightDistrictSurfaceNetwork.ClampToPlayableLand(
                new Vector2(data.vehicleX, Mathf.Clamp(data.vehicleZ,
                    HarborlightDistrictSurfaceNetwork.PlayableMinZ,
                    HarborlightDistrictSurfaceNetwork.VehicleMaxZ)));
            _resumeVehiclePosition = new Vector3(vehiclePosition.x,
                HarborlightDistrictSurfaceNetwork.TerrainHeight(vehiclePosition.y) + 0.02f,
                vehiclePosition.y);
            _resumeVehicleHeading = Mathf.Repeat(data.vehicleHeading, 360f);
            OwnedCosmeticSkus = data.ownedCosmeticSkus ?? new string[0];
            _tutorialCompleted = data.schemaVersion < 5 || data.tutorialCompleted;
            var loadedPosition = HarborlightDistrictSurfaceNetwork.ClampToPlayableLand(
                new Vector2(data.playerX, data.playerZ));
            _loadedX = loadedPosition.x;
            _loadedZ = loadedPosition.y;
            if (fromLegacy || !string.IsNullOrEmpty(previousSavePath) || data.schemaVersion != SaveSchemaVersion)
            {
                if (Save())
                {
                    PlayerPrefs.DeleteKey(LegacySaveKey);
                    PlayerPrefs.Save();
                    try
                    {
                        if (!string.IsNullOrEmpty(previousSavePath)) File.Delete(previousSavePath);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning("Migrated Harborlight save, but could not remove its old copy: " + exception.Message);
                    }
                }
            }
        }

        private bool Save()
        {
            var drivenVehicle = Player != null ? Player.GetComponentInParent<HarborlightVehicle>() : null;
            var drivenPosition = drivenVehicle != null ? drivenVehicle.transform.position : Vector3.zero;
            var data = new SaveData
            {
                schemaVersion = SaveSchemaVersion,
                credits = Credits,
                bankCredits = BankCredits,
                bankTransactions = _bankTransactions.ToArray(),
                reputation = Reputation,
                contractStage = CurrentContract,
                courierStage = CurrentCourierStage,
                inspectionStage = CurrentInspectionStage,
                deliveriesCompleted = DeliveriesCompleted,
                policeApplied = PoliceApplied,
                policeOnDuty = PoliceOnDuty,
                policeCallsCompleted = PoliceCallsCompleted,
                policeCreditsEarned = PoliceCreditsEarned,
                policeActiveDispatchCode = PoliceActiveDispatchCode,
                selectedVehicleIndex = SelectedVehicleIndex,
                ownedVehicles = OwnedVehicles,
                vehiclePaintSkus = _vehiclePaintSkus,
                taxiPaintSku = _vehiclePaintSkus.Length > 0 ? _vehiclePaintSkus[0] : "",
                sedanPaintSku = _vehiclePaintSkus.Length > 1 ? _vehiclePaintSkus[1] : "",
                vanPaintSku = _vehiclePaintSkus.Length > 2 ? _vehiclePaintSkus[2] : "",
                ownedCosmeticSkus = OwnedCosmeticSkus,
                tutorialCompleted = _tutorialCompleted,
                playerX = Player != null ? Player.position.x : _loadedX,
                playerZ = Player != null ? Player.position.z : _loadedZ,
                wasDriving = drivenVehicle != null,
                drivenVehicleIndex = drivenVehicle != null ? drivenVehicle.GarageIndex : -1,
                vehicleX = drivenPosition.x,
                vehicleZ = drivenPosition.z,
                vehicleHeading = drivenVehicle != null ? drivenVehicle.transform.eulerAngles.y : 0f
            };
            try
            {
                Directory.CreateDirectory(HarborlightLocalProfileStorage.PersistentDataPath);
                var temporaryPath = SavePath + ".tmp";
                File.WriteAllText(temporaryPath, JsonUtility.ToJson(data));
                if (File.Exists(SavePath)) File.Replace(temporaryPath, SavePath, null);
                else File.Move(temporaryPath, SavePath);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError("Harborlight save failed: " + exception.Message);
                return false;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void OnApplicationQuit()
        {
            Save();
        }

    }
}
