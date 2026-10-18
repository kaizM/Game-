using UnityEngine;
using Meridian.GameAdmin;

namespace Harborlight
{
    public sealed class HarborlightHud : MonoBehaviour
    {
        private HarborlightGameState _state;
        private HarborlightBackendClient _backend;
        private HarborlightGarageService _garage;
        private HarborlightPlayer _movement;
        private Transform _player;
        private GUIStyle _title;
        private GUIStyle _body;
        private GUIStyle _small;
        private GUIStyle _card;
        private GUIStyle _mapLabel;
        private GUIStyle _mapDistrictLabel;
        private Texture2D _panelTexture;
        private Texture2D _mapTexture;
        private Texture2D _mapWaterMask;
        private Texture2D _mapDistrictTexture;
        private string _garageFeedback = "Select a vehicle, then choose an owned finish.";
        private string _bankAmountText = "50";
        private string _bankFeedback = "Enter an amount, then deposit or withdraw.";
        private string _inventoryFeedback = "Your supplies and equipment.";
        private string _policeCareerFeedback = "Complete the three starter jobs to qualify.";
        private Vector2 _garageScroll;
        private Vector2 _inventoryScroll;
        private bool _districtMapExpanded;
        private bool _bankRefreshRequested;
        private bool _inventoryRefreshRequested;
        private bool _policeCareerRefreshRequested;
        private bool _inventoryWasOnline;
        private HarborlightNetworkPlayer[] _networkMapPlayers;
        private float _nextNetworkMapPlayerRefreshAt;

        public void Bind(HarborlightGameState state, Transform player)
        {
            _state = state;
            _player = player;
            _movement = player != null ? player.GetComponent<HarborlightPlayer>() : null;
        }

        public void SetExpandedDistrictMapForQaCapture()
        {
            _districtMapExpanded = true;
        }

        public void BindBackend(HarborlightBackendClient backend)
        {
            _backend = backend;
        }

        public void BindGarage(HarborlightGarageService garage)
        {
            _garage = garage;
        }

        private void OnGUI()
        {
            if (_state == null) return;
            EnsureStyles();
            var inventoryOnline = _backend != null && _backend.IsConnected;
            if (_state.InventoryOpen && inventoryOnline && !_inventoryWasOnline)
                _inventoryRefreshRequested = false;
            _inventoryWasOnline = inventoryOnline;

            if (_state.TutorialOpen)
            {
                DrawTutorial();
                return;
            }

            if (_state.GarageOpen)
            {
                if (!GameAdminOverlay.IsVisible) DrawGaragePanel();
                return;
            }

            if (!_state.BankOpen) _bankRefreshRequested = false;
            if (_state.BankOpen)
            {
                if (!GameAdminOverlay.IsVisible) DrawBankPanel();
                return;
            }

            if (!_state.InventoryOpen) _inventoryRefreshRequested = false;
            if (_state.InventoryOpen)
            {
                if (!GameAdminOverlay.IsVisible) DrawInventoryPanel();
                return;
            }

            if (!_state.PoliceCareerOpen) _policeCareerRefreshRequested = false;
            if (_state.PoliceCareerOpen)
            {
                if (!GameAdminOverlay.IsVisible) DrawPoliceCareerPanel();
                return;
            }

            if (!GameAdminOverlay.IsVisible && (_movement == null || !_movement.IsDriving) &&
                Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.I)
            {
                _state.OpenInventory();
                Event.current.Use();
            }

            if (!GameAdminOverlay.IsVisible && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.M)
            {
                _districtMapExpanded = !_districtMapExpanded;
                Event.current.Use();
            }
            if (!GameAdminOverlay.IsVisible && (_movement == null || !_movement.IsDriving) &&
                Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.P)
            {
                _state.OpenPoliceCareer();
                Event.current.Use();
            }
            if (_districtMapExpanded && !GameAdminOverlay.IsVisible)
            {
                DrawExpandedDistrictMap();
                return;
            }

            if (HarborlightTouchControls.IsSupported)
            {
                DrawMobileHud();
                return;
            }

            GUI.Box(new Rect(18, 18, 340, 138), GUIContent.none, _card);
            GUI.Label(new Rect(34, 29, 300, 28), "HARBORLIGHT  /  NORTH PIER", _title);
            GUI.Label(new Rect(34, 62, 300, 25), _state.StatusLabel(), _small);
            GUI.Label(new Rect(34, 89, 300, 26), "¢ " + _state.DisplayCredits + "     REP  " + _state.DisplayReputation + "     RUNS  " + _state.DisplayDeliveriesCompleted, _body);
            var gems = _backend != null && _backend.IsConnected ? "  ·  GEMS " + _backend.Gems : "  ·  GEMS OFFLINE";
            GUI.Label(new Rect(34, 119, 305, 26), _state.CurrentLocation + gems, _small);

            GUI.Box(new Rect(18, 170, 340, 136), GUIContent.none, _card);
            GUI.Label(new Rect(34, 182, 305, 24), "CITY CONTRACTS", _small);
            GUI.Label(new Rect(34, 210, 305, 98), "BEACON  ·  " + _state.ContractStatus() + "\nCOURIER  ·  " + _state.CourierStatus() + "\nPIER  ·  " + _state.InspectionStatus(), _body);

            GUI.Box(new Rect(18, Screen.height - 108, 510, 82), GUIContent.none, _card);
            GUI.Label(new Rect(34, Screen.height - 96, 475, 30), CurrentPrompt(), _body);
            var controls = _movement != null && _movement.IsDriving
                ? "W/S accelerate / brake   ·   A/D steer   ·   E exit   ·   F1 admin"
                : "WASD move   ·   Shift jog   ·   E interact   ·   I bag   ·   P career   ·   F1 admin";
            GUI.Label(new Rect(34, Screen.height - 64, 475, 24), controls, _small);

            if (!GameAdminOverlay.IsVisible) DrawDistrictMap();
        }

