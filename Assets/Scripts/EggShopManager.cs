using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EggShopManager : MonoBehaviour
{
    public ShopDatabase shopDatabase; // Reference to centralized shop data
    public TMP_Text coinUI;
    public GameObject[] shopPanelsGO;
    public ShopTemplate[] shopPanels;
    public Button[] myPurchaseBtns;

    // Start is called before the first frame update
    void Start()
    {
        if (shopDatabase == null || shopDatabase.shopItemsSO == null)
        {
            Debug.LogError("ShopDatabase or shopItemsSO not assigned in EggShopManager");
            return;
        }

        for (int i = 0; i < shopDatabase.shopItemsSO.Length; i++)
        {
            shopPanelsGO[i].SetActive(true);
        }
        
        UpdateCoinDisplay();
        LoadPanels();
        CheckPurchaseable();
        CheckPanel();
    }

    // Get coins from the centralized system
    private int GetCoins()
    {
        return SimpleTimer.TotalCoins;
    }

    // Update the UI to show current coins
    private void UpdateCoinDisplay()
    {
        coinUI.text = "Coins: " + GetCoins().ToString();
    }

    public void CheckPurchaseable()
    {
        if (shopDatabase == null || shopDatabase.shopItemsSO == null) return;

        int currentCoins = GetCoins();
        for (int i = 0; i < shopDatabase.shopItemsSO.Length; i++)
        {
            if (currentCoins >= shopDatabase.shopItemsSO[i].baseCost) // If I have enough money
                myPurchaseBtns[i].interactable = true;
            else
                myPurchaseBtns[i].interactable = false;
        }
    }

    public void PurchaseItem(int btnNo)
    {
        if (shopDatabase == null || shopDatabase.shopItemsSO == null) return;

        int itemCost = shopDatabase.shopItemsSO[btnNo].baseCost;
        
        // Use the SimpleTimer's static method to spend coins
        if (SimpleTimer.SpendCoins(itemCost))
        {
            Debug.Log($"Purchased {shopDatabase.shopItemsSO[btnNo].title} for {itemCost} coins");
            
            // Update display
            UpdateCoinDisplay();
            CheckPurchaseable();
            
            // Track purchase in PlayerPrefs, avoid duplicates
            string purchasedItems = PlayerPrefs.GetString("PurchasedItems", "");
            string eggTitle = shopDatabase.shopItemsSO[btnNo].title;
            if (!purchasedItems.Contains(eggTitle + ","))
            {
                purchasedItems += eggTitle + ",";
                PlayerPrefs.SetString("PurchasedItems", purchasedItems);
                PlayerPrefs.Save();
            }
            else
            {
                Debug.Log($"Egg {eggTitle} already purchased");
            }
        }
        else
        {
            Debug.Log("Not enough coins to purchase this item");
        }
    }

    public void LoadPanels()
    {
        if (shopDatabase == null || shopDatabase.shopItemsSO == null) return;

        for (int i = 0; i < shopDatabase.shopItemsSO.Length; i++)
        {
            shopPanels[i].titleTXT.text = shopDatabase.shopItemsSO[i].title;
            shopPanels[i].descriptionTXT.text = shopDatabase.shopItemsSO[i].description;
            shopPanels[i].costTXT.text = "Coins: " + shopDatabase.shopItemsSO[i].baseCost.ToString();
            
            // Get image from the item prefab
            if (shopPanels[i].itemImage != null && shopDatabase.shopItemsSO[i].itemPrefab != null)
            {
                // Try to get image component directly from the prefab
                Image prefabImage = shopDatabase.shopItemsSO[i].itemPrefab.GetComponent<Image>();
                
                // If no Image component, try to get SpriteRenderer
                if (prefabImage != null && prefabImage.sprite != null)
                {
                    shopPanels[i].itemImage.sprite = prefabImage.sprite;
                }
                else
                {
                    SpriteRenderer spriteRenderer = shopDatabase.shopItemsSO[i].itemPrefab.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null && spriteRenderer.sprite != null)
                    {
                        shopPanels[i].itemImage.sprite = spriteRenderer.sprite;
                    }
                    else
                    {
                        // If not found on the main object, try to find in children
                        Image childImage = shopDatabase.shopItemsSO[i].itemPrefab.GetComponentInChildren<Image>();
                        if (childImage != null && childImage.sprite != null)
                        {
                            shopPanels[i].itemImage.sprite = childImage.sprite;
                        }
                        else
                        {
                            SpriteRenderer childSprite = shopDatabase.shopItemsSO[i].itemPrefab.GetComponentInChildren<SpriteRenderer>();
                            if (childSprite != null && childSprite.sprite != null)
                            {
                                shopPanels[i].itemImage.sprite = childSprite.sprite;
                            }
                        }
                    }
                }
            }

            // Add cost to the purchase button's text
            TMP_Text buttonText = myPurchaseBtns[i].GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = "Buy: " + shopDatabase.shopItemsSO[i].baseCost.ToString();
            }
        }
    }

    public void CheckPanel()
    {
        if (shopDatabase == null || shopDatabase.shopItemsSO == null) return;

        for (int i = 0; i < shopDatabase.shopItemsSO.Length; i++)
        {
            Debug.Log($"Loading panel {i}: Item = {shopDatabase.shopItemsSO[i].title}, Cost = {shopDatabase.shopItemsSO[i].baseCost}");
        }
    }
    
    // For testing purposes - you can remove this in production
    public void AddTestCoins(int amount) 
    {
        SimpleTimer.TotalCoins += amount;
        UpdateCoinDisplay();
        CheckPurchaseable();
    }
}