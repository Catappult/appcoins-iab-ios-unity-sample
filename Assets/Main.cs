using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
            var response = await AppCoinsSDK.Instance.ConsumePurchase(purchaseResponse.PurchaseSku);

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