        private void DrawMobileHud()
        {
            var scale = Mathf.Clamp(Screen.width / 430f, 0.78f, 1.15f);
            var title = new GUIStyle(_title) { fontSize = Mathf.RoundToInt(14f * scale) };
            var body = new GUIStyle(_body) { fontSize = Mathf.RoundToInt(11f * scale) };
            var small = new GUIStyle(_small) { fontSize = Mathf.RoundToInt(9f * scale) };
            var panelWidth = Mathf.Max(156f, Screen.width * 0.48f);
            var panelHeight = Mathf.Max(92f, Screen.height * 0.12f);
            var safe = Screen.safeArea;
            var topInset = Screen.height - safe.yMax;
            var bottomInset = safe.yMin;
            var rightInset = Screen.width - safe.xMax;
            GUI.Box(new Rect(safe.xMin + 10f, topInset + 10f, panelWidth, panelHeight), GUIContent.none, _card);
            GUI.Label(new Rect(safe.xMin + 20f, topInset + 17f, panelWidth - 18f, 21f), "HARBORLIGHT", title);
            GUI.Label(new Rect(safe.xMin + 20f, topInset + 39f, panelWidth - 18f, 17f), _state.StatusLabel() + "  ·  " + _state.CurrentLocation, small);
            GUI.Label(new Rect(safe.xMin + 20f, topInset + 59f, panelWidth - 18f, 20f), "¢ " + _state.DisplayCredits + "     REP " + _state.DisplayReputation, body);
            var wallet = _backend != null && _backend.IsConnected ? "GEMS " + _backend.Gems : "GEMS OFFLINE";
            GUI.Label(new Rect(safe.xMin + 20f, topInset + 78f, panelWidth - 18f, 17f), wallet, small);
            if (!GameAdminOverlay.IsVisible && (_movement == null || !_movement.IsDriving) &&
                GUI.Button(new Rect(safe.xMax - 88f, topInset + 15f, 78f, 30f), "CAREER", _card))
                _state.OpenPoliceCareer();

            if (!GameAdminOverlay.IsVisible) DrawDistrictMap();

            var prompt = CurrentPrompt();
            var promptWidth = Mathf.Min(Screen.width - 24f, 520f);
            var promptRect = new Rect((Screen.width - promptWidth) * 0.5f, Screen.height - bottomInset - 82f, promptWidth, 52f);
            GUI.Box(promptRect, GUIContent.none, _card);
            GUI.Label(new Rect(promptRect.x + 10f, promptRect.y + 10f, promptRect.width - 20f, promptRect.height - 20f), prompt, body);

            if (GameAdminOverlay.IsVisible) return;
            var radius = Screen.width * 0.12f;
            var joystick = new Vector2(Screen.width * 0.18f, Screen.height * 0.20f);
            DrawMapRect(new Rect(joystick.x - radius, Screen.height - joystick.y - radius, radius * 2f, radius * 2f), new Color(0.05f, 0.09f, 0.10f, 0.48f));
            var knob = joystick + HarborlightTouchControls.Move * radius * 0.72f;
            DrawMapRect(new Rect(knob.x - radius * 0.34f, Screen.height - knob.y - radius * 0.34f, radius * 0.68f, radius * 0.68f), new Color(0.36f, 0.72f, 0.66f, 0.88f));

            var controlWidth = Screen.width * 0.17f;
            var controlHeight = Screen.height * 0.105f;
            var gas = new Rect(Screen.width * 0.79f, Screen.height * 0.64f, controlWidth, controlHeight);
            var brake = new Rect(Screen.width * 0.60f, Screen.height * 0.64f, controlWidth, controlHeight);
            var action = new Rect(Screen.width * 0.79f, Screen.height * 0.51f, controlWidth, controlHeight);
            var exit = new Rect(Screen.width * 0.60f, Screen.height * 0.51f, controlWidth, controlHeight);
            if ((_movement == null || !_movement.IsDriving) &&
                GUI.Button(new Rect(Screen.width * 0.79f, Screen.height * 0.41f, controlWidth, controlHeight * 0.52f), "BAG", _card))
                _state.OpenInventory();
            GUI.Box(gas, "GAS", _card);
            GUI.Box(brake, "BRAKE", _card);
            GUI.Box(action, _movement != null && _movement.IsDriving ? "DRIVE" : "USE", _card);
            GUI.Box(exit, _movement != null && _movement.IsDriving ? "EXIT" : "", _card);
        }

        private void DrawPoliceCareerPanel()
        {
            var networkSession = _movement != null && _movement.IsNetworkSession;
            var online = networkSession && _backend != null && _backend.IsConnected;
            if (networkSession && online && !_policeCareerRefreshRequested)
            {
                _policeCareerRefreshRequested = true;
                _backend.LoadPoliceCareer((succeeded, message) => _policeCareerFeedback = message);
            }

            var safe = Screen.safeArea;
            var topInset = Screen.height - safe.yMax;
            var width = Mathf.Min(HarborlightTouchControls.IsSupported ? 560f : 620f, safe.width - 24f);
            var height = Mathf.Min(390f, safe.height - 24f);
            var panel = new Rect(safe.xMin + (safe.width - width) * 0.5f,
                topInset + (safe.height - height) * 0.5f, width, height);
            GUI.Box(panel, GUIContent.none, _card);
            GUI.Label(new Rect(panel.x + 18f, panel.y + 14f, panel.width - 110f, 30f), "HARBOR PATROL", _title);
            if (GUI.Button(new Rect(panel.xMax - 82f, panel.y + 12f, 66f, 32f), "CLOSE"))
                _state.ClosePoliceCareer();

            var career = online ? _backend.PoliceCareer : null;
            if (networkSession && career == null)
            {
                GUI.Label(new Rect(panel.x + 20f, panel.y + 62f, panel.width - 40f, 34f),
                    online ? "Loading officer records…" : "Connect to Harborlight to apply and save an officer career.", _body);
                if (online && !(_backend.IsPoliceCareerBusy) &&
                    GUI.Button(new Rect(panel.x + 20f, panel.y + 108f, 130f, 38f), "RETRY LOAD"))
                {
                    _policeCareerRefreshRequested = false;
                    return;
                }
                GUI.Label(new Rect(panel.x + 20f, panel.yMax - 48f, panel.width - 40f, 34f),
                    "Career progression requires a service connection. Free roam remains available offline.", _small);
                return;
            }

            var applied = networkSession ? career.applied : _state.PoliceApplied;
            var onDuty = networkSession ? career.isOnDuty : _state.PoliceOnDuty;
            var eligible = networkSession ? career.eligible : _state.IsEligibleForPoliceCareer;
            var callsCompleted = networkSession ? career.callsCompleted : _state.PoliceCallsCompleted;
            var creditsEarned = networkSession ? career.creditsEarned : _state.PoliceCreditsEarned;
            var activeDispatchCode = networkSession ? career.activeDispatchCode : _state.PoliceActiveDispatchCode;
            var activeDispatchName = networkSession ? career.activeDispatchName : HarborlightGameState.PoliceDispatchName(activeDispatchCode);
            var activeDispatchTarget = networkSession ? career.activeDispatchTarget : HarborlightGameState.PoliceDispatchTarget(activeDispatchCode);
            var dispatchReward = networkSession ? career.dispatchReward : HarborlightGameState.PoliceDispatchReward(activeDispatchCode);
            var canSendCareerAction = !networkSession || (online && !_backend.IsPoliceCareerBusy);

            GUI.Label(new Rect(panel.x + 20f, panel.y + 58f, panel.width - 40f, 28f),
                applied ? (onDuty ? "OFFICER  ·  ON DUTY" : "OFFICER  ·  OFF DUTY") : "HARBOR PATROL RECRUITMENT", _small);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 92f, panel.width - 40f, 30f),
                "CALLS  " + callsCompleted + "       CAREER PAY  ¢ " + creditsEarned, _body);

            if (!applied)
            {
                GUI.Label(new Rect(panel.x + 20f, panel.y + 132f, panel.width - 40f, 56f),
                    eligible ? "Your record qualifies. Apply to join the Harbor Patrol." :
                        "Qualification: finish the Beacon, Courier, and Pier starter jobs and reach 4 reputation. " +
                        "Current reputation: " + _state.DisplayReputation + ".", _body);
                GUI.enabled = canSendCareerAction && eligible;
                if (GUI.Button(new Rect(panel.x + 20f, panel.y + 206f, 190f, 42f), "APPLY TO HARBOR PATROL"))
                    RunPoliceCareerAction("Apply");
                GUI.enabled = true;
            }
            else
            {
                if (!onDuty)
                {
                    GUI.enabled = canSendCareerAction;
                    if (GUI.Button(new Rect(panel.x + 20f, panel.y + 144f, 190f, 42f), "GO ON DUTY"))
                        RunPoliceCareerAction("StartDuty");
                    GUI.enabled = true;
                }
                else if (string.IsNullOrEmpty(activeDispatchCode))
                {
                    GUI.enabled = canSendCareerAction;
                    if (GUI.Button(new Rect(panel.x + 20f, panel.y + 144f, 190f, 42f), "REQUEST DISPATCH"))
                        RunPoliceCareerAction("AcceptDispatch");
                    GUI.enabled = true;
                }
                else
                {
                    GUI.Label(new Rect(panel.x + 20f, panel.y + 142f, panel.width - 40f, 28f),
                        activeDispatchName + "  ·  ¢ " + dispatchReward, _body);
                    GUI.Label(new Rect(panel.x + 20f, panel.y + 174f, panel.width - 40f, 48f),
                        activeDispatchTarget + ". Travel to the listed site and use E / USE to complete it.", _small);
                }

                GUI.enabled = canSendCareerAction && onDuty;
                if (GUI.Button(new Rect(panel.x + 224f, panel.y + 144f, 140f, 42f), "END DUTY"))
                    RunPoliceCareerAction("EndDuty");
                GUI.enabled = true;
            }

