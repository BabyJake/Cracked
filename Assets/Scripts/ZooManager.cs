using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class AnimalEntry
{
    public string animalName;
    public GameObject animalPrefab;
}

public class ZooManager : MonoBehaviour
{
    [Header("Animal Settings")]
    [Tooltip("List all possible animals with their corresponding prefabs.")]
    public List<AnimalEntry> allAnimals;

    [Header("Enclosure Settings")]
    [Tooltip("Parent object to hold animal instances.")]
    public Transform animalParent;
    [Tooltip("Size of the enclosure (width & height).")]
    public Vector2 enclosureSize = new Vector2(10f, 10f);
    [Tooltip("Minimum distance between animals to prevent overlap.")]
    public float minDistanceBetweenAnimals = 1.5f;
    
    private List<Vector2> usedPositions = new List<Vector2>(); // Track used positions

    void Start()
    {
        DisplayUnlockedAnimals();
    }

    void DisplayUnlockedAnimals()
    {
        string unlockedAnimals = PlayerPrefs.GetString("UnlockedAnimals", "");
        if (string.IsNullOrEmpty(unlockedAnimals))
        {
            Debug.Log("No unlocked animals found.");
            return;
        }

        string[] animalNames = unlockedAnimals.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
        
        foreach (string name in animalNames)
        {
            AnimalEntry entry = allAnimals.Find(x => x.animalName == name);
            if (entry != null && entry.animalPrefab != null)
            {
                // Find a valid random position inside the enclosure
                Vector2 spawnPosition = GetValidPosition();
                
                // Instantiate the animal at the position
                GameObject animalInstance = Instantiate(entry.animalPrefab, new Vector3(spawnPosition.x, 0, spawnPosition.y), Quaternion.Euler(0, Random.Range(0, 360), 0), animalParent);
                
                animalInstance.name = entry.animalName; // Rename instance
            }
            else
            {
                Debug.LogWarning($"No prefab found for animal: {name}");
            }
        }
    }

    Vector2 GetValidPosition()
    {
        Vector2 randomPosition;
        int attempts = 0;
        
        do
        {
            // Random position inside the enclosure
            randomPosition = new Vector2(
                Random.Range(-enclosureSize.x / 2, enclosureSize.x / 2),
                Random.Range(-enclosureSize.y / 2, enclosureSize.y / 2)
            );

            attempts++;

            // Prevent infinite loops (failsafe)
            if (attempts > 50) break;
        }
        while (IsPositionTooClose(randomPosition));

        usedPositions.Add(randomPosition); // Save used position
        return randomPosition;
    }

    bool IsPositionTooClose(Vector2 newPosition)
    {
        foreach (Vector2 pos in usedPositions)
        {
            if (Vector2.Distance(newPosition, pos) < minDistanceBetweenAnimals)
            {
                return true; // Too close, pick another position
            }
        }
        return false;
    }
}
