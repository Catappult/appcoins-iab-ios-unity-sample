using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class Main : MonoBehaviour
{
    public static Main Instance { get; private set; }  // Singleton Instance

    public int gas = 4;
    
    public Image gasLevelPortrait; 
    public Image gasLevelLandscape; 

    public Sprite level4;
    public Sprite level3;
    public Sprite level2;
    public Sprite level1;
    public Sprite level0;

    public bool isSignedIn = false; 
    
    public Image signInPortrait; 
    public Image signInLandscape; 

    public Sprite signedIn;
    public Sprite signedOut;

    Product[] products;

    private void Awake() {
        // Singleton enforcement
        if (Instance != null && Instance != this) {
            Destroy(gameObject);  // Destroy duplicate instances
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist across scenes

        AppCoinsPurchaseManager.OnPurchaseUpdated += HandlePurchaseIntent; // Subscribe to purchase updates
    }

    async void Start() {
        var sdkAvailable = await AppCoinsSDK.Instance.IsAvailable();
        Debug.Log("AppCoins SDK isAvailable: " + sdkAvailable);

        if (sdkAvailable) {
            Debug.Log("Address: " + AppCoinsSDK.Instance.GetTestingWalletAddress());

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

            // OR 

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

            ConsumeUnfinishedPurchases();
        }
    }

    public void Drive() {
        if (gas > 0) {
            gas -= 1;
        }
        Debug.Log("New gas value: " + gas);

        SetGasLevel();
    }

    async public void BuyGas() {   
        var purchaseResult = await AppCoinsSDK.Instance.Purchase("antifreeze");
        HandlePurchase(purchaseResult);
    }

    private async void HandlePurchaseIntent(PurchaseIntent purchaseIntent) {
        if (isSignedIn) {
            var purchaseResult = await AppCoinsSDK.Instance.ConfirmPurchaseIntent();
            HandlePurchase(purchaseResult);
        } else {
            Debug.Log("Ignored Purchase Intent because the user is signed out.");
        }
    }

    private async void HandlePurchase(AppCoinsSDKPurchaseResult purchaseResult) {
        switch (purchaseResult.State) {
            case AppCoinsSDK.PURCHASE_STATE_SUCCESS:
                switch (purchaseResult.Value.VerificationResult) {
                    case AppCoinsSDK.PURCHASE_VERIFICATION_STATE_VERIFIED:
                        // Consume the item and give it to the user
                        string packageName = purchaseResult.Value.Purchase.Verification.Data.PackageName;
                        string productId = purchaseResult.Value.Purchase.Verification.Data.ProductId;
                        string purchaseToken = purchaseResult.Value.Purchase.Verification.Data.PurchaseToken;

                        if (await VerifyPurchaseOnServer(packageName, productId, purchaseToken)) {
                            var consumeResult = await AppCoinsSDK.Instance.ConsumePurchase(purchaseResult.Value.Purchase.Sku);

                            if (consumeResult.IsSuccess) {
                                Debug.Log("Purchase consumed successfully");
                                AddGas();
                            } else {
                                Debug.Log("Error consuming purchase: " + consumeResult.Error);
                            }
                        } else {
                            Debug.Log("Failed to verify purchase on server.");
                        }   
                        break;  
                    case AppCoinsSDK.PURCHASE_VERIFICATION_STATE_UNVERIFIED:
                        // Handle unverified purchase according to your game logic
                        break;  
                }
                break;
            case AppCoinsSDK.PURCHASE_STATE_PENDING:
                // Handle pending purchase according to your game logic
                Debug.Log("Purchase is pending.");
                break;
            case AppCoinsSDK.PURCHASE_STATE_USER_CANCELLED:
                // Handle cancelled purchase according to your game logic
                Debug.Log("Purchase was cancelled.");
                break;
            case AppCoinsSDK.PURCHASE_STATE_FAILED:
                // Handle failed purchase according to your game logic
                Debug.Log("Purchase failed with error" + purchaseResult.Error);
                break;
        }
    }

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

    async public void GetAllPurchases() {
        var purchasesResult = await AppCoinsSDK.Instance.GetAllPurchases();

        if (purchasesResult.IsSuccess) {
            var purchases = purchasesResult.Value;

            Debug.Log("––––––––––––––––––––––");
            Debug.Log("ALL PURCHASES");

            string purchasesJson = JsonUtility.ToJson(purchases, true); 
            Debug.Log(purchasesJson);
        }
    }

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
            Debug.Log("Latest purchase query failed with error");
            Debug.Log(latestPurchaseResult.Error);
        }
    }

    async public void GetUnfinishedPurchases() {
        var unfinishedPurchasesResult = await AppCoinsSDK.Instance.GetUnfinishedPurchases();

        if (unfinishedPurchasesResult.IsSuccess) {
            var purchases = unfinishedPurchasesResult.Value;

            Debug.Log("––––––––––––––––––––––");
            Debug.Log("UNFINISHED PURCHASES");

            string purchasesJson = JsonUtility.ToJson(purchases, true); 
            Debug.Log(purchasesJson);
        }
    }

    async public void ConsumeUnfinishedPurchases() {
        var unfinishedPurchasesResult = await AppCoinsSDK.Instance.GetUnfinishedPurchases();

        if (unfinishedPurchasesResult.IsSuccess) {
            var purchases = unfinishedPurchasesResult.Value;

            foreach (var purchase in purchases) {
                var consumeResult = await AppCoinsSDK.Instance.ConsumePurchase(purchase.Sku);

                if (consumeResult.IsSuccess) {
                    Debug.Log("Unfinished Purchase consumed successfully");
                    AddGas();
                } else {
                    Debug.Log("Error consuming unfinished purchase: " + consumeResult.Error);
                }
            }
        }
    }

    async public Task<bool> VerifyPurchaseOnServer(string packageName, string productId, string purchaseToken) {
        string url = $"https://api.ios.trivialdrive.aptoide.com/iap/validate?package_name={packageName}&product_id={productId}&token={purchaseToken}";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url)) {
            var operation = webRequest.SendWebRequest();

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

    public void AddGas() {
        if (gas < 4) {
            gas += 1;
        }

        SetGasLevel();
    }

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

    public void ToggleSignIn() {
        if (isSignedIn) {
            SignOut();
        } else {
            SignIn();
        }
    }

    public void SignIn() {
        isSignedIn = true;
        signInPortrait.sprite = signedIn;
        signInLandscape.sprite = signedIn;
        GetPurchaseIntent();
    }

    public void SignOut() {
        isSignedIn = false;
        signInPortrait.sprite = signedOut;
        signInLandscape.sprite = signedOut;
    }
}