            var feedback = _policeCareerFeedback;
            GUI.Label(new Rect(panel.x + 20f, panel.yMax - 52f, panel.width - 40f, 42f), feedback, _small);
        }

        private void RunPoliceCareerAction(string action)
        {
            _policeCareerFeedback = "Saving officer status…";
            if (_movement == null || !_movement.IsNetworkSession)
            {
                var accepted = _state.ApplyLocalPoliceCareerAction(action, out var message);
                _policeCareerFeedback = message;
                if (accepted) _policeCareerRefreshRequested = false;
                return;
            }

            _backend.RunPoliceCareerAction(action, (succeeded, message) =>
            {
                _policeCareerFeedback = message;
                if (succeeded)
                    _backend.LoadPoliceCareer((loaded, status) =>
                    {
                        if (!loaded) _policeCareerFeedback = status;
                    });
            });
        }

        private void DrawTutorial()
        {
            var safe = Screen.safeArea;
            DrawMapRect(new Rect(safe.xMin, Screen.height - safe.yMax, safe.width, safe.height), new Color(0.035f, 0.065f, 0.075f, 0.90f));
            var width = Mathf.Min(560f, safe.width - 32f);
            var height = HarborlightTouchControls.IsSupported ? 310f : 285f;
            var panel = new Rect(safe.xMin + (safe.width - width) * 0.5f,
                Screen.height - safe.yMin - (safe.height - height) * 0.5f - height, width, height);
            GUI.Box(panel, GUIContent.none, _card);
            GUI.Label(new Rect(panel.x + 22f, panel.y + 20f, width - 44f, 34f), "WELCOME TO NORTH PIER", _title);

            var instructions = HarborlightTouchControls.IsSupported
                ? "Move with the left stick. Drag the open view to look around. Tap BAG for inventory and the city map for the full map.\n\nTap USE near an objective to interact. Talk to Mara in Lantern Square to take your first harbor job. Use GAS and BRAKE when driving."
                : "WASD moves, the right mouse button looks around, and E interacts. Press I for inventory and M or click the city map to open it.\n\nTalk to Mara in Lantern Square to take your first harbor job. Use E beside a vehicle to drive; WASD steers and moves it.";
            GUI.Label(new Rect(panel.x + 22f, panel.y + 72f, width - 44f, height - 142f), instructions, _body);

            var button = new Rect(panel.x + 22f, panel.yMax - 58f, width - 44f, 40f);
            if (GUI.Button(button, "START HARBORLIGHT") ||
                (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)))
            {
                _state.CompleteTutorial();
                Event.current.Use();
            }
        }

        private void DrawGaragePanel()
        {
            var safe = Screen.safeArea;
            var safeTop = Screen.height - safe.yMax;
            var width = Mathf.Min(HarborlightTouchControls.IsSupported ? 620f : 680f, safe.width - 24f);
            var height = Mathf.Min(540f, safe.height - 24f);
            var panel = new Rect(safe.xMin + (safe.width - width) * 0.5f, safeTop + (safe.height - height) * 0.5f, width, height);
            GUI.Box(panel, GUIContent.none, _card);
            GUI.Label(new Rect(panel.x + 18f, panel.y + 14f, panel.width - 100f, 28f), "HARBORLIGHT MOTOR WORKS", _title);
            if (GUI.Button(new Rect(panel.xMax - 82f, panel.y + 12f, 66f, 32f), "CLOSE")) _state.CloseGarage();
            GUI.Label(new Rect(panel.x + 18f, panel.y + 48f, panel.width - 36f, 24f),
                _garage != null ? "Selected vehicle  ·  " + _garage.SelectedVehicleName : "Garage unavailable", _small);

            if (_garage == null) return;
            var vehicles = _garage.VehicleCount;
            const int columns = 3;
            const float gap = 8f;
            var buttonWidth = (panel.width - 36f - gap * (columns - 1)) / columns;
            for (var i = 0; i < vehicles; i++)
            {
                var column = i % columns;
                var row = i / columns;
                var rect = new Rect(panel.x + 18f + column * (buttonWidth + gap), panel.y + 78f + row * 42f, buttonWidth, 38f);
                if (GUI.Button(rect, _garage.VehicleChoiceLabel(i))) _garageFeedback = _garage.SelectOrBuyVehicle(i);
            }

            var gridRows = Mathf.Max(1, Mathf.CeilToInt(vehicles / (float)columns));
            var defaultY = panel.y + 78f + gridRows * 42f + 4f;
            var defaultRect = new Rect(panel.x + 18f, defaultY, panel.width - 36f, 36f);
            if (GUI.Button(defaultRect, "RESTORE FACTORY FINISH")) _garage.RestoreFactoryPaint();
            GUI.Label(new Rect(panel.x + 18f, defaultY + 44f, panel.width - 36f, 24f),
                "PAINT COLLECTION  ·  " + (_backend != null && _backend.IsConnected ? "GEMS " + _backend.Gems : "OFFLINE"), _small);

            var listTop = defaultY + 72f;
            var rowHeight = 48f;
            var items = _garage.Catalog;
            if (items.Count == 0)
                GUI.Label(new Rect(panel.x + 18f, listTop, panel.width - 36f, 50f),
                    _backend != null && _backend.IsConnected ? "No finishes are available right now." : "Online finishes appear when the Harborlight service is connected.", _body);
            else
            {
                var viewport = new Rect(panel.x + 18f, listTop, panel.width - 36f, Mathf.Max(32f, panel.yMax - 54f - listTop));
                var content = new Rect(0f, 0f, viewport.width, items.Count * rowHeight);
                _garageScroll = GUI.BeginScrollView(viewport, _garageScroll, content);
                for (var i = 0; i < items.Count; i++)
                {
                    var cosmetic = items[i];
                    var rowY = i * rowHeight;
                    GUI.Label(new Rect(4f, rowY + 4f, viewport.width - 136f, 38f), cosmetic.displayName + "  ·  " + cosmetic.priceGems + " gems", _body);
                    var owned = _garage.IsOwned(cosmetic.sku);
                    var canBuy = _backend != null && _backend.IsConnected && _backend.Gems >= cosmetic.priceGems;
                    var buttonLabel = owned ? "EQUIP" : canBuy ? "UNLOCK" : "LOCKED";
                    GUI.enabled = owned || canBuy;
                    if (GUI.Button(new Rect(viewport.width - 112f, rowY, 106f, 38f), buttonLabel))
                    {
                        _garage.ChoosePaint(cosmetic, (succeeded, message) => _garageFeedback = message);
                    }
                    GUI.enabled = true;
                }
                GUI.EndScrollView();
            }
            GUI.Label(new Rect(panel.x + 18f, panel.yMax - 42f, panel.width - 36f, 30f), _garageFeedback, _small);
        }

        private void DrawBankPanel()
        {
            var online = _backend != null && _backend.IsConnected;
            if (online && !_bankRefreshRequested)
            {
                _bankRefreshRequested = true;
                _backend.LoadBank((succeeded, message) =>
                {
                    _bankFeedback = message;
                    if (succeeded) _state.ApplyNetworkBankSnapshot(_backend.BankCashCredits, _backend.BankCredits);
                });
            }

            var safe = Screen.safeArea;
            var safeTop = Screen.height - safe.yMax;
            var width = Mathf.Min(HarborlightTouchControls.IsSupported ? 620f : 660f, safe.width - 24f);
            var height = Mathf.Min(470f, safe.height - 24f);
            var panel = new Rect(safe.xMin + (safe.width - width) * 0.5f,
                safeTop + (safe.height - height) * 0.5f, width, height);
            GUI.Box(panel, GUIContent.none, _card);
            GUI.Label(new Rect(panel.x + 18f, panel.y + 14f, panel.width - 110f, 30f), "HARBOR BANK", _title);
            if (GUI.Button(new Rect(panel.xMax - 82f, panel.y + 12f, 66f, 32f), "CLOSE"))
            {
                _state.CloseBank();
                _bankRefreshRequested = false;
            }

            var cash = online ? _backend.BankCashCredits : _state.Credits;
            var bank = online ? _backend.BankCredits : _state.BankCredits;
            GUI.Label(new Rect(panel.x + 20f, panel.y + 54f, panel.width - 40f, 28f),
                "CASH  ¢ " + cash + "        BANK  ¢ " + bank, _body);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 82f, panel.width - 40f, 20f),
                online ? "Online balance · stored by Harborlight" : "Offline balance · saved on this device", _small);

            GUI.Label(new Rect(panel.x + 20f, panel.y + 114f, 90f, 30f), "AMOUNT", _small);
            var inputWidth = Mathf.Min(180f, panel.width * 0.38f);
            _bankAmountText = GUI.TextField(new Rect(panel.x + 112f, panel.y + 110f, inputWidth, 34f), _bankAmountText);
            var filteredAmount = "";
            foreach (var character in _bankAmountText ?? "")
            {
                if (!char.IsDigit(character) || filteredAmount.Length >= 7) continue;
                filteredAmount += character;
            }
            if (filteredAmount != _bankAmountText) _bankAmountText = filteredAmount;
            var validAmount = int.TryParse(_bankAmountText, out var amount) && amount > 0 && amount <= 999999;
            var actionsEnabled = validAmount && !(online && _backend.IsBankBusy) && (!online || _backend.BankLoaded);
            GUI.enabled = actionsEnabled;
            if (GUI.Button(new Rect(panel.x + 20f, panel.y + 154f, 150f, 38f), "DEPOSIT"))
                TransferBank("deposit", amount, online);
            if (GUI.Button(new Rect(panel.x + 180f, panel.y + 154f, 150f, 38f), "WITHDRAW"))
                TransferBank("withdraw", amount, online);
            GUI.enabled = true;

            if (online && !_backend.BankLoaded && GUI.Button(new Rect(panel.x + 340f, panel.y + 154f, 126f, 38f), "RETRY LOAD"))
            {
                _bankRefreshRequested = false;
                return;
            }

            GUI.Label(new Rect(panel.x + 20f, panel.y + 200f, panel.width - 40f, 34f), _bankFeedback, _small);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 236f, panel.width - 40f, 22f), "RECENT TRANSACTIONS", _small);
            var transactions = online ? _backend.BankTransactions : _state.LocalBankTransactions;
            if (transactions.Count == 0)
            {
                GUI.Label(new Rect(panel.x + 20f, panel.y + 264f, panel.width - 40f, 26f), "No transactions yet.", _small);
                return;
            }

            var rowY = panel.y + 264f;
            var rows = Mathf.Min(5, transactions.Count);
            for (var i = 0; i < rows; i++)
            {
                var transaction = transactions[i];
                var direction = transaction.direction ?? "transfer";
                var signedAmount = transaction.cashDelta < 0 ? "−" : "+";
                GUI.Label(new Rect(panel.x + 20f, rowY + i * 28f, panel.width - 40f, 24f),
                    direction.ToUpperInvariant() + "    " + signedAmount + "¢ " + transaction.amount +
                    "    CASH ¢ " + transaction.cashBalanceAfter + "    BANK ¢ " + transaction.bankBalanceAfter, _small);
            }
        }

        private void DrawInventoryPanel()
        {
            var online = _backend != null && _backend.IsConnected;
            if (!_inventoryRefreshRequested)
            {
                _inventoryRefreshRequested = true;
                if (online) LoadInventoryFromPanel();
                else _inventoryFeedback = "Connect to the Harborlight service to access saved inventory. Offline play remains available.";
            }

            var safe = Screen.safeArea;
            var topInset = Screen.height - safe.yMax;
            var width = Mathf.Min(HarborlightTouchControls.IsSupported ? 720f : 760f, safe.width - 24f);
            var height = Mathf.Min(660f, safe.height - 24f);
            var panel = new Rect(safe.xMin + (safe.width - width) * 0.5f,
                topInset + (safe.height - height) * 0.5f, width, height);
            GUI.Box(panel, GUIContent.none, _card);
            GUI.Label(new Rect(panel.x + 18f, panel.y + 14f, panel.width - 110f, 30f), "HARBOR SUPPLY", _title);
            if (GUI.Button(new Rect(panel.xMax - 82f, panel.y + 12f, 66f, 32f), "CLOSE"))
            {
                _state.CloseInventory();
                _inventoryRefreshRequested = false;
            }

            if (online && _backend.InventoryLoaded)
            {
                GUI.Label(new Rect(panel.x + 20f, panel.y + 52f, panel.width - 40f, 24f),
                    "CASH  ¢ " + _backend.InventoryCashCredits + "     PACK  " +
                    (_backend.InventoryWeightGrams / 1000f).ToString("F1") + " / " +
                    (_backend.InventoryCapacityGrams / 1000f).ToString("F1") + " KG", _small);
            }
            else GUI.Label(new Rect(panel.x + 20f, panel.y + 52f, panel.width - 40f, 24f),
                online ? "SAVED INVENTORY" : "OFFLINE PLAY", _small);

            if (online && GUI.Button(new Rect(panel.xMax - 132f, panel.y + 82f, 112f, 34f),
                    _backend.IsInventoryBusy ? "UPDATING" : "REFRESH"))
                LoadInventoryFromPanel();
            if (online && _backend.HasPendingInventoryAction &&
                GUI.Button(new Rect(panel.x + 20f, panel.y + 82f, 190f, 34f), "RETRY PENDING ACTION"))
            {
                _backend.RetryPendingInventoryAction((succeeded, message) =>
                {
                    _inventoryFeedback = message;
                    _state.ApplyNetworkCashSnapshot(_backend.InventoryCashCredits);
                });
            }

            if (online && _backend.InventoryLoaded)
            {
                var list = new Rect(panel.x + 18f, panel.y + 128f, panel.width - 36f, panel.height - 212f);
                var catalog = _backend.InventoryCatalog;
                var contentWidth = Mathf.Max(100f, list.width - 20f);
                _inventoryScroll = GUI.BeginScrollView(list, _inventoryScroll,
                    new Rect(0f, 0f, contentWidth, catalog.Count * 78f));
                for (var index = 0; index < catalog.Count; index++)
                {
                    var item = catalog[index];
                    if (item == null) continue;
                    var row = new Rect(0f, index * 78f, contentWidth, 72f);
                    GUI.Box(row, GUIContent.none, _card);
                    var owned = FindInventoryItem(item.sku);
                    var textWidth = Mathf.Max(80f, contentWidth - 185f);
                    GUI.Label(new Rect(row.x + 10f, row.y + 7f, textWidth, 24f), item.displayName, _body);
                    var ownedText = owned != null
                        ? "OWNED x" + owned.quantity + (owned.isEquipped ? "  ·  EQUIPPED" : "")
                        : "NOT OWNED";
                    GUI.Label(new Rect(row.x + 10f, row.y + 35f, textWidth, 22f),
                        "¢ " + item.priceCredits + "   ·   " + (item.weightGrams / 1000f).ToString("F1") + " kg   ·   " + ownedText,
                        _small);

                    var canEquip = !string.IsNullOrEmpty(item.equipmentSlot);
                    var buyRect = new Rect(contentWidth - 82f, row.y + 18f, 72f, 34f);
                    var equipRect = new Rect(contentWidth - 170f, row.y + 18f, 80f, 34f);
                    var wasEnabled = GUI.enabled;
                    GUI.enabled = wasEnabled && !_backend.IsInventoryBusy;
                    if (canEquip)
                    {
                        GUI.enabled = GUI.enabled && owned != null;
                        if (GUI.Button(equipRect, owned != null && owned.isEquipped ? "UNEQUIP" : "EQUIP"))
                            _backend.SetInventoryEquipped(item.sku, owned != null && !owned.isEquipped,
                                (succeeded, message) =>
                                {
                                    _inventoryFeedback = message;
                                    _state.ApplyNetworkCashSnapshot(_backend.InventoryCashCredits);
                                });
                        GUI.enabled = wasEnabled && !_backend.IsInventoryBusy;
                    }
                    if (GUI.Button(buyRect, "BUY"))
                        _backend.PurchaseInventoryItem(item.sku, (succeeded, message) =>
                        {
                            _inventoryFeedback = message;
                            _state.ApplyNetworkCashSnapshot(_backend.InventoryCashCredits);
                        });
                    GUI.enabled = wasEnabled;
                }
                GUI.EndScrollView();
            }
            else if (online)
            {
                GUI.Label(new Rect(panel.x + 22f, panel.y + 136f, panel.width - 44f, 46f),
                    "The saved catalog and inventory will appear here when the service responds.", _body);
                if (GUI.Button(new Rect(panel.x + 22f, panel.y + 186f, 126f, 36f), "RETRY LOAD"))
                    LoadInventoryFromPanel();
            }
            else
            {
                GUI.Label(new Rect(panel.x + 22f, panel.y + 136f, panel.width - 44f, 76f),
                    "Saved inventory, equipment, and purchases need a service connection. You can continue playing offline.", _body);
            }

            GUI.Label(new Rect(panel.x + 18f, panel.yMax - 46f, panel.width - 36f, 36f), _inventoryFeedback, _small);
        }

        private void LoadInventoryFromPanel()
        {
            _inventoryRefreshRequested = true;
            _backend.LoadInventory((succeeded, message) =>
            {
                _inventoryFeedback = message;
                if (succeeded) _state.ApplyNetworkCashSnapshot(_backend.InventoryCashCredits);
            });
        }

        private HarborlightBackendClient.InventoryItem FindInventoryItem(string sku)
        {
            foreach (var item in _backend.InventoryItems)
                if (item != null && item.sku == sku) return item;
            return null;
        }

        private void TransferBank(string direction, int amount, bool online)
        {
            if (online)
            {
                _bankFeedback = "Sending transfer...";
                _backend.TransferBank(direction, amount, (succeeded, message) =>
                {
                    _bankFeedback = message;
                    _state.ApplyNetworkBankSnapshot(_backend.BankCashCredits, _backend.BankCredits);
                });
                return;
            }

            if (_state.TryTransferLocalBank(direction, amount, out var result)) _bankAmountText = "";
            _bankFeedback = result;
        }

        private string CurrentPrompt()
        {
            if (_movement != null && _movement.IsDriving)
            {
                return "DRIVING  ·  " + _movement.VehicleName + "  ·  " + Mathf.RoundToInt(_movement.VehicleSpeed * 3.6f) + " km/h";
            }
            if (_player == null) return _state.LastMessage;
            var career = _backend != null ? _backend.PoliceCareer : null;
            var activeDispatchCode = _movement != null && _movement.IsNetworkSession
                ? career?.activeDispatchCode ?? ""
                : _state.PoliceActiveDispatchCode;
            if (!string.IsNullOrEmpty(activeDispatchCode))
            {
                foreach (var site in FindObjectsByType<HarborlightPoliceDispatchSite>())
                    if (site != null && site.DispatchCode == activeDispatchCode &&
                        Vector3.Distance(_player.position, site.transform.position) <= 3.2f)
                        return "E  ·  Complete " + HarborlightGameState.PoliceDispatchName(activeDispatchCode);
            }
            var candidates = FindObjectsByType<HarborlightInteractable>();
            HarborlightInteractable nearest = null;
            var distance = 3.2f;
            foreach (var candidate in candidates)
            {
                var currentDistance = Vector3.Distance(_player.position, candidate.transform.position);
                if (currentDistance >= distance) continue;
                nearest = candidate;
                distance = currentDistance;
            }
            if (nearest == null) return _state.LastMessage;
            return nearest.Action == WorldAction.EnterVehicle ? "E  ·  Drive " + nearest.DisplayName : _state.PromptFor(nearest.Action);
        }

        private void EnsureStyles()
        {
            if (_title != null) return;
            _panelTexture = SolidTexture(new Color(0.055f, 0.085f, 0.10f, 0.9f));
            _mapTexture = SolidTexture(new Color(0.19f, 0.28f, 0.28f, 0.96f));
            _mapWaterMask = CreateMapWaterMask();
            _mapDistrictTexture = CreateMapDistrictTexture();
            _title = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.94f, 0.92f, 0.82f) } };
            _body = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true, normal = { textColor = new Color(0.91f, 0.94f, 0.92f) } };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.64f, 0.80f, 0.79f) } };
            _card = new GUIStyle(GUI.skin.box);
            _card.normal.background = _panelTexture;
            _card.border = new RectOffset(12, 12, 12, 12);
            _mapLabel = new GUIStyle(_small) { fontSize = 10, alignment = TextAnchor.UpperLeft };
            _mapDistrictLabel = new GUIStyle(_small)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.91f, 0.89f, 0.78f) }
            };
        }

        private void DrawDistrictMap()
        {
            var size = HarborlightTouchControls.IsSupported ? Mathf.Clamp(Screen.width * 0.36f, 132f, 170f) : 194f;
            var safe = Screen.safeArea;
            var topInset = Screen.height - safe.yMax;
            var rightInset = Screen.width - safe.xMax;
            var bounds = new Rect(Screen.width - rightInset - size - 12f, topInset + 12f, size, size);
            GUI.Box(bounds, GUIContent.none, _card);
            var map = new Rect(bounds.x + 12, bounds.y + 28, bounds.width - 24, bounds.height - 40);
            GUI.DrawTexture(map, _mapTexture);
            DrawWorldMap(map);
            if (GUI.Button(bounds, GUIContent.none, GUIStyle.none)) _districtMapExpanded = true;
        }

        private void DrawExpandedDistrictMap()
        {
            var safe = Screen.safeArea;
            var topInset = Screen.height - safe.yMax;
            DrawMapRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.025f, 0.045f, 0.052f, 0.94f));
            var widthKm = (HarborlightDistrictSurfaceNetwork.WorldMaxX - HarborlightDistrictSurfaceNetwork.WorldMinX) / 1000f;
            var depthKm = (HarborlightDistrictSurfaceNetwork.WorldMaxZ - HarborlightDistrictSurfaceNetwork.WorldMinZ) / 1000f;
            GUI.Label(new Rect(safe.xMin + 16f, topInset + 12f, safe.width - 120f, 30f),
                "HARBORLIGHT OPEN WORLD  ·  " + widthKm.ToString("F2") + " × " + depthKm.ToString("F2") + " KM", _title);
            if (GUI.Button(new Rect(safe.xMin + 16f, topInset + 44f, 76f, 32f), "CLOSE"))
                _districtMapExpanded = false;

            var aspect = (HarborlightDistrictSurfaceNetwork.WorldMaxX - HarborlightDistrictSurfaceNetwork.WorldMinX) /
                         (HarborlightDistrictSurfaceNetwork.WorldMaxZ - HarborlightDistrictSurfaceNetwork.WorldMinZ);
            var mapAreaTop = topInset + 82f;
            var mapAreaHeight = Mathf.Max(80f, safe.height - 120f);
            var mapWidth = Mathf.Min(safe.width - 32f, mapAreaHeight * aspect);
            var mapHeight = mapWidth / aspect;
            var map = new Rect(safe.xMin + (safe.width - mapWidth) * 0.5f,
                mapAreaTop + (mapAreaHeight - mapHeight) * 0.5f, mapWidth, mapHeight);
            GUI.Box(new Rect(map.x - 8f, map.y - 8f, map.width + 16f, map.height + 16f), GUIContent.none, _card);
            GUI.DrawTexture(map, _mapTexture);
            DrawWorldMap(map);
            GUI.Label(new Rect(safe.xMin + 16f, Screen.height - safe.yMin - 28f, safe.width - 32f, 20f),
                HarborlightTouchControls.IsSupported ? "Tap CLOSE to return to the district." : "Press M or CLOSE to return to the district.", _small);
        }

        private void DrawWorldMap(Rect map)
        {
            var localRoadColor = new Color(0.10f, 0.14f, 0.12f, 1f);
            var arterialColor = new Color(0.22f, 0.29f, 0.27f, 1f);
            var boundsMinX = HarborlightDistrictSurfaceNetwork.WorldMinX;
            var boundsMaxX = HarborlightDistrictSurfaceNetwork.WorldMaxX;
            GUI.DrawTexture(map, _mapDistrictTexture);

            DrawCoastalWater(map);

            foreach (var z in HarborlightDistrictSurfaceNetwork.EastWestRoads)
                DrawWorldRoad(map, boundsMinX + 4f, z, boundsMaxX - 4f, z, z == -1f ? 10f : 9f, localRoadColor);
            foreach (var x in HarborlightDistrictSurfaceNetwork.NorthSouthRoads)
            {
                var endZ = x == -20f ? -32.5f : x == 18f ? -34f : 30f;
                DrawWorldRoad(map, x, HarborlightDistrictSurfaceNetwork.CoreRoadSouthStartZ, x, endZ,
                    x == -20f ? 7f : 8f, localRoadColor);
            }
            var outerEastWestRoads = HarborlightDistrictSurfaceNetwork.OuterEastWestRoads;
            for (var row = 0; row < outerEastWestRoads.Length; row++)
            {
                if (!HarborlightDistrictSurfaceNetwork.IsOuterEastWestRoadActive(row)) continue;
                var arterial = row % 5 == 0;
                DrawWorldRoad(map, boundsMinX + 4f, outerEastWestRoads[row], boundsMaxX - 4f,
                    outerEastWestRoads[row], arterial ? 16f : 9f, arterial ? arterialColor : localRoadColor);
            }
            DrawWorldRoad(map, HarborlightDistrictSurfaceNetwork.WorldMinXAtZ(
                    HarborlightDistrictSurfaceNetwork.OuterCityGridSouthZ) + 4f,
                HarborlightDistrictSurfaceNetwork.OuterCityGridSouthZ,
                HarborlightDistrictSurfaceNetwork.WorldMaxXAtZ(
                    HarborlightDistrictSurfaceNetwork.OuterCityGridSouthZ) - 4f,
                HarborlightDistrictSurfaceNetwork.OuterCityGridSouthZ, 16f, arterialColor);
            var outerNorthSouthRoads = HarborlightDistrictSurfaceNetwork.OuterNorthSouthRoads;
            for (var column = 0; column < outerNorthSouthRoads.Length; column++)
            {
                var x = outerNorthSouthRoads[column];
                var southStart = HarborlightDistrictSurfaceNetwork.WorldMinZAtX(x) +
                    HarborlightDistrictSurfaceNetwork.OuterRoadBoundaryClearance;
                var cityStart = Mathf.Max(southStart, HarborlightDistrictSurfaceNetwork.OuterCityGridSouthZ);
                var arterial = column % 3 == 1;
                if (cityStart < HarborlightDistrictSurfaceNetwork.CoreRoadSouthStartZ + 1f)
                    DrawWorldRoad(map, x, cityStart, x,
                        HarborlightDistrictSurfaceNetwork.CoreRoadSouthStartZ + 1f,
                        arterial ? 14f : 7.5f, arterial ? arterialColor : localRoadColor);
                if (arterial && southStart < HarborlightDistrictSurfaceNetwork.OuterCityGridSouthZ)
                    DrawWorldRoad(map, x, southStart, x,
                        HarborlightDistrictSurfaceNetwork.OuterCityGridSouthZ, 14f, arterialColor);
            }
            DrawWorldPolyline(map, HarborlightDistrictSurfaceNetwork.BaylineParkway, 18f,
                new Color(0.81f, 0.75f, 0.58f, 1f));
            DrawWorldPolyline(map, HarborlightDistrictSurfaceNetwork.SouthgateConnector, 13f,
                new Color(0.63f, 0.72f, 0.66f, 1f));
            DrawWorldPolyline(map, HarborlightDistrictSurfaceNetwork.CedarValeScenicRoute, 12f,
                new Color(0.63f, 0.72f, 0.66f, 1f));
            GUI.DrawTexture(map, _mapWaterMask);
            DrawCoastline(map);
            DrawLandPerimeter(map);

            DrawWorldArea(map, -15f, 15f, 28f, 40f, new Color(0.56f, 0.55f, 0.47f, 1f));
            DrawWorldArea(map, 9f, 17f, 28f, 48f, new Color(0.56f, 0.55f, 0.47f, 1f));
            foreach (var building in new[]
            {
                new Vector2(-31f, -24f), new Vector2(-10f, -25f), new Vector2(30f, -22f),
                new Vector2(-32f, 11f), new Vector2(-9f, 15f), new Vector2(32f, 16f), new Vector2(-29f, 31f),
                new Vector2(-85f, -66f), new Vector2(-42f, -66f), new Vector2(43f, -66f), new Vector2(85f, -66f),
                new Vector2(-85f, -112f), new Vector2(43f, -112f), new Vector2(85f, -112f)
            })
                DrawWorldArea(map, building.x - 7f, building.x + 7f, building.y - 7f, building.y + 7f,
                    new Color(0.40f, 0.36f, 0.32f, 1f));
            if (map.width > 300f)
            {
                DrawDistrictLabel(map, "WEST WORKS", -1450f, -475f, 140f);
                DrawDistrictLabel(map, "EAST DOCKS", 1450f, -475f, 140f);
                DrawDistrictLabel(map, "SOUTHGATE", 0f, -1120f, 160f);
                DrawDistrictLabel(map, "WEST RIDGE", -1450f, -1330f, 140f);
                DrawDistrictLabel(map, "EAST RIDGE", 1450f, -1330f, 140f);
                DrawDistrictLabel(map, "SOUTHPORT", 0f, -1900f, 160f);
                DrawDistrictLabel(map, "SOUTHPORT FIELDS", 0f, -2700f, 190f);
                DrawDistrictLabel(map, "WEST BLUFFS", -2500f, -1200f, 130f);
                DrawDistrictLabel(map, "EAST BLUFFS", 2500f, -1200f, 130f);
                DrawDistrictLabel(map, "CEDAR VALE", 0f, -3550f, 150f);
                DrawDistrictLabel(map, "BAYLINE PARKWAY", 300f, -1735f, 220f);
                DrawDistrictLabel(map, "HARBOR RIDGE ROUTE", -2110f, -690f, 205f);
                DrawDistrictLabel(map, "CEDAR VALE ROUTE", 1670f, -3420f, 190f);
                DrawMapLegend(map);
                DrawMapServiceLegend(map);
                DrawMapServiceMarkers(map);
            }

            var beacon = MapPoint(map, HarborlightGameState.BeaconPosition, 8f);
            var courier = MapPoint(map, _state.CourierObjectivePosition, 7f);
            var inspection = MapPoint(map, _state.InspectionObjectivePosition, 7f);
            var player = _player != null ? _player.position : Vector3.zero;
            var marker = MapPoint(map, player, 8f);
            DrawMapRect(beacon, new Color(0.97f, 0.69f, 0.31f, 1f));
            DrawMapRect(courier, new Color(0.98f, 0.80f, 0.38f, 1f));
            DrawMapRect(inspection, new Color(0.54f, 0.83f, 0.62f, 1f));

            RefreshNetworkMapPlayers();
            if (_networkMapPlayers != null)
            {
                var otherPlayerColor = new Color(0.39f, 0.70f, 0.96f, 1f);
                var otherPlayerMarkerSize = map.width > 300f ? 7f : 4f;
                foreach (var networkPlayer in _networkMapPlayers)
                {
                    if (networkPlayer == null || !networkPlayer.IsSpawned || networkPlayer.IsOwner) continue;
                    DrawMapRect(MapPoint(map, networkPlayer.transform.position, otherPlayerMarkerSize), otherPlayerColor);
                }
            }

            DrawMapRect(marker, new Color(0.32f, 0.88f, 0.78f, 1f));
            if (map.width < 400f)
                GUI.Label(new Rect(map.x + 2f, map.y - 19f, map.width - 4f, 18f), "CITY MAP · TAP TO EXPAND", _mapLabel);
        }

        private void RefreshNetworkMapPlayers()
        {
            if (Time.unscaledTime < _nextNetworkMapPlayerRefreshAt) return;
            _nextNetworkMapPlayerRefreshAt = Time.unscaledTime + 1f;
            _networkMapPlayers = FindObjectsByType<HarborlightNetworkPlayer>();
        }

        private void DrawMapLegend(Rect map)
        {
            var y = map.y + 28f;
            var x = map.x + 12f;
            DrawMapRect(new Rect(x, y + 4f, 9f, 9f), new Color(0.32f, 0.88f, 0.78f, 1f));
            GUI.Label(new Rect(x + 13f, y, 36f, 18f), "YOU", _small);
            x += 54f;
            DrawMapRect(new Rect(x, y + 4f, 9f, 9f), new Color(0.39f, 0.70f, 0.96f, 1f));
            GUI.Label(new Rect(x + 13f, y, 70f, 18f), "NEARBY", _small);
        }

        private void DrawMapServiceLegend(Rect map)
        {
            var y = map.y + 48f;
            DrawMapRect(new Rect(map.x + 6f, y - 2f, map.width - 12f, 20f),
                new Color(0.035f, 0.06f, 0.065f, 0.82f));
            var x = map.x + 12f;
            DrawServiceLegendItem(ref x, y, "BEACON", new Color(0.97f, 0.69f, 0.31f, 1f), 76f);
            DrawServiceLegendItem(ref x, y, "MARKET", new Color(0.98f, 0.80f, 0.38f, 1f), 78f);
            DrawServiceLegendItem(ref x, y, "PIER OFFICE", new Color(0.54f, 0.83f, 0.62f, 1f), 103f);
            DrawServiceLegendItem(ref x, y, "MOTOR WORKS", new Color(0.74f, 0.63f, 0.91f, 1f), 112f);
            DrawServiceLegendItem(ref x, y, "BANK", new Color(0.43f, 0.72f, 0.93f, 1f), 64f);
        }

        private void DrawMapServiceMarkers(Rect map)
        {
            DrawServiceMarker(map, HarborlightGameState.BeaconPosition,
                new Color(0.97f, 0.69f, 0.31f, 1f));
            DrawServiceMarker(map, HarborlightGameState.CourierContactPosition,
                new Color(0.98f, 0.80f, 0.38f, 1f));
            DrawServiceMarker(map, new Vector3(23f, 0f, 8f),
                new Color(0.54f, 0.83f, 0.62f, 1f));
            DrawServiceMarker(map, HarborlightGameState.GaragePosition,
                new Color(0.74f, 0.63f, 0.91f, 1f));
            DrawServiceMarker(map, new Vector3(-42f, 0f, 7f),
                new Color(0.43f, 0.72f, 0.93f, 1f));
        }

        private void DrawServiceLegendItem(ref float x, float y, string label, Color color, float advance)
        {
            DrawMapRect(new Rect(x, y + 4f, 8f, 8f), color);
            GUI.Label(new Rect(x + 12f, y, advance - 12f, 18f), label, _small);
            x += advance;
        }

        private static void DrawServiceMarker(Rect map, Vector3 position, Color color)
        {
            DrawMapRect(MapPoint(map, position, 9f), color);
        }

        private void DrawDistrictLabel(Rect map, string label, float x, float z, float width)
        {
            var position = WorldMapPoint(map, x, z);
            GUI.Label(new Rect(position.x - width * 0.5f, position.y - 14f, width, 28f),
                label, _mapDistrictLabel);
        }

        private static Rect MapPoint(Rect map, Vector3 worldPosition, float size)
        {
            var x = Mathf.InverseLerp(HarborlightDistrictSurfaceNetwork.WorldMinX,
                HarborlightDistrictSurfaceNetwork.WorldMaxX, worldPosition.x);
            var y = 1f - Mathf.InverseLerp(HarborlightDistrictSurfaceNetwork.WorldMinZ,
                HarborlightDistrictSurfaceNetwork.WorldMaxZ, worldPosition.z);
            return new Rect(map.x + x * map.width - size * 0.5f, map.y + y * map.height - size * 0.5f, size, size);
        }

        private static void DrawWorldRoad(Rect map, float startX, float startZ, float endX, float endZ,
            float width, Color color)
        {
            var xMin = HarborlightDistrictSurfaceNetwork.WorldMinX;
            var xMax = HarborlightDistrictSurfaceNetwork.WorldMaxX;
            var zMin = HarborlightDistrictSurfaceNetwork.WorldMinZ;
            var zMax = HarborlightDistrictSurfaceNetwork.WorldMaxZ;
            var start = WorldMapPoint(map, startX, startZ);
            var end = WorldMapPoint(map, endX, endZ);
            if (Mathf.Abs(startZ - endZ) < 0.01f)
            {
                var thickness = width / (zMax - zMin) * map.height;
                DrawMapRect(new Rect(Mathf.Min(start.x, end.x), start.y - thickness * 0.5f,
                    Mathf.Abs(end.x - start.x), thickness), color);
            }
            else
            {
                var thickness = width / (xMax - xMin) * map.width;
                DrawMapRect(new Rect(start.x - thickness * 0.5f, Mathf.Min(start.y, end.y),
                    thickness, Mathf.Abs(end.y - start.y)), color);
            }
        }

        private static void DrawWorldArea(Rect map, float minX, float maxX, float minZ, float maxZ, Color color)
        {
            var topLeft = WorldMapPoint(map, minX, maxZ);
            var bottomRight = WorldMapPoint(map, maxX, minZ);
            DrawMapRect(Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y), color);
        }

        private static void DrawCoastalWater(Rect map)
        {
            const int segments = 64;
            var minX = HarborlightDistrictSurfaceNetwork.WorldMinX;
            var maxX = HarborlightDistrictSurfaceNetwork.WorldMaxX;
            var color = new Color(0.16f, 0.34f, 0.39f, 1f);
            for (var i = 0; i < segments; i++)
            {
                var x0 = Mathf.Lerp(minX, maxX, (float)i / segments);
                var x1 = Mathf.Lerp(minX, maxX, (float)(i + 1) / segments);
                var y0 = WorldMapPoint(map, x0,
                    HarborlightDistrictSurfaceNetwork.CoastlineZ(x0)).y;
                var y1 = WorldMapPoint(map, x1,
                    HarborlightDistrictSurfaceNetwork.CoastlineZ(x1)).y;
                DrawMapRect(Rect.MinMaxRect(map.x + map.width * i / segments, map.y,
                    map.x + map.width * (i + 1) / segments, Mathf.Max(y0, y1)), color);
            }
        }

        private static void DrawCoastline(Rect map)
        {
            const int segments = 64;
            var minX = HarborlightDistrictSurfaceNetwork.WorldMinX;
            var maxX = HarborlightDistrictSurfaceNetwork.WorldMaxX;
            var color = new Color(0.79f, 0.72f, 0.56f, 1f);
            var previous = WorldMapPoint(map, minX, HarborlightDistrictSurfaceNetwork.CoastlineZ(minX));
            for (var i = 1; i <= segments; i++)
            {
                var x = Mathf.Lerp(minX, maxX, (float)i / segments);
                var next = WorldMapPoint(map, x, HarborlightDistrictSurfaceNetwork.CoastlineZ(x));
                DrawMapLine(previous, next, map.width > 300f ? 2f : 1.25f, color);
                previous = next;
            }
        }

        private static void DrawLandPerimeter(Rect map)
        {
            var color = new Color(0.79f, 0.72f, 0.56f, 1f);
            var eastStartZ = HarborlightDistrictSurfaceNetwork.CoastlineZ(
                HarborlightDistrictSurfaceNetwork.WorldMaxX);
            var previous = WorldMapPoint(map,
                HarborlightDistrictSurfaceNetwork.WorldMaxXAtZ(eastStartZ), eastStartZ);
            const int segments = 48;
            for (var i = 1; i <= segments; i++)
            {
                var z = Mathf.Lerp(eastStartZ, HarborlightDistrictSurfaceNetwork.WorldMinZ,
                    (float)i / segments);
                var next = WorldMapPoint(map, HarborlightDistrictSurfaceNetwork.WorldMaxXAtZ(z), z);
                DrawMapLine(previous, next, 1.5f, color);
                previous = next;
            }

            var bottomRight = WorldMapPoint(map,
                HarborlightDistrictSurfaceNetwork.WorldMaxXAtZ(HarborlightDistrictSurfaceNetwork.WorldMinZ),
                HarborlightDistrictSurfaceNetwork.WorldMinZ);
            var bottomLeft = WorldMapPoint(map,
                HarborlightDistrictSurfaceNetwork.WorldMinXAtZ(HarborlightDistrictSurfaceNetwork.WorldMinZ),
                HarborlightDistrictSurfaceNetwork.WorldMinZ);
            DrawMapLine(bottomRight, bottomLeft, 1.5f, color);

            var westEndZ = HarborlightDistrictSurfaceNetwork.CoastlineZ(
                HarborlightDistrictSurfaceNetwork.WorldMinX);
            previous = bottomLeft;
            for (var i = 1; i <= segments; i++)
            {
                var z = Mathf.Lerp(HarborlightDistrictSurfaceNetwork.WorldMinZ, westEndZ,
                    (float)i / segments);
                var next = WorldMapPoint(map, HarborlightDistrictSurfaceNetwork.WorldMinXAtZ(z), z);
                DrawMapLine(previous, next, 1.5f, color);
                previous = next;
            }
        }

        private static void DrawWorldPolyline(Rect map, Vector2[] points, float worldWidth, Color color)
        {
            if (points == null || points.Length < 2) return;
            var thickness = Mathf.Max(map.width > 300f ? 2f : 1.25f,
                worldWidth / (HarborlightDistrictSurfaceNetwork.WorldMaxX -
                              HarborlightDistrictSurfaceNetwork.WorldMinX) * map.width);
            for (var i = 0; i + 1 < points.Length; i++)
            {
                var start = WorldMapPoint(map, points[i].x, points[i].y);
                var end = WorldMapPoint(map, points[i + 1].x, points[i + 1].y);
                DrawMapLine(start, end, thickness, color);
            }
        }

        private static void DrawMapLine(Vector2 start, Vector2 end, float thickness, Color color)
        {
            var delta = end - start;
            var center = (start + end) * 0.5f;
            var previousMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, center);
            DrawMapRect(new Rect(center.x - delta.magnitude * 0.5f, center.y - thickness * 0.5f,
                delta.magnitude, thickness), color);
            GUI.matrix = previousMatrix;
        }

        private static Vector2 WorldMapPoint(Rect map, float x, float z)
        {
            var normalizedX = Mathf.InverseLerp(HarborlightDistrictSurfaceNetwork.WorldMinX,
                HarborlightDistrictSurfaceNetwork.WorldMaxX, x);
            var normalizedY = 1f - Mathf.InverseLerp(HarborlightDistrictSurfaceNetwork.WorldMinZ,
                HarborlightDistrictSurfaceNetwork.WorldMaxZ, z);
            return new Vector2(map.x + normalizedX * map.width, map.y + normalizedY * map.height);
        }

        private static void DrawMapRect(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static Texture2D SolidTexture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateMapWaterMask()
        {
            const int width = 320;
            const int height = 224;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Harborlight peninsula map water mask",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var water = new Color32(41, 87, 98, 255);
            var clear = new Color32(0, 0, 0, 0);
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                var z = Mathf.Lerp(HarborlightDistrictSurfaceNetwork.WorldMinZ,
                    HarborlightDistrictSurfaceNetwork.WorldMaxZ, (float)y / (height - 1));
                for (var x = 0; x < width; x++)
                {
                    var worldX = Mathf.Lerp(HarborlightDistrictSurfaceNetwork.WorldMinX,
                        HarborlightDistrictSurfaceNetwork.WorldMaxX, (float)x / (width - 1));
                    var land = z <= HarborlightDistrictSurfaceNetwork.CoastlineZ(worldX) &&
                               worldX >= HarborlightDistrictSurfaceNetwork.WorldMinXAtZ(z) &&
                               worldX <= HarborlightDistrictSurfaceNetwork.WorldMaxXAtZ(z);
                    pixels[y * width + x] = land ? clear : water;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D CreateMapDistrictTexture()
        {
            const int width = 320;
            const int height = 224;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Harborlight curved district map",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[width * height];
            var clear = new Color32(0, 0, 0, 0);
            var westWorks = new Color32(105, 91, 86, 255);
            var eastDocks = new Color32(89, 104, 101, 255);
            var southgate = new Color32(77, 97, 87, 255);
            var westRidge = new Color32(71, 104, 81, 255);
            var eastRidge = new Color32(79, 99, 99, 255);
            var westBluffs = new Color32(89, 97, 107, 255);
            var eastBluffs = new Color32(87, 100, 112, 255);
            var southport = new Color32(77, 102, 94, 255);
            var fields = new Color32(110, 102, 77, 255);
            var cedarVale = new Color32(107, 97, 81, 255);

            for (var y = 0; y < height; y++)
            {
                var z = Mathf.Lerp(HarborlightDistrictSurfaceNetwork.WorldMinZ,
                    HarborlightDistrictSurfaceNetwork.WorldMaxZ, (float)y / (height - 1));
                for (var x = 0; x < width; x++)
                {
                    var worldX = Mathf.Lerp(HarborlightDistrictSurfaceNetwork.WorldMinX,
                        HarborlightDistrictSurfaceNetwork.WorldMaxX, (float)x / (width - 1));
                    if (z > HarborlightDistrictSurfaceNetwork.CoastlineZ(worldX) ||
                        worldX < HarborlightDistrictSurfaceNetwork.WorldMinXAtZ(z) ||
                        worldX > HarborlightDistrictSurfaceNetwork.WorldMaxXAtZ(z))
                    {
                        pixels[y * width + x] = clear;
                        continue;
                    }

                    var northBoundary = -540f + 105f * Mathf.Sin(worldX * 0.0013f + 0.4f);
                    var ridgeBoundary = -1640f + 145f * Mathf.Sin(worldX * 0.0011f - 0.7f);
                    var portBoundary = -2320f + 155f * Mathf.Sin(worldX * 0.0015f + 1.1f);
                    var fieldBoundary = -3160f + 115f * Mathf.Sin(worldX * 0.0012f - 0.5f);
                    var westBoundary = -900f + 125f * Mathf.Sin(z * 0.0014f + 0.6f);
                    var eastBoundary = 950f + 135f * Mathf.Sin(z * 0.0012f - 0.8f);

                    Color32 district;
                    if (z > northBoundary)
                        district = worldX < westBoundary ? westWorks : worldX > eastBoundary ? eastDocks : southgate;
                    else if (z > ridgeBoundary)
                        district = worldX < -2180f ? westBluffs
                            : worldX < -350f ? westRidge
                            : worldX < 850f ? southgate
                            : worldX < 2180f ? eastRidge
                            : eastBluffs;
                    else if (z > portBoundary)
                        district = southport;
                    else if (z > fieldBoundary)
                        district = fields;
                    else
                        district = cedarVale;

                    pixels[y * width + x] = district;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
