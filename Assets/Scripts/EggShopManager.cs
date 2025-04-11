using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EggS : MonoBehaviour
{
    public TMP_Text titleTXT;
    public TMP_Text descriptionTXT;
    public TMP_Text costTXT;
    public Image itemImage;  // Added for displaying egg images
}

public class EggShopManager : MonoBehaviour
{
    public TMP_Text coinUI;
    public ShopItemSO[] shopItemsSO;
    public GameObject[] shopPanelsGO;
    public ShopTemplate[] shopPanels;
    public Button[] myPurchaseBtns;

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < shopItemsSO.Length; i++)
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
        int currentCoins = GetCoins();
        for (int i = 0; i < shopItemsSO.Length; i++)
        {
            if (currentCoins >= shopItemsSO[i].baseCost) //if i have enough money.
                myPurchaseBtns[i].interactable = true;
            else
                myPurchaseBtns[i].interactable = false;
        }
    }

    public void PurchaseItem(int btnNo)
    {
        int itemCost = shopItemsSO[btnNo].baseCost;
        
        // Use the SimpleTimer's static method to spend coins
        if (SimpleTimer.SpendCoins(itemCost))
        {
            Debug.Log($"Purchased {shopItemsSO[btnNo].title} for {itemCost} coins");
            
            // Update display
            UpdateCoinDisplay();
            CheckPurchaseable();
            
            // Track purchase in PlayerPrefs if needed
            string purchasedItems = PlayerPrefs.GetString("PurchasedItems", "");
            purchasedItems += shopItemsSO[btnNo].title + ",";
            PlayerPrefs.SetString("PurchasedItems", purchasedItems);
            PlayerPrefs.Save();
            
            // Additional purchase logic can go here
            // For example, unlocking items, changing UI, etc.
        }
        else
        {
            Debug.Log("Not enough coins to purchase this item");
        }
    }

    public void LoadPanels()
    {
        for (int i = 0; i < shopItemsSO.Length; i++)
        {
            shopPanels[i].titleTXT.text = shopItemsSO[i].title;
            shopPanels[i].descriptionTXT.text = shopItemsSO[i].description;
            shopPanels[i].costTXT.text = "Coins: " + shopItemsSO[i].baseCost.ToString();
            
            // Get image from the item prefab
            if (shopPanels[i].itemImage != null && shopItemsSO[i].itemPrefab != null)
            {
                // Try to get image component directly from the prefab
                Image prefabImage = shopItemsSO[i].itemPrefab.GetComponent<Image>();
                
                // If no Image component, try to get SpriteRenderer
                if (prefabImage != null && prefabImage.sprite != null)
                {
                    shopPanels[i].itemImage.sprite = prefabImage.sprite;
                }
                else
                {
                    SpriteRenderer spriteRenderer = shopItemsSO[i].itemPrefab.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null && spriteRenderer.sprite != null)
                    {
                        shopPanels[i].itemImage.sprite = spriteRenderer.sprite;
                    }
                    else
                    {
                        // If not found on the main object, try to find in children
                        Image childImage = shopItemsSO[i].itemPrefab.GetComponentInChildren<Image>();
                        if (childImage != null && childImage.sprite != null)
                        {
                            shopPanels[i].itemImage.sprite = childImage.sprite;
                        }
                        else
                        {
                            SpriteRenderer childSprite = shopItemsSO[i].itemPrefab.GetComponentInChildren<SpriteRenderer>();
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
                buttonText.text = "Buy: " + shopItemsSO[i].baseCost.ToString();
            }
        }
    }

    public void CheckPanel()
{
    for (int i = 0; i < shopItemsSO.Length; i++)
    {
        Debug.Log($"Loading panel {i}: Item = {shopItemsSO[i].title}, Cost = {shopItemsSO[i].baseCost}");
        // rest of your code...
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