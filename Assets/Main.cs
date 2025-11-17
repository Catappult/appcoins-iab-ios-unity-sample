using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

// ================================================================================================
// AppCoins SDK Unity Plugin Demo Application
// ================================================================================================
// This class demonstrates best practices for integrating the AppCoins SDK Unity Plugin into your Unity game.
//
// Key Features Demonstrated:
// - Singleton pattern for persistent purchase management across scenes
// - Checking SDK availability before making purchases
// - Querying products from Aptoide Connect
// - Handling direct purchases (user-initiated via Buy button)
// - Handling indirect purchases (initiated from Aptoide Store or web links via PurchaseIntent)
// - Server-side purchase verification for fraud prevention
// - Consuming purchases and granting items to users
// - Managing unfinished purchases on app startup
// ================================================================================================

public class Main : MonoBehaviour
{
    public static Main Instance { get; private set; }  // Singleton Instance

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

    // Cached products from Catappult
    Product[] products;

    // ============================================================================================
    // INITIALIZATION
    // ============================================================================================

    // IMPORTANT: This setup ensures purchase intents are handled throughout the app lifecycle
    // The singleton pattern keeps this object alive across scene changes
    private void Awake() {
        // Singleton enforcement - ensures only one instance exists across scenes
        if (Instance != null && Instance != this) {
            Destroy(gameObject);  // Destroy duplicate instances
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist across scenes

        // Subscribe to purchase intent updates for handling indirect purchases
        // Indirect purchases are initiated from:
        // - The Aptoide Store product catalog
        // - Web links that deep-link into purchases
        AppCoinsPurchaseManager.OnPurchaseUpdated += HandlePurchaseIntent;
    }

    // Initialize the SDK and setup the application on startup
    async void Start() {
        // STEP 1: Check if AppCoins SDK is available
        // The SDK is only available when:
        // - iOS version is 17.4 or higher
        // - App is distributed through Aptoide App Store
        var sdkAvailable = await AppCoinsSDK.Instance.IsAvailable();
        Debug.Log("AppCoins SDK isAvailable: " + sdkAvailable);

        if (sdkAvailable) {
            // STEP 2: Get testing wallet address for sandbox environment
            // Add this address to the Sandbox menu in the Developer Console to test purchases
            var walletAddressResult = await AppCoinsSDK.Instance.GetTestingWalletAddress();
            if (walletAddressResult.IsSuccess) {
                Debug.Log("Testing Wallet Address: " + walletAddressResult.Value);
            }

            // STEP 3: Query all available products from Aptoide Connect
            var productsResult = await AppCoinsSDK.Instance.GetProducts();
            if (productsResult.IsSuccess) {
                products = productsResult.Value;

                Debug.Log("––––––––––––––––––––––");
                Debug.Log("ALL PRODUCTS");

                foreach (var product in products) {
                    string productJson = JsonUtility.ToJson(product, true);
                    Debug.Log(productJson);
                }
            } else {
                Debug.Log("Failed to get products: " + productsResult.Error);
            }

            // ALTERNATIVE: Query only specific products by SKU
            // Uncomment this section to use this approach instead
            //
            // var selectedProductsResult = await AppCoinsSDK.Instance.GetProducts(new string[] { "antifreeze", "gas" });
            // if (selectedProductsResult.IsSuccess) {
            //     var selectedProducts = selectedProductsResult.Value;

            //     Debug.Log("––––––––––––––––––––––");
            //     Debug.Log("SELECTED PRODUCTS");

            //     foreach (var product in selectedProducts) {
            //         string productJson = JsonUtility.ToJson(product, true);
            //         Debug.Log(productJson);
            //     }
            // }

            // STEP 4: Consume any unfinished purchases from previous sessions
            // CRITICAL: Always call this on app startup to ensure users receive their items
            // This handles cases where:
            // - App crashed after purchase but before consumption
            // - User closed app during purchase flow
            // - Purchase completed in background while app was closed
            ConsumeUnfinishedPurchases();
        }
    }

    // ============================================================================================
    // GAME LOGIC
    // ============================================================================================

    // Example game action that consumes the user's gas
    // This is normal game logic, independent of the SDK
    public void Drive() {
        if (gas > 0) {
            gas -= 1;
        }
        Debug.Log("New gas value: " + gas);

        SetGasLevel();
    }

    // Grants gas to the user and updates the UI
    // This is called after a purchase is verified and consumed
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
    // DIRECT PURCHASE FLOW (User-Initiated)
    // ============================================================================================

    // Initiates a direct purchase when the user clicks a "Buy" button in the game
    // This is the standard purchase flow for in-app purchases
    async public void BuyGas() {
        // Purchase the "antifreeze" product with optional payload
        // The payload can be used to associate purchase with user ID or transaction metadata
        var purchaseResult = await AppCoinsSDK.Instance.Purchase("antifreeze");

        // Process the purchase using the unified handler
        HandlePurchase(purchaseResult);
    }

    // ============================================================================================
    // INDIRECT PURCHASE FLOW (Store/Web-Initiated)
    // ============================================================================================

    // Handles purchase intents from external sources (Aptoide Store, web links)
    // This is called automatically when AppCoinsPurchaseManager receives a purchase intent
    //
    // Common scenarios:
    // - User browses products in Aptoide Store and purchases directly from catalog
    // - User clicks a promotional web link that triggers a purchase
    //
    // NOTE: The authentication check below is just ONE example of how to handle purchase intents.
    // You can implement your own logic based on your game's needs:
    // - Always confirm immediately (no authentication required)
    // - Check inventory limits before confirming
    // - Validate user state or permissions
    // - Show a confirmation dialog to the user
    // - etc.
    private async void HandlePurchaseIntent(PurchaseIntent purchaseIntent) {
        if (isSignedIn) {
            // Example use case: User is authenticated - confirm the purchase intent
            Debug.Log($"Confirming purchase intent for: {purchaseIntent.Product.Title}");
            var purchaseResult = await AppCoinsSDK.Instance.ConfirmPurchaseIntent();
            HandlePurchase(purchaseResult);
        } else {
            // Example use case: User is not authenticated - ignore the purchase intent
            // You could also prompt the user to sign in first, then confirm the intent
            Debug.Log("Ignored Purchase Intent because the user is signed out.");
        }
    }

    // Manually check for pending purchase intents
    //
    // Use this when:
    // - User signs in (to process any pending intents from when they were signed out)
    // - App becomes active (to catch intents that arrived while app was in background)
    // - You want to proactively check for pending purchases
    async public void GetPurchaseIntent()
    {
        var intentResult = await AppCoinsSDK.Instance.GetPurchaseIntent();

        if (intentResult.IsSuccess && intentResult.Value != null) {
            var intent = intentResult.Value;
            string intentJson = JsonUtility.ToJson(intent, true);

            Debug.Log("––––––––––––––––––––––");
            Debug.Log("PURCHASE INTENT");
            Debug.Log(intentJson);

            HandlePurchaseIntent(intent);
        }
    }

    // ============================================================================================
    // UNIFIED PURCHASE HANDLER
    // ============================================================================================

    // Handles purchase results from both direct purchases and confirmed purchase intents
    // This method demonstrates best practices for handling all possible purchase states
    //
    // Purchase States:
    // - SUCCESS with VERIFIED: Purchase signature verification passed - safe to grant item
    // - SUCCESS with UNVERIFIED: Purchase signature verification failed - decide based on your business logic
    // - PENDING: Purchase is still in progress (rare)
    // - USER_CANCELLED: User cancelled the purchase
    // - FAILED: Purchase failed with an error
    //
    // BEST PRACTICE: Always verify purchases on your server before granting valuable items
    private async void HandlePurchase(AppCoinsSDKPurchaseResult purchaseResult) {
        switch (purchaseResult.State) {
            case AppCoinsSDK.PURCHASE_STATE_SUCCESS:
                // Purchase completed successfully - now check verification status
                switch (purchaseResult.Value.VerificationResult) {
                    case AppCoinsSDK.PURCHASE_VERIFICATION_STATE_VERIFIED:
                        // Local signature verification passed
                        // Extract verification data for server-side validation
                        string packageName = purchaseResult.Value.Purchase.Verification.Data.PackageName;
                        string productId = purchaseResult.Value.Purchase.Verification.Data.ProductId;
                        string purchaseToken = purchaseResult.Value.Purchase.Verification.Data.PurchaseToken;

                        // BEST PRACTICE: Verify on your server to prevent fraud
                        // This is critical for:
                        // - High-value items or currency
                        // - Preventing fraudulent purchases
                        // - Ensuring purchase data integrity
                        if (await VerifyPurchaseOnServer(packageName, productId, purchaseToken)) {
                            // Server verification passed - safe to consume and grant item
                            var consumeResult = await AppCoinsSDK.Instance.ConsumePurchase(purchaseResult.Value.Purchase.Sku);

                            if (consumeResult.IsSuccess) {
                                Debug.Log("Purchase consumed successfully");
                                // Grant the purchased item to the user
                                AddGas();
                            } else {
                                Debug.Log("Error consuming purchase: " + consumeResult.Error);
                            }
                        } else {
                            // Server verification failed - do NOT grant the item
                            Debug.Log("Failed to verify purchase on server.");
                        }
                        break;

                    case AppCoinsSDK.PURCHASE_VERIFICATION_STATE_UNVERIFIED:
                        // Local signature verification failed
                        //
                        // DECISION POINT: You must decide based on your business logic:
                        // Option 1: Still consume and grant the item (risky for high-value items)
                        // Option 2: Do NOT grant the item - purchase will be auto-refunded in 24 hours
                        //
                        // For this demo, we do not grant unverified purchases
                        Debug.Log("Purchase verification failed. Item not granted.");
                        break;
                }
                break;

            case AppCoinsSDK.PURCHASE_STATE_PENDING:
                // Purchase is still in progress (rare case)
                Debug.Log("Purchase is pending.");
                break;

            case AppCoinsSDK.PURCHASE_STATE_USER_CANCELLED:
                // User cancelled the purchase flow
                Debug.Log("Purchase was cancelled.");
                break;

            case AppCoinsSDK.PURCHASE_STATE_FAILED:
                // Purchase failed with an error
                // The error object contains details about what went wrong
                Debug.Log("Purchase failed with error: " + purchaseResult.Error);
                break;
        }
    }

    // ============================================================================================
    // SERVER-SIDE VERIFICATION
    // ============================================================================================

    // Verifies a purchase on your backend server to prevent fraud
    //
    // WHY THIS IS IMPORTANT:
    // - Prevents fraudulent purchases and chargebacks
    // - Ensures purchase data hasn't been tampered with
    // - Required for high-value items, currency, or consumables
    //
    // IMPLEMENTATION:
    // 1. Replace the URL with your own server endpoint
    // 2. Your server should call Aptoide Connect's validation API
    // 3. Return success only if Aptoide Connect confirms the purchase is valid
    async public Task<bool> VerifyPurchaseOnServer(string packageName, string productId, string purchaseToken) {
        // Replace this URL with your own server endpoint
        string url = $"https://api.ios.trivialdrive.aptoide.com/iap/validate?package_name={packageName}&product_id={productId}&token={purchaseToken}";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url)) {
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

    // ============================================================================================
    // PURCHASE QUERIES
    // ============================================================================================

    // Query all purchases made by the user in your application
    // Use case: Display purchase history, restore purchases, or sync with server
    async public void GetAllPurchases() {
        var purchasesResult = await AppCoinsSDK.Instance.GetAllPurchases();

        if (purchasesResult.IsSuccess) {
            var purchases = purchasesResult.Value;

            Debug.Log("––––––––––––––––––––––");
            Debug.Log("ALL PURCHASES");

            string purchasesJson = JsonUtility.ToJson(purchases, true);
            Debug.Log(purchasesJson);
        } else {
            Debug.Log("Failed to get purchases: " + purchasesResult.Error);
        }
    }

    // Query the latest purchase for a specific product SKU
    // Use case: Check if user has purchased a specific item before
    // Returns null if no purchase exists for that SKU
    async public void GetLatestPurchase(string sku) {
        var latestPurchaseResult = await AppCoinsSDK.Instance.GetLatestPurchase(sku);

        if (latestPurchaseResult.IsSuccess) {
            if (latestPurchaseResult.Value == null) {
                Debug.Log("No latest purchase found for SKU: " + sku);
                return;
            }

            var purchase = latestPurchaseResult.Value;
            string purchaseJson = JsonUtility.ToJson(purchase, true);

            Debug.Log("––––––––––––––––––––––");
            Debug.Log("LATEST PURCHASE");
            Debug.Log(purchaseJson);
        } else {
            Debug.Log("Latest purchase query failed with error: " + latestPurchaseResult.Error);
        }
    }

    // Query all unfinished purchases (not yet consumed or acknowledged)
    // Use case: Concluding purchases that were interrupted in previous user sessions
    async public void GetUnfinishedPurchases() {
        var unfinishedPurchasesResult = await AppCoinsSDK.Instance.GetUnfinishedPurchases();

        if (unfinishedPurchasesResult.IsSuccess) {
            var purchases = unfinishedPurchasesResult.Value;

            Debug.Log("––––––––––––––––––––––");
            Debug.Log("UNFINISHED PURCHASES");

            string purchasesJson = JsonUtility.ToJson(purchases, true);
            Debug.Log(purchasesJson);
        } else {
            Debug.Log("Failed to get unfinished purchases: " + unfinishedPurchasesResult.Error);
        }
    }

    // ============================================================================================
    // UNFINISHED PURCHASE RECOVERY
    // ============================================================================================

    // Consumes all unfinished purchases and grants items to the user
    //
    // CRITICAL: Always call this on app startup (in the Start method)
    //
    // WHY THIS IS ESSENTIAL:
    // - If app crashes after purchase but before consumption, user won't receive their item
    // - If user closes app during purchase flow, purchase might complete in background
    // - Ensures users always receive items they paid for, even after app restarts
    // - Prevents support tickets and refund requests
    async public void ConsumeUnfinishedPurchases() {
        var unfinishedPurchasesResult = await AppCoinsSDK.Instance.GetUnfinishedPurchases();

        if (unfinishedPurchasesResult.IsSuccess) {
            var purchases = unfinishedPurchasesResult.Value;

            foreach (var purchase in purchases) {
                // Consume each unfinished purchase
                var consumeResult = await AppCoinsSDK.Instance.ConsumePurchase(purchase.Sku);

                if (consumeResult.IsSuccess) {
                    Debug.Log("Unfinished Purchase consumed successfully");
                    // Grant the item to the user
                    AddGas();
                } else {
                    Debug.Log("Error consuming unfinished purchase: " + consumeResult.Error);
                }
            }
        } else {
            Debug.Log("Failed to get unfinished purchases: " + unfinishedPurchasesResult.Error);
        }
    }

    // ============================================================================================
    // AUTHENTICATION (Demo Only)
    // ============================================================================================
    // The following methods simulate user authentication for demonstration purposes.
    // This shows one possible use case for conditionally handling purchase intents,
    // but you can implement your own logic based on your game's requirements.

    // Toggles sign-in state (for demo purposes only)
    public void ToggleSignIn() {
        if (isSignedIn) {
            SignOut();
        } else {
            SignIn();
        }
    }

    // Simulates user sign-in and checks for pending purchase intents
    //
    // This demonstrates one approach: checking for pending purchase intents after sign-in
    // so that purchases initiated while signed out can be processed once authenticated
    public void SignIn() {
        isSignedIn = true;
        signInPortrait.sprite = signedIn;
        signInLandscape.sprite = signedIn;

        // Check for any pending purchase intents after sign in
        GetPurchaseIntent();
    }

    // Simulates user sign-out
    public void SignOut() {
        isSignedIn = false;
        signInPortrait.sprite = signedOut;
        signInLandscape.sprite = signedOut;
    }
}
