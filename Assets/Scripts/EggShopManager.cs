using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class EggShopManager : MonoBehaviour
{
    public ShopDatabase shopDatabase;
    public TMP_Text coinUI;
    public GameObject[] shopPanelsGO;
    public ShopTemplate[] shopPanels;
    public Button[] myPurchaseBtns;

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

        // Subscribe to coin changes
        StudyTimer.OnCoinsChanged += OnCoinsChanged;
    }

    void OnDestroy()
    {
        // Unsubscribe from coin changes
        StudyTimer.OnCoinsChanged -= OnCoinsChanged;
    }

    private void OnCoinsChanged(int newAmount)
    {
        UpdateCoinDisplay();
        CheckPurchaseable();
    }

    private int GetCoins()
    {
        return StudyTimer.TotalCoins;
    }

    private void UpdateCoinDisplay()
    {
        if (coinUI == null)
        {
            Debug.LogError("coinUI is not assigned in EggShopManager!");
            return;
        }
        
        int coins = StudyTimer.TotalCoins;
        coinUI.text = "Coins: " + coins.ToString();
        Debug.Log($"Updated coin display to: {coins}");
    }

    public void CheckPurchaseable()
    {
        if (shopDatabase == null || shopDatabase.shopItemsSO == null) return;

        int currentCoins = GetCoins();
        for (int i = 0; i < shopDatabase.shopItemsSO.Length; i++)
        {
            myPurchaseBtns[i].interactable = currentCoins >= shopDatabase.shopItemsSO[i].baseCost;
        }
    }

    public void PurchaseItem(int btnNo)
    {
        if (shopDatabase == null || shopDatabase.shopItemsSO == null) return;

        int itemCost = shopDatabase.shopItemsSO[btnNo].baseCost;

        if (StudyTimer.SpendCoins(itemCost))
        {
            string eggTitle = shopDatabase.shopItemsSO[btnNo].title;
            Debug.Log($"Purchased {eggTitle} for {itemCost} coins");

            // Update UI immediately after purchase
            UpdateCoinDisplay();
            CheckPurchaseable();

            string purchasedItems = PlayerPrefs.GetString("PurchasedItems", "");
            Dictionary<string, int> eggQuantities = ParsePurchasedItems(purchasedItems);

            if (eggQuantities.ContainsKey(eggTitle))
                eggQuantities[eggTitle]++;
            else
                eggQuantities[eggTitle] = 1;

            string updatedPurchasedItems = string.Join(",", eggQuantities.Select(kvp => $"{kvp.Key}:{kvp.Value}")) + ",";
            PlayerPrefs.SetString("PurchasedItems", updatedPurchasedItems);
            PlayerPrefs.Save();
            Debug.Log($"Updated PurchasedItems: {updatedPurchasedItems}");

            // Force UI update
            if (coinUI != null)
            {
                coinUI.text = "Coins: " + StudyTimer.TotalCoins.ToString();
            }
        }
        else
        {
            Debug.Log("Not enough coins to purchase this item");
        }
    }

    private Dictionary<string, int> ParsePurchasedItems(string purchasedItems)
    {
        Dictionary<string, int> eggQuantities = new Dictionary<string, int>();
        if (!string.IsNullOrEmpty(purchasedItems))
        {
            string[] items = purchasedItems.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
            foreach (string item in items)
            {
                string[] parts = item.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1], out int quantity))
                {
                    eggQuantities[parts[0]] = quantity;
                }
            }
        }
        return eggQuantities;
    }

    public void LoadPanels()
    {
        if (shopDatabase == null || shopDatabase.shopItemsSO == null) return;

        for (int i = 0; i < shopDatabase.shopItemsSO.Length; i++)
        {
            shopPanels[i].titleTXT.text = shopDatabase.shopItemsSO[i].title;
            shopPanels[i].descriptionTXT.text = shopDatabase.shopItemsSO[i].description;
            shopPanels[i].costTXT.text = "Coins: " + shopDatabase.shopItemsSO[i].baseCost.ToString();

            // Display possible animals
            string animalsList = "\nPossible Animals:\n";
            foreach (var spawnChance in shopDatabase.shopItemsSO[i].animalSpawnChances)
            {
                if (spawnChance.animalPrefab != null)
                {
                    animalsList += $"{spawnChance.animalPrefab.name}: {spawnChance.spawnChance}%\n";
                }
            }
            shopPanels[i].descriptionTXT.text += animalsList;

            // Set sprite and color
            if (shopPanels[i].itemImage != null && shopDatabase.shopItemsSO[i].itemPrefab != null)
            {
                SetImageAndColorFromPrefab(shopDatabase.shopItemsSO[i].itemPrefab, shopPanels[i].itemImage);
            }

            TMP_Text buttonText = myPurchaseBtns[i].GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = "Buy: " + shopDatabase.shopItemsSO[i].baseCost.ToString();
            }
        }
    }

    private void SetImageAndColorFromPrefab(GameObject prefab, Image targetImage)
    {
        Image prefabImage = prefab.GetComponent<Image>();
        if (prefabImage != null && prefabImage.sprite != null)
        {
            targetImage.sprite = prefabImage.sprite;
            targetImage.color = prefabImage.color;
            return;
        }

        SpriteRenderer spriteRenderer = prefab.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            targetImage.sprite = spriteRenderer.sprite;
            targetImage.color = spriteRenderer.color;
            return;
        }

        Image childImage = prefab.GetComponentInChildren<Image>();
        if (childImage != null && childImage.sprite != null)
        {
            targetImage.sprite = childImage.sprite;
            targetImage.color = childImage.color;
            return;
        }

        SpriteRenderer childSprite = prefab.GetComponentInChildren<SpriteRenderer>();
        if (childSprite != null && childSprite.sprite != null)
        {
            targetImage.sprite = childSprite.sprite;
            targetImage.color = childSprite.color;
            return;
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

    public void AddTestCoins(int amount)
    {
        StudyTimer.TotalCoins += amount;
        UpdateCoinDisplay();
        CheckPurchaseable();
    }
}
