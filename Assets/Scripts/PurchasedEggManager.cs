using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class PurchasedEggManager : MonoBehaviour
{
    public ShopDatabase shopDatabase;
    public SimpleTimer simpleTimer;
    public GameObject eggMenuContent;
    public GameObject eggButtonPrefab;
    public ShopItemSO defaultEggSO;
    public TMP_Text totalEggsBoughtText;
    
    private string currentSelectedEgg;
    private Dictionary<string, ShopItemSO> availableEggs = new Dictionary<string, ShopItemSO>();
    private Dictionary<string, int> eggQuantities = new Dictionary<string, int>();
    
    void Start()
    {
        Debug.Log("PurchasedEggManager started");

        if (shopDatabase == null || shopDatabase.shopItemsSO == null)
        {
            Debug.LogError("ShopDatabase or shopItemsSO not assigned in PurchasedEggManager");
            return;
        }
        
        if (totalEggsBoughtText == null)
        {
            Debug.LogError("totalEggsBoughtText not assigned in PurchasedEggManager");
        }
        
        if (defaultEggSO != null && !eggQuantities.ContainsKey(defaultEggSO.title))
        {
            availableEggs.Add(defaultEggSO.title, defaultEggSO);
            eggQuantities[defaultEggSO.title] = 1;
        }
        
        LoadPurchasedEggs();
        PopulateEggMenu();
        
        if (!string.IsNullOrEmpty(PlayerPrefs.GetString("CurrentSelectedEgg", "")))
        {
            currentSelectedEgg = PlayerPrefs.GetString("CurrentSelectedEgg");
        }
        else if (availableEggs.Count > 0)
        {
            currentSelectedEgg = new List<string>(availableEggs.Keys)[0];
            PlayerPrefs.SetString("CurrentSelectedEgg", currentSelectedEgg);
            PlayerPrefs.Save();
        }
        
        ApplySelectedEgg();
        UpdateTotalEggsBought();
    }
    
    void LoadPurchasedEggs()
    {
        string purchasedItems = PlayerPrefs.GetString("PurchasedItems", "");
        Debug.Log("Raw purchased items string: " + purchasedItems);
        
        if (!string.IsNullOrEmpty(purchasedItems))
        {
            string[] purchasedEggs = purchasedItems.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
            Debug.Log("Found " + purchasedEggs.Length + " items in the split array");
            
            foreach (string eggEntry in purchasedEggs)
            {
                string[] parts = eggEntry.Split(':');
                if (parts.Length != 2 || !int.TryParse(parts[1], out int quantity)) continue;
                
                string eggName = parts[0];
                Debug.Log("Processing egg: '" + eggName + "' with quantity: " + quantity);
                
                if (!availableEggs.ContainsKey(eggName))
                {
                    ShopItemSO eggData = FindEggSOByTitle(eggName);
                    if (eggData != null)
                    {
                        Debug.Log("Successfully added egg: " + eggName);
                        availableEggs.Add(eggName, eggData);
                        eggQuantities[eggName] = quantity;
                    }
                    else
                    {
                        Debug.LogWarning("Could not find egg data for: " + eggName);
                    }
                }
                else
                {
                    eggQuantities[eggName] = quantity;
                    Debug.Log("Egg already in dictionary: " + eggName);
                }
            }
        }
        
        Debug.Log("Total unique eggs loaded: " + availableEggs.Count);
    }
    
    ShopItemSO FindEggSOByTitle(string title)
    {
        if (shopDatabase != null && shopDatabase.shopItemsSO != null)
        {
            Debug.Log("Searching " + shopDatabase.shopItemsSO.Length + " items");
            foreach (ShopItemSO item in shopDatabase.shopItemsSO)
            {
                if (item != null && item.title == title)
                {
                    Debug.Log("Found matching egg: " + title);
                    return item;
                }
            }
            Debug.LogWarning("Searched all shop items but didn't find: " + title);
        }
        else
        {
            Debug.LogError("ShopDatabase or shopItemsSO not assigned in PurchasedEggManager");
        }
        return null;
    }
    
    void PopulateEggMenu()
    {
        Debug.Log("Populating egg menu with " + availableEggs.Count + " eggs");
        
        foreach (Transform child in eggMenuContent.transform)
        {
            Destroy(child.gameObject);
        }
        
        foreach (KeyValuePair<string, ShopItemSO> egg in availableEggs)
        {
            Debug.Log("Creating button for egg: " + egg.Key);
            GameObject eggButton = Instantiate(eggButtonPrefab, eggMenuContent.transform);
            
            Image buttonImage = eggButton.GetComponent<Image>();
            if (buttonImage != null && egg.Value.itemPrefab != null)
            {
                Image prefabImage = egg.Value.itemPrefab.GetComponent<Image>();
                if (prefabImage != null && prefabImage.sprite != null)
                {
                    buttonImage.sprite = prefabImage.sprite;
                }
                else
                {
                    SpriteRenderer prefabSprite = egg.Value.itemPrefab.GetComponent<SpriteRenderer>();
                    if (prefabSprite != null && prefabSprite.sprite != null)
                    {
                        buttonImage.sprite = prefabSprite.sprite;
                    }
                    else
                    {
                        Image childImage = egg.Value.itemPrefab.GetComponentInChildren<Image>();
                        if (childImage != null && childImage.sprite != null)
                        {
                            buttonImage.sprite = childImage.sprite;
                        }
                        else
                        {
                            SpriteRenderer childSprite = egg.Value.itemPrefab.GetComponentInChildren<SpriteRenderer>();
                            if (childSprite != null && childSprite.sprite != null)
                            {
                                buttonImage.sprite = childSprite.sprite;
                            }
                            else
                            {
                                Debug.LogWarning("Could not find sprite for egg: " + egg.Key);
                            }
                        }
                    }
                }
            }
            
            Button button = eggButton.GetComponent<Button>();
            string eggName = egg.Key;
            button.onClick.AddListener(() => SelectEgg(eggName));
            
            if (eggName == currentSelectedEgg)
            {
                button.interactable = false;
            }
            
            TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = $"{eggName} (x{eggQuantities[eggName]})";
            }
        }
    }
    
    public void SelectEgg(string eggName)
    {
        if (availableEggs.ContainsKey(eggName))
        {
            currentSelectedEgg = eggName;
            PlayerPrefs.SetString("CurrentSelectedEgg", eggName);
            PlayerPrefs.Save();
            Debug.Log($"Selected egg: {eggName}");

            if (!simpleTimer.isTimerRunning)
            {
                ApplySelectedEgg();
            }
            else
            {
                Debug.Log("Egg selection will be applied after current timer completes");
            }

            PopulateEggMenu();
        }
    }
    
    void ApplySelectedEgg()
    {
        if (simpleTimer != null && availableEggs.ContainsKey(currentSelectedEgg))
        {
            simpleTimer.ChangeEggPrefab(availableEggs[currentSelectedEgg].itemPrefab, availableEggs[currentSelectedEgg]);
            simpleTimer.RespawnCurrentEgg();
            Debug.Log($"Applied egg: {currentSelectedEgg}");
        }
    }
    
    void UpdateTotalEggsBought()
    {
        if (totalEggsBoughtText != null)
        {
            int totalEggs = 0;
            foreach (int quantity in eggQuantities.Values)
            {
                totalEggs += quantity;
            }
            totalEggsBoughtText.text = "Eggs Bought: " + totalEggs;
            Debug.Log("Total eggs bought: " + totalEggs);
        }
    }
    
    public void ClearPurchasedEggs()
    {
        PlayerPrefs.DeleteKey("PurchasedItems");
        PlayerPrefs.DeleteKey("CurrentSelectedEgg");
        PlayerPrefs.Save();
        Debug.Log("Cleared purchased eggs data");
        eggQuantities.Clear();
        availableEggs.Clear();
        Start();
    }
}