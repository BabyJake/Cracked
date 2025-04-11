using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class PurchasedEggManager : MonoBehaviour
{
    public SimpleTimer simpleTimer;
    public GameObject eggMenuContent;
    public GameObject eggButtonPrefab;
    
    // Reference to the ShopItemSO for default egg
    public ShopItemSO defaultEggSO;
    
    private string currentSelectedEgg;
    private Dictionary<string, ShopItemSO> availableEggs = new Dictionary<string, ShopItemSO>();
    
    void Start()
    {
        Debug.Log("PurchasedEggManager started");
        
        // Add default egg that player always has
        if (defaultEggSO != null)
        {
            availableEggs.Add(defaultEggSO.title, defaultEggSO);
        }
        
        // Load purchased eggs from PlayerPrefs
        LoadPurchasedEggs();
        
        // Populate the egg menu
        PopulateEggMenu();
        
        // Set current egg (default to the first available one)
        if (!string.IsNullOrEmpty(PlayerPrefs.GetString("CurrentSelectedEgg", "")))
        {
            currentSelectedEgg = PlayerPrefs.GetString("CurrentSelectedEgg");
        }
        else if (availableEggs.Count > 0)
        {
            // Default to first egg if none selected
            currentSelectedEgg = new List<string>(availableEggs.Keys)[0];
            PlayerPrefs.SetString("CurrentSelectedEgg", currentSelectedEgg);
            PlayerPrefs.Save();
        }
        
        // Apply the selected egg to the current egg
        ApplySelectedEgg();
    }
    
    void LoadPurchasedEggs()
    {
        string purchasedItems = PlayerPrefs.GetString("PurchasedItems", "");
        Debug.Log("Raw purchased items string: " + purchasedItems);
        
        if (!string.IsNullOrEmpty(purchasedItems))
        {
            string[] purchasedEggs = purchasedItems.Split(',');
            Debug.Log("Found " + purchasedEggs.Length + " items in the split array");
            
            foreach (string eggName in purchasedEggs)
            {
                Debug.Log("Processing egg: '" + eggName + "'");
                if (!string.IsNullOrEmpty(eggName) && !availableEggs.ContainsKey(eggName))
                {
                    // Find the corresponding ShopItemSO
                    ShopItemSO eggData = FindEggSOByTitle(eggName);
                    if (eggData != null)
                    {
                        Debug.Log("Successfully added egg: " + eggName);
                        availableEggs.Add(eggName, eggData);
                    }
                    else
                    {
                        Debug.LogWarning("Could not find egg data for: " + eggName);
                    }
                }
                else if (string.IsNullOrEmpty(eggName))
                {
                    Debug.Log("Skipping empty egg name");
                }
                else if (availableEggs.ContainsKey(eggName))
                {
                    Debug.Log("Egg already in dictionary: " + eggName);
                }
            }
        }
        
        Debug.Log("Total eggs loaded: " + availableEggs.Count);
    }
    
    ShopItemSO FindEggSOByTitle(string title)
    {
        // Find the EggShopManager to access all available shop items
        EggShopManager shopManager = FindObjectOfType<EggShopManager>();
        if (shopManager != null)
        {
            Debug.Log("Found EggShopManager with " + shopManager.shopItemsSO.Length + " items");
            foreach (ShopItemSO item in shopManager.shopItemsSO)
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
            Debug.LogError("Could not find EggShopManager - make sure it exists in the scene");
        }
        return null;
    }
    
    void PopulateEggMenu()
    {
        Debug.Log("Populating egg menu with " + availableEggs.Count + " eggs");
        
        // Clear existing egg buttons
        foreach (Transform child in eggMenuContent.transform)
        {
            Destroy(child.gameObject);
        }
        
        // Add button for each available egg
        foreach (KeyValuePair<string, ShopItemSO> egg in availableEggs)
        {
            Debug.Log("Creating button for egg: " + egg.Key);
            GameObject eggButton = Instantiate(eggButtonPrefab, eggMenuContent.transform);
            
            // Set button image from itemPrefab's sprite
            Image buttonImage = eggButton.GetComponent<Image>();
            if (buttonImage != null && egg.Value.itemPrefab != null)
            {
                // Try to get image component directly from the prefab
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
                        // If not found on the main object, try to find in children
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
            
            // Add click handler
            Button button = eggButton.GetComponent<Button>();
            string eggName = egg.Key; // Create local variable to capture for lambda
            button.onClick.AddListener(() => SelectEgg(eggName));
            
            // Highlight the currently selected egg
            if (eggName == currentSelectedEgg)
            {
                button.interactable = false; // Visual indicator of selection
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
            
            // Apply selection to current egg if timer is not running
            if (!simpleTimer.isTimerRunning)
            {
                ApplySelectedEgg();
            }
            else
            {
                Debug.Log("Egg selection will be applied after current timer completes");
            }
            
            // Update visual selection in menu
            PopulateEggMenu();
        }
    }
    
    void ApplySelectedEgg()
    {
        if (simpleTimer != null && availableEggs.ContainsKey(currentSelectedEgg))
        {
            // Change the egg prefab in SimpleTimer
            simpleTimer.ChangeEggPrefab(availableEggs[currentSelectedEgg].itemPrefab);
            
            // Respawn the egg with the new prefab
            simpleTimer.RespawnCurrentEgg();
        }
    }
    
    // Utility method to clear PlayerPrefs data for testing
    public void ClearPurchasedEggs()
    {
        PlayerPrefs.DeleteKey("PurchasedItems");
        PlayerPrefs.DeleteKey("CurrentSelectedEgg");
        PlayerPrefs.Save();
        Debug.Log("Cleared purchased eggs data");
        
        // Reload the scene or just restart the manager
        Start();
    }
}