using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class AnimalEntry
{
    public string animalName;      // The name used to identify the animal (must match saved name)
    public GameObject animalPrefab; // The prefab to instantiate
}

public class ZooManager : MonoBehaviour
{
    [Header("Animal Settings")]
    [Tooltip("List all possible animals with their corresponding prefabs.")]
    public List<AnimalEntry> allAnimals;

    [Header("Layout Settings")]
    [Tooltip("Parent object to hold animal instances.")]
    public Transform animalParent;
    [Tooltip("Starting position for the grid layout.")]
    public Vector2 startPosition = new Vector2(-3f, 3f);
    [Tooltip("Horizontal and vertical spacing between animals.")]
    public Vector2 gridSpacing = new Vector2(3f, 3f);
    [Tooltip("Number of animals per row.")]
    public int animalsPerRow = 3;

    void Start()
    {
        DisplayUnlockedAnimals();
    }

    void DisplayUnlockedAnimals()
    {
        // Retrieve the comma-separated list of unlocked animal names from PlayerPrefs
        string unlockedAnimals = PlayerPrefs.GetString("UnlockedAnimals", "");
        if (string.IsNullOrEmpty(unlockedAnimals))
        {
            Debug.Log("No unlocked animals found.");
            return;
        }

        // Split the string by commas (ignoring empty entries)
        string[] animalNames = unlockedAnimals.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
        
        int index = 0;
        foreach (string name in animalNames)
        {
            // Find the corresponding AnimalEntry from the list
            AnimalEntry entry = allAnimals.Find(x => x.animalName == name);
            if (entry != null && entry.animalPrefab != null)
            {
                // Calculate grid position based on index
                int row = index / animalsPerRow;
                int col = index % animalsPerRow;
                Vector2 position = startPosition + new Vector2(col * gridSpacing.x, -row * gridSpacing.y);

                // Instantiate the animal prefab at the calculated position
                GameObject animalInstance = Instantiate(entry.animalPrefab, position, Quaternion.identity, animalParent);
                animalInstance.name = entry.animalName; // Optional: rename the instance

                index++;
            }
            else
            {
                Debug.LogWarning($"No prefab found for animal: {name}");
            }
        }
    }
}
