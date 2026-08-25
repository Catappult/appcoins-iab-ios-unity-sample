using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Purchasing;

// ================================================================================================
// StoreKit In-App Purchases via Unity IAP 5.x - Trivial Drive
// ================================================================================================
// Integrates Apple StoreKit in-app purchases using Unity IAP 5.x (com.unity.purchasing),
// which wraps StoreKit natively on iOS — no custom Swift bridge required.
//
// Unity IAP 5.x uses an event-driven StoreController API (UnityIAPServices), replacing the
// older 4.x IStoreListener / ConfigurationBuilder / ProcessPurchase model.
//
// Key Features Demonstrated:
// - Singleton pattern for persistent purchase management across scenes
// - Connecting the StoreController and fetching the product catalog
// - Handling user-initiated purchases (Buy button)
// - Server-side receipt validation for fraud prevention
// - Confirming (finishing) consumable purchases and granting items to users
// - Replaying unconfirmed purchases on startup so users always receive what they paid for
// ================================================================================================

public class Main : MonoBehaviour
{
    public static Main Instance { get; private set; }  // Singleton Instance

    // Product identifier configured in App Store Connect for the consumable "gas" product.
    private const string ProductAntifreeze = "antifreeze";

    // Game state
    public int gas = 4;

    // UI Elements
    public Image gasLevelPortrait;
    public Image gasLevelLandscape;

    public Sprite level4;
    public Sprite level3;
    public Sprite level2;
    public Sprite level1;
    public Sprite level0;

    // User authentication state (simulated for demo purposes)
    public bool isSignedIn = false;

    public Image signInPortrait;
    public Image signInLandscape;

    public Sprite signedIn;
    public Sprite signedOut;

    // Unity IAP 5.x store controller and the cached product once fetched.
    private StoreController storeController;
    private Product antifreezeProduct;

    // ============================================================================================
    // INITIALIZATION
    // ============================================================================================

