using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class Main : MonoBehaviour
{
    public int gas = 4;
    
    public Image gasLevel; 

    public Sprite level4;
    public Sprite level3;
    public Sprite level2;
    public Sprite level1;
    public Sprite level0;

    ProductData[] products;

    async void Start()
    {
        var sdkAvailable = await AppCoinsSDK.Instance.IsAvailable();

        if (sdkAvailable) 
        {   
            Debug.Log("AppCoins SDK isAvailable");
            Debug.Log("Adress: " + AppCoinsSDK.Instance.GetTestingWalletAddress());

            // products = await AppCoinsSDK.Instance.GetProducts();
            // OR 
            // var selectedProducts = await AppCoinsSDK.Instance.GetProducts(new string[] { "antifreeze", "gas" });
        }
    }


    public void Drive()
    {
        if (gas > 0) {
            gas -= 1;
        }
        Debug.Log("New gas value: " + gas);

        SetGasLevel();
    }

    async public void BuyGas()
    {   
        var purchaseResponse = await AppCoinsSDK.Instance.Purchase("antifreeze");

        if (purchaseResponse.State == AppCoinsSDK.PURCHASE_STATE_SUCCESS)
            {

            string packageName = purchaseResponse.Purchase.Verification.Data.PackageName;
            string productId = purchaseResponse.Purchase.Verification.Data.ProductId;
            string purchaseToken = purchaseResponse.Purchase.Verification.Data.PurchaseToken;

            bool isValid = await VerifyPurchaseOnServer(packageName, productId, purchaseToken);

            if (isValid)
            {
                var response = await AppCoinsSDK.Instance.ConsumePurchase(purchaseResponse.Purchase.Sku);

                if (response.Success)
                {
                    Debug.Log("Purchase consumed successfully");

                    if (gas < 4) {
                        gas += 1;
                    }
                    Debug.Log("New gas value: " + gas);

                    SetGasLevel();
                }
                else
                {
                    Debug.Log("Error consuming purchase: " + response.Error);
                }
            }
            else
            {
                Debug.LogError("Failed to verify purchase.");
            }            
        }
    }

    async public void GetAllPurchases()
    {
        var purchases = await AppCoinsSDK.Instance.GetAllPurchases();
        Debug.Log("––––––––––––––––––––––");
        Debug.Log("ALL PURCHASES");
        for (int i = 0; i < purchases.Length; i++)
        {
            Debug.Log("UID: " + purchases[i].UID);
            Debug.Log("Sku: " + purchases[i].Sku);
            Debug.Log("State: " + purchases[i].State);
            Debug.Log("OrderUID: " + purchases[i].OrderUID);
            Debug.Log("Payload: " + purchases[i].Payload);
            Debug.Log("Created: " + purchases[i].Created);
            Debug.Log("Verification Type: " + purchases[i].Verification.Type);
            Debug.Log("Verification Signature: " + purchases[i].Verification.Signature);
            Debug.Log("Verification Data OrderId: " + purchases[i].Verification.Data.OrderId);
            Debug.Log("Verification Data PackageName: " + purchases[i].Verification.Data.PackageName);
            Debug.Log("Verification Data ProductId: " + purchases[i].Verification.Data.ProductId);
            Debug.Log("Verification Data PurchaseTime: " + purchases[i].Verification.Data.PurchaseTime);
            Debug.Log("Verification Data PurchaseToken: " + purchases[i].Verification.Data.PurchaseToken);
            Debug.Log("Verification Data PurchaseState: " + purchases[i].Verification.Data.PurchaseState);
            Debug.Log("Verification Data DeveloperPayload: " + purchases[i].Verification.Data.DeveloperPayload);
        }
    }

    async public void GetLatestPurchase(string sku)
    {
        var purchase = await AppCoinsSDK.Instance.GetLatestPurchase(sku);
        Debug.Log("––––––––––––––––––––––");
        Debug.Log("LATEST PURCHASE");
        Debug.Log("UID: " + purchase.UID);
        Debug.Log("Sku: " + purchase.Sku);
        Debug.Log("State: " + purchase.State);
        Debug.Log("OrderUID: " + purchase.OrderUID);
        Debug.Log("Payload: " + purchase.Payload);
        Debug.Log("Created: " + purchase.Created);
        Debug.Log("Verification Type: " + purchase.Verification.Type);
        Debug.Log("Verification Signature: " + purchase.Verification.Signature);
        Debug.Log("Verification Data OrderId: " + purchase.Verification.Data.OrderId);
        Debug.Log("Verification Data PackageName: " + purchase.Verification.Data.PackageName);
        Debug.Log("Verification Data ProductId: " + purchase.Verification.Data.ProductId);
        Debug.Log("Verification Data PurchaseTime: " + purchase.Verification.Data.PurchaseTime);
        Debug.Log("Verification Data PurchaseToken: " + purchase.Verification.Data.PurchaseToken);
        Debug.Log("Verification Data PurchaseState: " + purchase.Verification.Data.PurchaseState);
        Debug.Log("Verification Data DeveloperPayload: " + purchase.Verification.Data.DeveloperPayload);
    }

    async public void GetUnfinishedPurchases()
    {
        var purchases = await AppCoinsSDK.Instance.GetUnfinishedPurchases();
        Debug.Log("––––––––––––––––––––––");
        Debug.Log("ALL PURCHASES");
        for (int i = 0; i < purchases.Length; i++)
        {
            Debug.Log("UID: " + purchases[i].UID);
            Debug.Log("Sku: " + purchases[i].Sku);
            Debug.Log("State: " + purchases[i].State);
            Debug.Log("OrderUID: " + purchases[i].OrderUID);
            Debug.Log("Payload: " + purchases[i].Payload);
            Debug.Log("Created: " + purchases[i].Created);
            Debug.Log("Verification Type: " + purchases[i].Verification.Type);
            Debug.Log("Verification Signature: " + purchases[i].Verification.Signature);
            Debug.Log("Verification Data OrderId: " + purchases[i].Verification.Data.OrderId);
            Debug.Log("Verification Data PackageName: " + purchases[i].Verification.Data.PackageName);
            Debug.Log("Verification Data ProductId: " + purchases[i].Verification.Data.ProductId);
            Debug.Log("Verification Data PurchaseTime: " + purchases[i].Verification.Data.PurchaseTime);
            Debug.Log("Verification Data PurchaseToken: " + purchases[i].Verification.Data.PurchaseToken);
            Debug.Log("Verification Data PurchaseState: " + purchases[i].Verification.Data.PurchaseState);
            Debug.Log("Verification Data DeveloperPayload: " + purchases[i].Verification.Data.DeveloperPayload);
        }
    }

    async public Task<bool> VerifyPurchaseOnServer(string packageName, string productId, string purchaseToken)
    {
        string url = $"https://api.ios.trivialdrive.aptoide.com/iap/validate?package_name={packageName}&product_id={productId}&token={purchaseToken}";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            var operation = webRequest.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (webRequest.result == UnityWebRequest.Result.Success && webRequest.responseCode == 200)
            {
                Debug.Log($"Purchase verified by request: {url}");
                return true;
            }
            else
            {
                Debug.Log($"Failed to verify purchase: {webRequest.error}");
                return false;
            }
        }
    }

    private void SetGasLevel()
    {
        if (gas == 4) { gasLevel.sprite = level4; }
        if (gas == 3) { gasLevel.sprite = level3; }
        if (gas == 2) { gasLevel.sprite = level2; }
        if (gas == 1) { gasLevel.sprite = level1; }
        if (gas == 0) { gasLevel.sprite = level0; }
    }
}
