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

            products = await AppCoinsSDK.Instance.GetProducts();
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
            string purchaseJson = JsonUtility.ToJson(purchases[i], true); // Serialize with pretty print
            Debug.Log(purchaseJson);
        }
    }

    async public void GetLatestPurchase(string sku)
    {
        var purchase = await AppCoinsSDK.Instance.GetLatestPurchase(sku);
        string purchaseJson = JsonUtility.ToJson(purchase, true); // Serialize with pretty print

        Debug.Log("––––––––––––––––––––––");
        Debug.Log("LATEST PURCHASE");
        Debug.Log(purchaseJson);
    }

    async public void GetUnfinishedPurchases()
    {
        var purchases = await AppCoinsSDK.Instance.GetUnfinishedPurchases();
        Debug.Log("––––––––––––––––––––––");
        Debug.Log("UNFINISHED PURCHASES");
        for (int i = 0; i < purchases.Length; i++)
        {
            string purchaseJson = JsonUtility.ToJson(purchases[i], true); // Serialize with pretty print
            Debug.Log(purchaseJson);
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
