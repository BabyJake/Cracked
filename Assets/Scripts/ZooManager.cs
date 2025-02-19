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
    
    private List<Vector2> usedPositions = new List<Vector2>(); // For random positions.
    
    // Reference to the animal instance that was just spawned and awaits manual placement.
    private GameObject pendingAnimalInstance; 

    void Start()
    {
        DisplayUnlockedAnimals();
    }

    void Update() 
    {
        // Check if there's a pending animal and the user taps (works for mouse or mobile)
        if (pendingAnimalInstance != null && Input.GetMouseButtonDown(0))
        {
            // (Optional) Skip taps on UI here if needed using EventSystem.current.IsPointerOverGameObject()
            Vector3 tapPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            tapPosition.z = 0; // For 2D, ensure z is zero.

            // Clamp the position within the enclosure (assuming animalParent.position is the center)
            Vector3 center = animalParent.position;
            float halfWidth = enclosureSize.x / 2;
            float halfHeight = enclosureSize.y / 2;
            float clampedX = Mathf.Clamp(tapPosition.x, center.x - halfWidth, center.x + halfWidth);
            float clampedY = Mathf.Clamp(tapPosition.y, center.y - halfHeight, center.y + halfHeight);
            Vector3 finalPosition = new Vector3(clampedX, clampedY, 0);

            pendingAnimalInstance.transform.position = finalPosition;
            Debug.Log($"Pending animal placed at {finalPosition}");

            // Clear the pending animal so further taps don’t move it.
            pendingAnimalInstance = null;
            // Remove the pending flag from PlayerPrefs.
            PlayerPrefs.DeleteKey("PendingAnimal");
            PlayerPrefs.Save();
        }
    }

    void DisplayUnlockedAnimals()
    {
        string unlockedAnimals = PlayerPrefs.GetString("UnlockedAnimals", "");
        if (string.IsNullOrEmpty(unlockedAnimals))
        {
            Debug.Log("No unlocked animals found.");
            return;
        }
        
        // Check if there is a pending animal set (the one that just hatched)
        string pendingAnimalName = PlayerPrefs.GetString("PendingAnimal", "");

        string[] animalNames = unlockedAnimals.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
        
        foreach (string name in animalNames)
        {
            // If this animal is the pending one, skip random placement here.
            if (!string.IsNullOrEmpty(pendingAnimalName) && name == pendingAnimalName)
            {
                continue;
            }
            
            AnimalEntry entry = allAnimals.Find(x => x.animalName == name);
            if (entry != null && entry.animalPrefab != null)
            {
                // Get a random valid position in the enclosure.
                Vector2 spawnPosition = GetValidPosition();
                // For 2D, we place the animal using x and y.
                GameObject animalInstance = Instantiate(entry.animalPrefab, 
                    new Vector3(spawnPosition.x, spawnPosition.y, 0), 
                    Quaternion.identity, animalParent);
                animalInstance.name = entry.animalName;
            }
            else
            {
                Debug.LogWarning($"No prefab found for animal: {name}");
            }
        }

        // If a pending animal exists, spawn it at a default location (here, the center)
        if (!string.IsNullOrEmpty(pendingAnimalName))
        {
            AnimalEntry entry = allAnimals.Find(x => x.animalName == pendingAnimalName);
            if (entry != null && entry.animalPrefab != null)
            {
                pendingAnimalInstance = Instantiate(entry.animalPrefab, 
                    animalParent.position, Quaternion.identity, animalParent);
                pendingAnimalInstance.name = entry.animalName;
                Debug.Log("Pending animal spawned. Tap anywhere in the zoo to place it.");
            }
            else
            {
                Debug.LogWarning($"No prefab found for pending animal: {pendingAnimalName}");
            }
        }
    }

    Vector2 GetValidPosition()
    {
        Vector2 randomPosition;
        int attempts = 0;
        
        do
        {
            randomPosition = new Vector2(
                Random.Range(-enclosureSize.x / 2, enclosureSize.x / 2),
                Random.Range(-enclosureSize.y / 2, enclosureSize.y / 2)
            );
            attempts++;
            if (attempts > 50) break; // Failsafe to avoid infinite loops.
        }
        while (IsPositionTooClose(randomPosition));

        usedPositions.Add(randomPosition);
        return randomPosition;
    }

    bool IsPositionTooClose(Vector2 newPosition)
    {
        foreach (Vector2 pos in usedPositions)
        {
            if (Vector2.Distance(newPosition, pos) < minDistanceBetweenAnimals)
            {
                return true; // Too close; try another position.
            }
        }
        return false;
    }
}
