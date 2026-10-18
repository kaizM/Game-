using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Harborlight
{
    [CreateAssetMenu(menuName = "Harborlight/Backend Settings")]
    public sealed class HarborlightBackendSettings : ScriptableObject
    {
        [Tooltip("HTTPS API base URL for staging/production. Leave blank to keep the game fully offline.")]
        public string ApiBaseUrl = "";
        [Tooltip("Base64 SubjectPublicKeyInfo RSA key used to encrypt short-lived realtime join tickets for NGO connection approval.")]
        public string RealtimeJoinTicketPublicKeyBase64 = "";
    }

    public sealed class HarborlightBackendClient : MonoBehaviour
    {
        [Serializable] public sealed class WalletSnapshot { public string playerId; public int gems; }
        [Serializable] public sealed class BankSnapshot { public int cashCredits; public int bankCredits; }
        [Serializable] public sealed class BankTransferResult
        {
            public bool succeeded;
            public bool replayed;
            public string result;
            public int cashCredits;
            public int bankCredits;
        }
        [Serializable] public sealed class Cosmetic { public string sku; public string displayName; public int priceGems; }
        [Serializable] public sealed class InventoryCatalogItem
        {
            public string sku;
            public string displayName;
            public string category;
            public int priceCredits;
            public int weightGrams;
            public int maxStack;
            public string equipmentSlot;
        }
        [Serializable] public sealed class InventoryItem
        {
            public string sku;
            public string displayName;
            public string category;
            public int quantity;
            public int weightGrams;
            public bool isEquipped;
            public string equipmentSlot;
        }
        [Serializable] public sealed class InventoryPurchaseResult
        {
            public bool succeeded;
            public bool replayed;
            public string result;
            public string sku;
            public int itemQuantity;
            public int cashCredits;
            public int weightGrams;
            public int capacityGrams;
        }
        [Serializable] public sealed class InventoryEquipResult
        {
            public bool succeeded;
            public bool replayed;
            public string result;
            public string sku;
            public bool equipped;
        }
        [Serializable] private sealed class GuestSession { public string accessToken; public string expiresAtUtc; }
        [Serializable] private sealed class RealtimeJoinTicket { public string ticket; public string expiresAtUtc; }
        [Serializable] private sealed class CosmeticList { public Cosmetic[] items; }
        [Serializable] private sealed class InventoryCatalogList { public InventoryCatalogItem[] items; }
        [Serializable] private sealed class InventorySnapshot
        {
            public string playerId;
            public int cashCredits;
            public int weightGrams;
            public int capacityGrams;
            public InventoryItem[] items;
        }
        [Serializable] private sealed class SpendRequest { public string sku; }
        [Serializable] private sealed class SpendResult { public bool succeeded; public int balanceGems; public string result; }
        [Serializable] private sealed class BankTransferRequest { public string direction; public int amount; }
        [Serializable] private sealed class InventoryPurchaseRequest { public string sku; public int quantity; }
        [Serializable] private sealed class InventoryEquipRequest { public string sku; public bool equipped; }
        [Serializable] private sealed class BankTransactionList { public HarborlightBankTransaction[] items; }

        private const string SessionFileName = "harborlight-session-token";
        private static string PendingBankKeyPreference => HarborlightLocalProfileStorage.PlayerPrefsKey("harborlight-bank-pending-key");
        private static string PendingBankRequestPreference => HarborlightLocalProfileStorage.PlayerPrefsKey("harborlight-bank-pending-request");
        private static string PendingInventoryKeyPreference => HarborlightLocalProfileStorage.PlayerPrefsKey("harborlight-inventory-pending-key");
        private static string PendingInventoryRequestPreference => HarborlightLocalProfileStorage.PlayerPrefsKey("harborlight-inventory-pending-request");
        private string _baseUrl;
        private string _token;
        private bool _connecting;
        private bool _requestingRealtimeJoinTicket;
        private bool _bankLoading;
        private bool _bankTransferring;
        private bool _inventoryLoading;
        private bool _inventoryMutating;
        private readonly Dictionary<string, string> _pendingSpendKeys = new Dictionary<string, string>();
        private readonly List<HarborlightBankTransaction> _bankTransactions = new List<HarborlightBankTransaction>();

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_baseUrl);
        public string JoinTicketPublicKeyBase64 { get; private set; } = "";
        public bool IsConnected { get; private set; }
        public int Gems { get; private set; }
        public int BankCashCredits { get; private set; }
        public int BankCredits { get; private set; }
        public bool BankLoaded { get; private set; }
        public bool IsBankBusy => _bankLoading || _bankTransferring;
        public IReadOnlyList<HarborlightBankTransaction> BankTransactions => _bankTransactions;
        public string Status { get; private set; } = "Offline play available";
        public IReadOnlyList<Cosmetic> Catalog { get; private set; } = new Cosmetic[0];
        public IReadOnlyList<Cosmetic> OwnedCosmetics { get; private set; } = new Cosmetic[0];
        public IReadOnlyList<InventoryCatalogItem> InventoryCatalog { get; private set; } = new InventoryCatalogItem[0];
        public IReadOnlyList<InventoryItem> InventoryItems { get; private set; } = new InventoryItem[0];
        public int InventoryCashCredits { get; private set; }
        public int InventoryWeightGrams { get; private set; }
        public int InventoryCapacityGrams { get; private set; }
        public bool InventoryLoaded { get; private set; }
        public bool IsInventoryBusy => _inventoryLoading || _inventoryMutating;
        public bool HasPendingInventoryAction
            => !string.IsNullOrEmpty(PlayerPrefs.GetString(PendingInventoryKeyPreference, ""));

        public void Connect(HarborlightBackendSettings settings)
        {
            var apiBaseUrl = settings != null ? settings.ApiBaseUrl.Trim() : "";
            JoinTicketPublicKeyBase64 = settings != null ? settings.RealtimeJoinTicketPublicKeyBase64.Trim() : "";
            if (Debug.isDebugBuild)
            {
                var localApiBaseUrl = Environment.GetEnvironmentVariable("HARBORLIGHT_API_BASE_URL");
                var localPublicKey = Environment.GetEnvironmentVariable("HARBORLIGHT_REALTIME_TICKET_PUBLIC_KEY_BASE64");
                if (!string.IsNullOrWhiteSpace(localApiBaseUrl)) apiBaseUrl = localApiBaseUrl.Trim();
                if (!string.IsNullOrWhiteSpace(localPublicKey)) JoinTicketPublicKeyBase64 = localPublicKey.Trim();
            }

            _baseUrl = "";
            if (Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var uri)
                && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment)
                && (uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)))
                _baseUrl = uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
            else if (!string.IsNullOrWhiteSpace(apiBaseUrl))
                Debug.LogError("Harborlight API URL must use HTTPS (HTTP is allowed only for loopback development). Online services remain offline.");
            if (_connecting || !IsConfigured) return;
            StartCoroutine(ConnectRoutine());
        }

        public void RequestRealtimeJoinTicket(Action<string, string> completed)
        {
            if (_requestingRealtimeJoinTicket)
            {
                completed?.Invoke(null, "A Harborlight sign-in request is already running.");
                return;
            }
            StartCoroutine(RequestRealtimeJoinTicketRoutine(completed));
        }

        public bool TryEncryptRealtimeJoinTicket(string ticket, out byte[] encryptedTicket)
        {
            encryptedTicket = null;
            if (string.IsNullOrWhiteSpace(ticket) || string.IsNullOrWhiteSpace(JoinTicketPublicKeyBase64)) return false;
            var publicKey = Array.Empty<byte>();
            RSA rsa = null;
            try
            {
                publicKey = Convert.FromBase64String(JoinTicketPublicKeyBase64);
                rsa = HarborlightRsaKeyImport.CreateFromSubjectPublicKeyInfo(publicKey);
            }
            catch (CryptographicException)
            {
                Debug.LogWarning("Harborlight realtime RSA public key could not be imported.");
                return false;
            }
            catch (FormatException)
            {
                Debug.LogWarning("Harborlight realtime public key is not valid base64.");
                return false;
            }
            catch (PlatformNotSupportedException)
            {
                Debug.LogWarning("Harborlight realtime RSA public-key import is unsupported by this runtime.");
                return false;
            }

            try
            {
                using (rsa)
                    encryptedTicket = rsa.Encrypt(Encoding.UTF8.GetBytes(ticket), HarborlightRsaKeyImport.JoinTicketPadding);
                return encryptedTicket.Length <= 512;
            }
            catch (CryptographicException exception)
            {
                Debug.LogWarning("Harborlight realtime OAEP-SHA256 encryption failed (" + exception.GetType().Name + ").");
                return false;
            }
            catch (PlatformNotSupportedException)
            {
                Debug.LogWarning("Harborlight realtime OAEP-SHA256 encryption is unsupported by this runtime.");
                return false;
            }
        }

        public void SpendCosmetic(string sku, Action<bool, string> completed)
        {
            if (!IsConnected)
            {
                completed?.Invoke(false, "Connect to Harborlight to use the online garage.");
                return;
            }
            StartCoroutine(SpendCosmeticRoutine(sku, completed));
        }

        private IEnumerator SpendCosmeticRoutine(string sku, Action<bool, string> completed)
        {
            SpendResult result = null;
            string error = null;
            long responseCode = 0;
            if (!_pendingSpendKeys.TryGetValue(sku, out var idempotencyKey))
            {
                idempotencyKey = Guid.NewGuid().ToString("D");
                _pendingSpendKeys[sku] = idempotencyKey;
            }
            yield return Send("POST", "/api/v1/wallet/spend", JsonUtility.ToJson(new SpendRequest { sku = sku }), true,
                idempotencyKey, (request, body) =>
                {
                    responseCode = request.responseCode;
                    if (request.responseCode >= 200 && request.responseCode < 300)
                        result = JsonUtility.FromJson<SpendResult>(body);
                    else error = ErrorMessage(request, body);
                });

            if (responseCode != 0) _pendingSpendKeys.Remove(sku);
            if (result == null)
            {
                if (responseCode == 409)
                {
                    yield return RefreshWallet();
                    yield return RefreshCosmetics();
                    if (ContainsOwnedCosmetic(sku))
                    {
                        completed?.Invoke(true, "This finish is already in your collection.");
                        yield break;
                    }
                }
                completed?.Invoke(false, error ?? "The server returned an invalid spend response.");
                yield break;
            }

            Gems = result.balanceGems;
            if (result.succeeded) yield return RefreshCosmetics();
            completed?.Invoke(result.succeeded, result.result);
        }

        public void LoadBank(Action<bool, string> completed)
        {
            if (!IsConnected)
            {
                completed?.Invoke(false, "Online banking is unavailable. Local banking is still available.");
                return;
            }
            if (_bankLoading) return;
            StartCoroutine(LoadBankRoutine(completed));
        }

        private IEnumerator LoadBankRoutine(Action<bool, string> completed)
        {
            _bankLoading = true;
            BankSnapshot snapshot = null;
            string error = null;
            yield return Send("GET", "/api/v1/bank", null, true, null, (request, body) =>
            {
                if (request.responseCode >= 200 && request.responseCode < 300)
                    snapshot = JsonUtility.FromJson<BankSnapshot>(body);
                else error = ErrorMessage(request, body);
            });

            if (snapshot == null)
            {
                _bankLoading = false;
                completed?.Invoke(false, error ?? "The bank returned an invalid balance snapshot.");
                yield break;
            }

            BankCashCredits = Mathf.Clamp(snapshot.cashCredits, 0, 999999);
            BankCredits = Mathf.Clamp(snapshot.bankCredits, 0, 999999);
            _bankTransactions.Clear();
            yield return Send("GET", "/api/v1/bank/transactions?limit=8", null, true, null, (request, body) =>
            {
                if (request.responseCode < 200 || request.responseCode >= 300) return;
                var parsed = JsonUtility.FromJson<BankTransactionList>("{\"items\":" + body + "}");
                if (parsed?.items == null) return;
                foreach (var transaction in parsed.items)
                    if (transaction != null) _bankTransactions.Add(transaction);
            });

            BankLoaded = true;
            _bankLoading = false;
            completed?.Invoke(true, "Bank balances updated.");
        }

        public void TransferBank(string direction, int amount, Action<bool, string> completed)
        {
            direction = direction != null ? direction.Trim().ToLowerInvariant() : "";
            if (!IsConnected || (direction != "deposit" && direction != "withdraw") || amount < 1 || amount > 999999)
            {
                completed?.Invoke(false, "Online bank transfer is unavailable or invalid.");
                return;
            }
            if (_bankTransferring) return;

            var requestValue = direction + ":" + amount;
            var pendingRequest = PlayerPrefs.GetString(PendingBankRequestPreference, "");
            var idempotencyKey = PlayerPrefs.GetString(PendingBankKeyPreference, "");
            if (!string.IsNullOrEmpty(idempotencyKey) && pendingRequest != requestValue)
            {
                completed?.Invoke(false, "Retry the pending " + pendingRequest.Replace(":", " ") + " transfer first.");
                return;
            }
            if (string.IsNullOrEmpty(idempotencyKey))
            {
                idempotencyKey = Guid.NewGuid().ToString("D");
                PlayerPrefs.SetString(PendingBankKeyPreference, idempotencyKey);
                PlayerPrefs.SetString(PendingBankRequestPreference, requestValue);
                PlayerPrefs.Save();
            }
            StartCoroutine(TransferBankRoutine(direction, amount, idempotencyKey, completed));
        }

        private IEnumerator TransferBankRoutine(string direction, int amount, string idempotencyKey,
            Action<bool, string> completed)
        {
            _bankTransferring = true;
            BankTransferResult transfer = null;
            string error = null;
            long responseCode = 0;
            yield return Send("POST", "/api/v1/bank/transfer",
                JsonUtility.ToJson(new BankTransferRequest { direction = direction, amount = amount }), true,
                idempotencyKey, (request, body) =>
                {
                    responseCode = request.responseCode;
                    if (request.responseCode >= 200 && request.responseCode < 300)
                        transfer = JsonUtility.FromJson<BankTransferResult>(body);
                    else error = ErrorMessage(request, body);
                });

            if (transfer != null)
            {
                BankCashCredits = Mathf.Clamp(transfer.cashCredits, 0, 999999);
                BankCredits = Mathf.Clamp(transfer.bankCredits, 0, 999999);
                ClearPendingBankTransfer();
                yield return LoadBankRoutine(null);
                _bankTransferring = false;
                completed?.Invoke(transfer.succeeded, transfer.result);
                yield break;
            }

            if (responseCode >= 400 && responseCode < 500) ClearPendingBankTransfer();
            _bankTransferring = false;
            completed?.Invoke(false, responseCode == 0 || responseCode >= 500
                ? "Transfer result is not confirmed. Retry the same amount to safely check it."
                : error ?? "The bank rejected the transfer.");
        }

        private static void ClearPendingBankTransfer()
        {
            PlayerPrefs.DeleteKey(PendingBankKeyPreference);
            PlayerPrefs.DeleteKey(PendingBankRequestPreference);
            PlayerPrefs.Save();
        }

        public void LoadInventory(Action<bool, string> completed)
        {
            if (!IsConnected)
            {
                completed?.Invoke(false, "Online inventory is unavailable. Offline play is still available.");
                return;
            }
            if (_inventoryLoading)
            {
                completed?.Invoke(false, "Inventory is already updating.");
                return;
            }
            StartCoroutine(LoadInventoryRoutine(completed));
        }

        private IEnumerator LoadInventoryRoutine(Action<bool, string> completed)
        {
            _inventoryLoading = true;
            string catalogJson = null;
            InventorySnapshot snapshot = null;
            string error = null;
            yield return Send("GET", "/api/v1/inventory/catalog", null, true, null, (request, body) =>
            {
                if (request.responseCode >= 200 && request.responseCode < 300) catalogJson = body;
                else error = ErrorMessage(request, body);
            });

            if (!string.IsNullOrEmpty(catalogJson))
            {
                var list = JsonUtility.FromJson<InventoryCatalogList>("{\"items\":" + catalogJson + "}");
                if (list?.items == null) error = "The inventory catalog response was invalid.";
                else InventoryCatalog = list.items;
            }

            if (error == null)
            {
                yield return Send("GET", "/api/v1/inventory", null, true, null, (request, body) =>
                {
                    if (request.responseCode >= 200 && request.responseCode < 300)
                        snapshot = JsonUtility.FromJson<InventorySnapshot>(body);
                    else error = ErrorMessage(request, body);
                });
            }

            if (snapshot?.items == null && error == null) error = "The inventory response was invalid.";
            if (snapshot != null && snapshot.items != null)
            {
                InventoryItems = snapshot.items;
                InventoryCashCredits = Mathf.Clamp(snapshot.cashCredits, 0, 999999);
                InventoryWeightGrams = Mathf.Clamp(snapshot.weightGrams, 0, 1000000);
                InventoryCapacityGrams = Mathf.Clamp(snapshot.capacityGrams, 1, 1000000);
                InventoryLoaded = true;
            }
            _inventoryLoading = false;
            completed?.Invoke(error == null, error ?? "Inventory updated.");
        }

        public void PurchaseInventoryItem(string sku, Action<bool, string> completed)
        {
            if (!IsConnected || string.IsNullOrWhiteSpace(sku))
            {
                completed?.Invoke(false, "Online inventory purchasing is unavailable.");
                return;
            }
            StartInventoryAction("purchase:" + sku + ":1", completed,
                key => PurchaseInventoryRoutine(sku, key, completed));
        }

        public void SetInventoryEquipped(string sku, bool equipped, Action<bool, string> completed)
        {
            if (!IsConnected || string.IsNullOrWhiteSpace(sku))
            {
                completed?.Invoke(false, "Online equipment updates are unavailable.");
                return;
            }
            StartInventoryAction("equip:" + sku + ":" + equipped, completed,
                key => EquipInventoryRoutine(sku, equipped, key, completed));
        }

        public void RetryPendingInventoryAction(Action<bool, string> completed)
        {
            var idempotencyKey = PlayerPrefs.GetString(PendingInventoryKeyPreference, "");
            var requestValue = PlayerPrefs.GetString(PendingInventoryRequestPreference, "");
            var parts = requestValue.Split(':');
            if (!IsConnected || string.IsNullOrEmpty(idempotencyKey) || parts.Length != 3)
            {
                completed?.Invoke(false, "There is no retryable inventory action.");
                return;
            }
            if (parts[0] == "purchase" && int.TryParse(parts[2], out var quantity) && quantity == 1)
            {
                StartInventoryAction(requestValue, completed,
                    key => PurchaseInventoryRoutine(parts[1], key, completed));
                return;
            }
            if (parts[0] == "equip" && bool.TryParse(parts[2], out var equipped))
            {
                StartInventoryAction(requestValue, completed,
                    key => EquipInventoryRoutine(parts[1], equipped, key, completed));
                return;
            }
            completed?.Invoke(false, "The saved inventory action could not be read.");
        }

        private void StartInventoryAction(string requestValue, Action<bool, string> completed,
            Func<string, IEnumerator> routineFactory)
        {
            if (_inventoryMutating)
            {
                completed?.Invoke(false, "Another inventory update is still running.");
                return;
            }
            var pendingRequest = PlayerPrefs.GetString(PendingInventoryRequestPreference, "");
            var idempotencyKey = PlayerPrefs.GetString(PendingInventoryKeyPreference, "");
            if (!string.IsNullOrEmpty(idempotencyKey) && pendingRequest != requestValue)
            {
                completed?.Invoke(false, "Retry the pending inventory action first.");
                return;
            }
            if (string.IsNullOrEmpty(idempotencyKey))
            {
                idempotencyKey = Guid.NewGuid().ToString("D");
                PlayerPrefs.SetString(PendingInventoryKeyPreference, idempotencyKey);
                PlayerPrefs.SetString(PendingInventoryRequestPreference, requestValue);
                PlayerPrefs.Save();
            }
            StartCoroutine(routineFactory(idempotencyKey));
        }

        private IEnumerator PurchaseInventoryRoutine(string sku, string idempotencyKey,
            Action<bool, string> completed)
        {
            _inventoryMutating = true;
            InventoryPurchaseResult result = null;
            string error = null;
            long responseCode = 0;
            yield return Send("POST", "/api/v1/inventory/purchase",
                JsonUtility.ToJson(new InventoryPurchaseRequest { sku = sku, quantity = 1 }), true,
                idempotencyKey, (request, body) =>
                {
                    responseCode = request.responseCode;
                    if (responseCode >= 200 && responseCode < 300)
                        result = JsonUtility.FromJson<InventoryPurchaseResult>(body);
                    else error = ErrorMessage(request, body);
                });

            if (result != null)
            {
                ClearPendingInventoryAction();
                InventoryCashCredits = Mathf.Clamp(result.cashCredits, 0, 999999);
                InventoryWeightGrams = Mathf.Clamp(result.weightGrams, 0, 1000000);
                InventoryCapacityGrams = Mathf.Clamp(result.capacityGrams, 1, 1000000);
                yield return LoadInventoryRoutine(null);
                _inventoryMutating = false;
                completed?.Invoke(result.succeeded, result.result);
                yield break;
            }

            if (responseCode >= 400 && responseCode < 500) ClearPendingInventoryAction();
            _inventoryMutating = false;
            completed?.Invoke(false, responseCode == 0 || responseCode >= 500
                ? "Purchase result is not confirmed. Retry the same item to safely check it."
                : error ?? "The server rejected the inventory purchase.");
        }

        private IEnumerator EquipInventoryRoutine(string sku, bool equipped, string idempotencyKey,
            Action<bool, string> completed)
        {
            _inventoryMutating = true;
            InventoryEquipResult result = null;
            string error = null;
            long responseCode = 0;
            yield return Send("POST", "/api/v1/inventory/equip",
                JsonUtility.ToJson(new InventoryEquipRequest { sku = sku, equipped = equipped }), true,
                idempotencyKey, (request, body) =>
                {
                    responseCode = request.responseCode;
                    if (responseCode >= 200 && responseCode < 300)
                        result = JsonUtility.FromJson<InventoryEquipResult>(body);
                    else error = ErrorMessage(request, body);
                });

            if (result != null)
            {
                ClearPendingInventoryAction();
                yield return LoadInventoryRoutine(null);
                _inventoryMutating = false;
                completed?.Invoke(result.succeeded, result.result);
                yield break;
            }

            if (responseCode >= 400 && responseCode < 500) ClearPendingInventoryAction();
            _inventoryMutating = false;
            completed?.Invoke(false, responseCode == 0 || responseCode >= 500
                ? "Equipment result is not confirmed. Retry the same action to safely check it."
                : error ?? "The server rejected the equipment update.");
        }

        private static void ClearPendingInventoryAction()
        {
            PlayerPrefs.DeleteKey(PendingInventoryKeyPreference);
            PlayerPrefs.DeleteKey(PendingInventoryRequestPreference);
            PlayerPrefs.Save();
        }

        private IEnumerator RequestRealtimeJoinTicketRoutine(Action<string, string> completed)
        {
            _requestingRealtimeJoinTicket = true;
            if (!_connecting && !IsConnected && IsConfigured) StartCoroutine(ConnectRoutine());
            while (_connecting) yield return null;

            if (!IsConnected || string.IsNullOrEmpty(_token))
            {
                _requestingRealtimeJoinTicket = false;
                completed?.Invoke(null, "Harborlight sign-in is unavailable.");
                yield break;
            }

            RealtimeJoinTicket issued = null;
            long responseCode = 0;
            yield return Send("POST", "/api/v1/realtime/join-tickets", "{}", true, null, (request, body) =>
            {
                responseCode = request.responseCode;
                if (request.responseCode >= 200 && request.responseCode < 300)
                    issued = JsonUtility.FromJson<RealtimeJoinTicket>(body);
            });

            _requestingRealtimeJoinTicket = false;
            if (issued == null || string.IsNullOrWhiteSpace(issued.ticket))
            {
                completed?.Invoke(null, responseCode == 0
                    ? "Harborlight sign-in could not reach the server."
                    : "Harborlight sign-in was rejected (HTTP " + responseCode + ").");
                yield break;
            }

            completed?.Invoke(issued.ticket, null);
        }

        private bool ContainsOwnedCosmetic(string sku)
        {
            foreach (var cosmetic in OwnedCosmetics)
                if (cosmetic.sku == sku) return true;
            return false;
        }

        private IEnumerator RefreshWallet()
        {
            yield return Send("GET", "/api/v1/wallet", null, true, null, (request, body) =>
            {
                if (request.responseCode >= 200 && request.responseCode < 300)
                    AcceptWallet(JsonUtility.FromJson<WalletSnapshot>(body));
            });
        }

        private IEnumerator ConnectRoutine()
        {
            _connecting = true;
            Status = "Connecting";
            _token = ReadSessionToken();

            if (!string.IsNullOrEmpty(_token))
            {
                var result = default(WalletSnapshot);
                var unauthorized = false;
                yield return Send("GET", "/api/v1/wallet", null, true, null, (request, body) =>
                {
                    if (request.responseCode >= 200 && request.responseCode < 300)
                        result = JsonUtility.FromJson<WalletSnapshot>(body);
                    else unauthorized = request.responseCode == 401;
                });

                if (result != null)
                {
                    AcceptWallet(result);
                    yield return RefreshCosmetics();
                    _connecting = false;
                    yield break;
                }
                if (!unauthorized)
                {
                    IsConnected = false;
                    Status = "Offline play available";
                    _connecting = false;
                    yield break;
                }

                ClearSessionToken();
                _token = "";
            }

            GuestSession guest = null;
            var sessionFailed = false;
            yield return Send("POST", "/api/v1/sessions/guest", "{}", false, null, (request, body) =>
            {
                if (request.responseCode >= 200 && request.responseCode < 300)
                    guest = JsonUtility.FromJson<GuestSession>(body);
                else sessionFailed = true;
            });

            if (guest == null || string.IsNullOrWhiteSpace(guest.accessToken))
            {
                IsConnected = false;
                Status = sessionFailed ? "Server unavailable · offline play" : "Offline play available";
                _connecting = false;
                yield break;
            }

            _token = guest.accessToken;
            try
            {
                WriteSessionToken(_token);
            }
            catch (IOException exception)
            {
                Debug.LogWarning("Harborlight guest session could not be persisted: " + exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                Debug.LogWarning("Harborlight guest session could not be persisted: " + exception.Message);
            }
            yield return Send("GET", "/api/v1/wallet", null, true, null, (request, body) =>
            {
                if (request.responseCode >= 200 && request.responseCode < 300)
                    AcceptWallet(JsonUtility.FromJson<WalletSnapshot>(body));
            });
            if (IsConnected) yield return RefreshCosmetics();
            _connecting = false;
        }

        private IEnumerator RefreshCosmetics()
        {
            var catalogJson = "";
            yield return Send("GET", "/api/v1/cosmetics", null, true, null, (request, body) =>
            {
                if (request.responseCode >= 200 && request.responseCode < 300) catalogJson = body;
            });
            if (!string.IsNullOrEmpty(catalogJson)) Catalog = ParseCosmetics(catalogJson);

            var ownedJson = "";
            yield return Send("GET", "/api/v1/me/cosmetics", null, true, null, (request, body) =>
            {
                if (request.responseCode >= 200 && request.responseCode < 300) ownedJson = body;
            });
            if (!string.IsNullOrEmpty(ownedJson)) OwnedCosmetics = ParseCosmetics(ownedJson);
        }

        private IEnumerator Send(string method, string route, string payload, bool authenticated, string idempotencyKey,
            Action<UnityWebRequest, string> completed)
        {
            using (var request = new UnityWebRequest(_baseUrl + route, method))
            {
                request.timeout = 12;
                request.downloadHandler = new DownloadHandlerBuffer();
                if (payload != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
                    request.SetRequestHeader("Content-Type", "application/json");
                }
                if (authenticated && !string.IsNullOrEmpty(_token)) request.SetRequestHeader("Authorization", "Bearer " + _token);
                if (!string.IsNullOrEmpty(idempotencyKey)) request.SetRequestHeader("Idempotency-Key", idempotencyKey);
                yield return request.SendWebRequest();
                completed?.Invoke(request, request.downloadHandler != null ? request.downloadHandler.text : "");
            }
        }

        private void AcceptWallet(WalletSnapshot wallet)
        {
            if (wallet == null) return;
            Gems = wallet.gems;
            IsConnected = true;
            Status = "Wallet connected";
        }

        private string ReadSessionToken()
        {
            var path = SessionFilePath;
            try
            {
                return File.Exists(path) ? File.ReadAllText(path).Trim() : "";
            }
            catch (IOException exception)
            {
                Debug.LogWarning("Harborlight guest session could not be read: " + exception.Message);
                return "";
            }
            catch (UnauthorizedAccessException exception)
            {
                Debug.LogWarning("Harborlight guest session could not be read: " + exception.Message);
                return "";
            }
        }

        private void WriteSessionToken(string token)
        {
            Directory.CreateDirectory(HarborlightLocalProfileStorage.PersistentDataPath);
            var path = SessionFilePath;
            var temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, token);
            if (File.Exists(path)) File.Replace(temporaryPath, path, null);
            else File.Move(temporaryPath, path);
        }

        private void ClearSessionToken()
        {
            var path = SessionFilePath;
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (IOException exception)
            {
                Debug.LogWarning("Harborlight guest session could not be cleared: " + exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                Debug.LogWarning("Harborlight guest session could not be cleared: " + exception.Message);
            }
        }

        private static string SessionFilePath
            => Path.Combine(HarborlightLocalProfileStorage.PersistentDataPath, SessionFileName);

        private static Cosmetic[] ParseCosmetics(string json)
        {
            var parsed = JsonUtility.FromJson<CosmeticList>("{\"items\":" + json + "}");
            return parsed != null && parsed.items != null ? parsed.items : new Cosmetic[0];
        }

        private static string ErrorMessage(UnityWebRequest request, string body)
        {
            if (request.result == UnityWebRequest.Result.ConnectionError) return "No network connection.";
            return string.IsNullOrWhiteSpace(body) ? "Server request failed (" + request.responseCode + ")." : body;
        }

    }

    internal static class HarborlightLocalProfileStorage
    {
        private const string TestProfileEnvironmentVariable = "HARBORLIGHT_TEST_PROFILE";

        public static string PersistentDataPath
        {
            get
            {
                var profile = TestProfileName;
                return profile == null
                    ? Application.persistentDataPath
                    : Path.Combine(Path.GetTempPath(), "Harborlight-TestProfiles", profile);
            }
        }

        public static string PlayerPrefsKey(string key)
        {
            var profile = TestProfileName;
            return profile == null ? key : key + "." + profile;
        }

        private static string TestProfileName
        {
            get
            {
                if (!Debug.isDebugBuild) return null;
                var profile = Environment.GetEnvironmentVariable(TestProfileEnvironmentVariable);
                if (string.IsNullOrWhiteSpace(profile)) return null;
                foreach (var character in profile)
                {
                    if ((character < 'a' || character > 'z') && (character < 'A' || character > 'Z') &&
                        (character < '0' || character > '9') && character != '-' && character != '_')
                        throw new InvalidOperationException(TestProfileEnvironmentVariable + " may contain only letters, digits, hyphens, and underscores.");
                }
                return profile;
            }
        }
    }
}