    // The singleton pattern keeps this object alive across scene changes so the store controller
    // and its transaction callbacks remain valid throughout the app lifecycle.
    private void Awake() {
        // Singleton enforcement - ensures only one instance exists across scenes
        if (Instance != null && Instance != this) {
            Destroy(gameObject);  // Destroy duplicate instances
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist across scenes
    }

    // Create the StoreController, subscribe to its events, connect, and fetch products.
    async void Start() {
        if (storeController != null) {
            return; // Already initialized
        }

        storeController = UnityIAPServices.StoreController();

        storeController.OnProductsFetched += OnProductsFetched;
        storeController.OnPurchasesFetched += OnPurchasesFetched;
        storeController.OnPurchasePending += OnPurchasePending;
        storeController.OnPurchaseFailed += OnPurchaseFailed;
        storeController.OnStoreDisconnected += OnStoreDisconnected;

        try {
            // Connect to the underlying store (StoreKit on iOS).
            await storeController.Connect();
        } catch (Exception e) {
            Debug.LogError("Unity IAP failed to connect: " + e.Message);
            return;
        }

        // Register the products we sell. "antifreeze" is a consumable that grants +1 gas.
        storeController.FetchProducts(new List<ProductDefinition> {
            new ProductDefinition(ProductAntifreeze, ProductType.Consumable)
        });
    }

    // Called when the product catalog has been fetched from the store.
    private void OnProductsFetched(List<Product> products) {
        Debug.Log("––––––––––––––––––––––");
        Debug.Log("ALL PRODUCTS");
        foreach (var product in products) {
            Debug.Log($"{product.definition.id} | price: {product.metadata.localizedPriceString}");
            if (product.definition.id == ProductAntifreeze) {
                antifreezeProduct = product;
            }
        }

        // Ask the store for existing purchases. Any consumable that was purchased in a previous
        // session but never confirmed comes back as a pending order we can now grant + confirm.
        storeController.FetchPurchases();
    }

    // Called when existing purchases have been fetched. Handles startup recovery of interrupted
    // purchases (e.g. app closed after paying but before the item was granted).
    private void OnPurchasesFetched(Orders orders) {
        foreach (var pendingOrder in orders.PendingOrders) {
            Debug.Log("Recovering unconfirmed purchase from a previous session.");
            ProcessPendingOrder(pendingOrder);
        }
    }

    // ============================================================================================
    // GAME LOGIC
    // ============================================================================================

    // Example game action that consumes the user's gas
    // This is normal game logic, independent of the store
    public void Drive() {
        if (gas > 0) {
            gas -= 1;
        }
        Debug.Log("New gas value: " + gas);

        SetGasLevel();
    }

    // Grants gas to the user and updates the UI
    // This is called after a purchase is verified and confirmed
    public void AddGas() {
        if (gas < 4) {
            gas += 1;
        }

        SetGasLevel();
    }

    // Updates the gas level UI sprites
    private void SetGasLevel() {
        switch (gas) {
            case 4:
                gasLevelPortrait.sprite = level4;
                gasLevelLandscape.sprite = level4;
                break;
            case 3:
                gasLevelPortrait.sprite = level3;
                gasLevelLandscape.sprite = level3;
                break;
            case 2:
                gasLevelPortrait.sprite = level2;
                gasLevelLandscape.sprite = level2;
                break;
            case 1:
                gasLevelPortrait.sprite = level1;
                gasLevelLandscape.sprite = level1;
                break;
            case 0:
                gasLevelPortrait.sprite = level0;
                gasLevelLandscape.sprite = level0;
                break;
        }
    }

    // ============================================================================================
    // PURCHASE FLOW (User-Initiated)
    // ============================================================================================

    // Initiates a purchase when the user clicks the "Buy" button in the game.
    public void BuyGas() {
        if (storeController == null || antifreezeProduct == null) {
            Debug.LogWarning("Store not ready yet. Cannot start purchase.");
            return;
        }

        Debug.Log($"Purchasing product: {antifreezeProduct.definition.id}");
        storeController.PurchaseProduct(antifreezeProduct);
    }

    // ============================================================================================
    // PURCHASE HANDLERS
    // ============================================================================================

    // Called when a purchase reaches the pending state (payment authorized, awaiting fulfillment).
    // This fires both for purchases the user just made and for unconfirmed purchases replayed on
    // startup. We validate on our server before granting the item and confirming the order.
    private void OnPurchasePending(PendingOrder pendingOrder) {
        Debug.Log("Purchase pending; validating before fulfillment.");
        ProcessPendingOrder(pendingOrder);
    }

    // Called when a purchase fails or is cancelled by the user.
    private void OnPurchaseFailed(FailedOrder failedOrder) {
        Debug.Log($"Purchase failed: {failedOrder.FailureReason} - {failedOrder.Details}");
    }

    // Called if the connection to the store is lost.
    private void OnStoreDisconnected(StoreConnectionFailureDescription description) {
        Debug.LogWarning($"Store disconnected: {description.message}");
    }

    // Validates a pending order on the server and, on success, grants the item and confirms the
    // order. Confirming finishes the StoreKit transaction so the consumable can be bought again.
    // If validation fails we do NOT confirm; the order stays pending and is replayed next launch.
    private async void ProcessPendingOrder(PendingOrder pendingOrder) {
        string productId = GetProductId(pendingOrder);

        // BEST PRACTICE: Validate the Apple receipt on your server before granting valuable items.
        // pendingOrder.Info.Receipt is the JSON receipt whose payload your server forwards to Apple.
        bool valid = await VerifyPurchaseOnServer(productId, pendingOrder.Info);

        if (valid) {
            Debug.Log("Server validation passed. Granting item and confirming order.");
            if (productId == ProductAntifreeze) {
                AddGas();
            }
            // Finish the transaction with StoreKit so the consumable can be purchased again.
            storeController.ConfirmPurchase(pendingOrder);
        } else {
            // Leave the order pending; it will be replayed on the next launch for another attempt.
            Debug.Log("Server validation failed. Item not granted; order left pending.");
        }
    }

    // Extracts the first product id from an order's cart.
    private static string GetProductId(Order order) {
        foreach (var item in order.CartOrdered.Items()) {
            return item.Product.definition.id;
        }
        return null;
    }

    // ============================================================================================
    // SERVER-SIDE VERIFICATION
    // ============================================================================================

    // Verifies a purchase on your backend server to prevent fraud.
    //
    // WHY THIS IS IMPORTANT:
    // - Prevents fraudulent purchases and chargebacks
    // - Ensures purchase data hasn't been tampered with
    // - Required for high-value items, currency, or consumables
    //
    // IMPLEMENTATION:
    // 1. Replace the URL with your own server endpoint.
    // 2. Forward the Apple receipt payload to your server.
    // 3. Your server calls Apple's App Store Server API (or the legacy verifyReceipt endpoint)
    //    to confirm the receipt is genuine and matches the expected product.
    // 4. Return success only if Apple confirms the purchase is valid.
    async public Task<bool> VerifyPurchaseOnServer(string productId, IOrderInfo orderInfo) {
        string url = "https://api.ios.trivialdrive.aptoide.com/iap/apple/validate";

        var body = new PurchaseValidationRequest {
            productId = productId,
            transactionId = orderInfo.TransactionID,
            receipt = orderInfo.Receipt,
            bundleId = Application.identifier
        };
        byte[] bodyBytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(body));

        using (UnityWebRequest webRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)) {
            webRequest.uploadHandler = new UploadHandlerRaw(bodyBytes);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            var operation = webRequest.SendWebRequest();

            // Wait for the request to complete
            while (!operation.isDone)
                await Task.Yield();

            if (webRequest.result == UnityWebRequest.Result.Success && webRequest.responseCode == 200) {
                return true;
            } else {
                Debug.Log($"Failed to verify purchase: {webRequest.error}");
                return false;
            }
        }
    }

    // Payload sent to the validation endpoint.
    [Serializable]
    private class PurchaseValidationRequest {
        public string productId;
        public string transactionId;
        public string receipt;
        public string bundleId;
    }

    // ============================================================================================
    // RESTORE PURCHASES
    // ============================================================================================

    // Re-fetches purchases from the store. On iOS, Apple requires a user-triggered "Restore
    // Purchases" action for apps that sell non-consumables or subscriptions. Consumables like
    // "antifreeze" are not restored, but the entry point is provided for completeness.
    public void RestorePurchases() {
        if (storeController == null) {
            Debug.LogWarning("Store not ready yet. Cannot restore purchases.");
            return;
        }
        storeController.FetchPurchases();
    }

    // ============================================================================================
    // AUTHENTICATION (Demo Only)
    // ============================================================================================
    // The following methods simulate user authentication for demonstration purposes only.

    // Toggles sign-in state (for demo purposes only)
    public void ToggleSignIn() {
        if (isSignedIn) {
            SignOut();
        } else {
            SignIn();
        }
    }

    // Simulates user sign-in
    public void SignIn() {
        isSignedIn = true;
        signInPortrait.sprite = signedIn;
        signInLandscape.sprite = signedIn;
    }

    // Simulates user sign-out
    public void SignOut() {
        isSignedIn = false;
        signInPortrait.sprite = signedOut;
        signInLandscape.sprite = signedOut;
    }
}
