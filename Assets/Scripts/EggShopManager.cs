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
    }

    private int GetCoins()
    {
        return SimpleTimer.TotalCoins;
    }

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
            if (currentCoins >= shopDatabase.shopItemsSO[i].baseCost)
                myPurchaseBtns[i].interactable = true;
            else
                myPurchaseBtns[i].interactable = false;
        }
    }

    public void PurchaseItem(int btnNo)
    {
        if (shopDatabase == null || shopDatabase.shopItemsSO == null) return;

        int itemCost = shopDatabase.shopItemsSO[btnNo].baseCost;
        
        if (SimpleTimer.SpendCoins(itemCost))
        {
            string eggTitle = shopDatabase.shopItemsSO[btnNo].title;
            Debug.Log($"Purchased {eggTitle} for {itemCost} coins");
            
            UpdateCoinDisplay();
            CheckPurchaseable();
            
            string purchasedItems = PlayerPrefs.GetString("PurchasedItems", "");
            Dictionary<string, int> eggQuantities = ParsePurchasedItems(purchasedItems);
            
            if (eggQuantities.ContainsKey(eggTitle))
            {
                eggQuantities[eggTitle]++;
            }
            else
            {
                eggQuantities[eggTitle] = 1;
            }

            string updatedPurchasedItems = string.Join(",", eggQuantities.Select(kvp => $"{kvp.Key}:{kvp.Value}")) + ",";
            PlayerPrefs.SetString("PurchasedItems", updatedPurchasedItems);
            PlayerPrefs.Save();
            Debug.Log($"Updated PurchasedItems: {updatedPurchasedItems}");
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

            if (shopPanels[i].itemImage != null && shopDatabase.shopItemsSO[i].itemPrefab != null)
            {
                Image prefabImage = shopDatabase.shopItemsSO[i].itemPrefab.GetComponent<Image>();
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
    
    public void AddTestCoins(int amount) 
    {
        SimpleTimer.TotalCoins += amount;
        UpdateCoinDisplay();
        CheckPurchaseable();
    }
}