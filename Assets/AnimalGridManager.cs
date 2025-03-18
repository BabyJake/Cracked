using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class AnimalGridManager : MonoBehaviour
{
    [Header("Tilemap & Tiles")]
    public Tilemap tilemap;
    public TileBase grassBlockTile;
    
    [Header("Animal Prefabs")]
    public List<GameObject> animalPrefabs;
    public Transform animalParent;

    [Header("UI Elements")]
    public Text dailyText;
    public Text weeklyText;
    public Text yearlyText;
    public Button dailyButton;
    public Button weeklyButton;
    public Button yearlyButton;

    [Header("Isometric Settings")]
    public float animalYOffset = 0.1f; // Offset to make animals appear on top of tiles

    private int gridSize = 3;
    private const string PendingAnimalKey = "PendingAnimal";
    private HatchData hatchData;
    private List<AnimalInstance> animalInstances = new List<AnimalInstance>();
    private string currentView = "All";

    [Serializable]
    public class DailyHatchCount
    {
        public string date;
        public int count;
    }

    [Serializable]
    public class HatchData
    {
        public List<DailyHatchCount> counts = new List<DailyHatchCount>();
    }

    [Serializable]
    public class AnimalInstance
    {
        public GameObject animalObject;
        public Vector3Int gridPosition;
        public string hatchDate;
    }

    void Start()
    {
        // Initialize HatchData first
        hatchData = LoadHatchData();
        
        // Ensure the grid is set to isometric layout
        if (tilemap.layoutGrid != null)
        {
            tilemap.layoutGrid.cellLayout = GridLayout.CellLayout.Isometric;
        }
        
        FillGrid();
        
        // Debug to show what's in the UnlockedAnimals string
        string unlockedAnimalsStr = PlayerPrefs.GetString("UnlockedAnimals", "");
        Debug.Log("UnlockedAnimals: " + unlockedAnimalsStr);
        
        // Clear any existing UI text to prevent null refs
        SafeUpdateHatchCountUI();
        
        // First check if there's a pending animal
        string pendingAnimal = PlayerPrefs.GetString(PendingAnimalKey, "");
        if (!string.IsNullOrEmpty(pendingAnimal))
        {
            Debug.Log("Processing pending animal: " + pendingAnimal);
            
            // Add the pending animal to the UnlockedAnimals list first
            AddAnimalToUnlockedList(pendingAnimal);
            
            // Clear the pending key
            PlayerPrefs.DeleteKey(PendingAnimalKey);
            PlayerPrefs.Save();
        }
        
        // Now process all unlocked animals at once
        ProcessUnlockedAnimals();

        UpdateGridVisibility();

        // Add button listeners after everything is set up
        if (dailyButton != null) dailyButton.onClick.AddListener(() => SetView("Day"));
        if (weeklyButton != null) weeklyButton.onClick.AddListener(() => SetView("Week"));
        if (yearlyButton != null) yearlyButton.onClick.AddListener(() => SetView("Year"));
    }
    
    // Helper method to add an animal to the unlocked list
    private void AddAnimalToUnlockedList(string animalName)
    {
        string unlockedAnimalsStr = PlayerPrefs.GetString("UnlockedAnimals", "");
        
        // Check if the animal is already in the list
        string[] existingAnimals = unlockedAnimalsStr.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
        foreach (string animal in existingAnimals)
        {
            if (animal == animalName)
            {
                Debug.Log("Animal already in unlocked list: " + animalName);
                return;  // Already in the list, no need to add
            }
        }
        
        // Add the new animal to the list
        if (string.IsNullOrEmpty(unlockedAnimalsStr))
        {
            unlockedAnimalsStr = animalName;
        }
        else
        {
            unlockedAnimalsStr += "," + animalName;
        }
        
        PlayerPrefs.SetString("UnlockedAnimals", unlockedAnimalsStr);
        PlayerPrefs.Save();
        Debug.Log("Added animal to unlocked list: " + animalName);
    }
    
    private void ProcessUnlockedAnimals()
    {
        string unlockedAnimalsStr = PlayerPrefs.GetString("UnlockedAnimals", "");
        if (!string.IsNullOrEmpty(unlockedAnimalsStr))
        {
            string[] animalNames = unlockedAnimalsStr.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
            Debug.Log("Found " + animalNames.Length + " unlocked animals");
            
            foreach (string animalName in animalNames)
            {
                Debug.Log("Processing unlocked animal: " + animalName);
                
                // Verify the animal prefab exists before trying to spawn it
                if (GetAnimalPrefabByName(animalName) != null)
                {
                    // This method already checks if animal exists in grid
                    PlaceUnlockedAnimal(animalName);
                }
                else
                {
                    Debug.LogWarning("No prefab found for animal: " + animalName);
                }
            }
        }
        else
        {
            Debug.Log("No unlocked animals found in PlayerPrefs");
        }
    }
    
    // This places an already unlocked animal without registering it as a new hatch
    private void PlaceUnlockedAnimal(string animalName)
    {
        // Skip if this animal already exists in the grid
        if (AnimalExistsInGrid(animalName))
        {
            Debug.Log("Animal already exists in grid: " + animalName);
            return;
        }
        
        bool placed = PlaceAnimalOnRandomCell(animalName, out Vector3Int placedPosition, out GameObject spawnedAnimal);
        int attempts = 0;
        const int maxAttempts = 10;
        
        // Try to place the animal, expanding grid if needed, but limit attempts to avoid infinite loop
        while (!placed && attempts < maxAttempts)
        {
            attempts++;
            ExpandGrid();
            FillGrid();
            placed = PlaceAnimalOnRandomCell(animalName, out placedPosition, out spawnedAnimal);
        }
        
        if (placed)
        {
            // Use an older date for unlocked animals to differentiate from newly hatched
            string hatchDate = DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd");
            animalInstances.Add(new AnimalInstance { animalObject = spawnedAnimal, gridPosition = placedPosition, hatchDate = hatchDate });
            Debug.Log($"Successfully placed unlocked animal: {animalName} at position {placedPosition}");
        }
        else
        {
            Debug.LogError($"Failed to place animal {animalName} after {maxAttempts} attempts");
        }
    }

    // Helper method to check if animal already exists in grid
    bool AnimalExistsInGrid(string animalName)
    {
        foreach (var instance in animalInstances)
        {
            if (instance.animalObject != null && instance.animalObject.name.Contains(animalName))
            {
                return true;
            }
        }
        return false;
    }

    void FillGrid()
    {
        int halfSize = gridSize / 2;
        for (int x = -halfSize; x < (-halfSize + gridSize); x++)
        {
            for (int y = -halfSize; y < (-halfSize + gridSize); y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                if (tilemap.GetTile(pos) == null)
                {
                    tilemap.SetTile(pos, grassBlockTile);
                }
            }
        }
    }

    public void HatchAnimal(string animalName)
    {
        // Skip if animal prefab doesn't exist
        if (GetAnimalPrefabByName(animalName) == null)
        {
            Debug.LogError("Cannot hatch animal, prefab not found: " + animalName);
            return;
        }
        
        bool placed = PlaceAnimalOnRandomCell(animalName, out Vector3Int placedPosition, out GameObject spawnedAnimal);
        int attempts = 0;
        const int maxAttempts = 10;
        
        while (!placed && attempts < maxAttempts)
        {
            attempts++;
            ExpandGrid();
            FillGrid();
            placed = PlaceAnimalOnRandomCell(animalName, out placedPosition, out spawnedAnimal);
        }
        
        if (placed)
        {
            string today = DateTime.Today.ToString("yyyy-MM-dd");
            animalInstances.Add(new AnimalInstance { animalObject = spawnedAnimal, gridPosition = placedPosition, hatchDate = today });
            RecordHatching();
            SafeUpdateHatchCountUI();
            Debug.Log($"Successfully hatched new animal: {animalName} at position {placedPosition}");
        }
        else
        {
            Debug.LogError($"Failed to hatch animal {animalName} after {maxAttempts} attempts");
        }
    }

    bool PlaceAnimalOnRandomCell(string animalName, out Vector3Int placedPosition, out GameObject spawnedAnimal)
    {
        placedPosition = Vector3Int.zero;
        spawnedAnimal = null;
        List<Vector3Int> emptyCells = GetEmptyCells();
        if (emptyCells.Count == 0)
        {
            Debug.LogWarning("No empty cells available for animal placement");
            return false;
        }
        
        placedPosition = emptyCells[UnityEngine.Random.Range(0, emptyCells.Count)];
        
        // Get the world position from the tilemap for isometric grid
        Vector3 worldPos = tilemap.GetCellCenterWorld(placedPosition);
        
        // Add a Y offset to make sure the animal appears to stand on the tile
        worldPos.y += animalYOffset;
        
        GameObject animalPrefab = GetAnimalPrefabByName(animalName);
        if (animalPrefab != null)
        {
            spawnedAnimal = Instantiate(animalPrefab, worldPos, Quaternion.identity, animalParent);
            spawnedAnimal.name = animalPrefab.name; // Don't append Clone
            
            // Set the sorting order based on y-position to maintain proper depth
            SpriteRenderer renderer = spawnedAnimal.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                // Higher grid position Y = further back = lower sorting order
                renderer.sortingOrder = -placedPosition.y;
            }
            
            // Add isometric sorting component if it doesn't exist
            if (spawnedAnimal.GetComponent<IsometricSorting>() == null)
            {
                spawnedAnimal.AddComponent<IsometricSorting>();
            }
            
            return true;
        }
        return false;
    }

    List<Vector3Int> GetEmptyCells()
    {
        List<Vector3Int> emptyCells = new List<Vector3Int>();
        int halfSize = gridSize / 2;
        for (int x = -halfSize; x < (-halfSize + gridSize); x++)
        {
            for (int y = -halfSize; y < (-halfSize + gridSize); y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                if (tilemap.GetTile(pos) == grassBlockTile && !IsPositionOccupied(pos))
                {
                    emptyCells.Add(pos);
                }
            }
        }
        return emptyCells;
    }

    bool IsPositionOccupied(Vector3Int pos)
    {
        foreach (var instance in animalInstances)
        {
            if (instance.gridPosition == pos && instance.animalObject != null && instance.animalObject.activeSelf)
            {
                return true;
            }
        }
        return false;
    }

    void ExpandGrid()
    {
        gridSize += 1;
        Debug.Log("Grid expanded to: " + gridSize + "x" + gridSize);
    }

    GameObject GetAnimalPrefabByName(string animalName)
    {
        foreach (GameObject prefab in animalPrefabs)
        {
            if (prefab.name == animalName)
            {
                return prefab;
            }
        }
        Debug.LogWarning("Could not find prefab for animal: " + animalName);
        return null;
    }

    private void RecordHatching()
    {
        string today = DateTime.Today.ToString("yyyy-MM-dd");
        DailyHatchCount todayCount = hatchData.counts.Find(c => c.date == today);
        if (todayCount != null)
        {
            todayCount.count++;
        }
        else
        {
            hatchData.counts.Add(new DailyHatchCount { date = today, count = 1 });
        }
        SaveHatchData();
    }

    private HatchData LoadHatchData()
    {
        string json = PlayerPrefs.GetString("HatchData", "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                return JsonUtility.FromJson<HatchData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError("Error parsing HatchData JSON: " + e.Message);
                return new HatchData { counts = new List<DailyHatchCount>() };
            }
        }
        return new HatchData { counts = new List<DailyHatchCount>() };
    }

    private void SaveHatchData()
    {
        try
        {
            string json = JsonUtility.ToJson(hatchData);
            PlayerPrefs.SetString("HatchData", json);
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogError("Error saving HatchData: " + e.Message);
        }
    }

    public int GetDailyHatchCount()
    {
        string today = DateTime.Today.ToString("yyyy-MM-dd");
        DailyHatchCount todayCount = hatchData.counts.Find(c => c.date == today);
        return todayCount != null ? todayCount.count : 0;
    }

    public int GetWeeklyHatchCount()
    {
        DateTime today = DateTime.Today;
        DateTime weekAgo = today.AddDays(-6);
        int sum = 0;
        foreach (var count in hatchData.counts)
        {
            try
            {
                DateTime date = DateTime.ParseExact(count.date, "yyyy-MM-dd", null);
                if (date >= weekAgo && date <= today)
                {
                    sum += count.count;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error parsing date: " + e.Message);
            }
        }
        return sum;
    }

    public int GetYearlyHatchCount()
    {
        DateTime today = DateTime.Today;
        DateTime yearAgo = today.AddDays(-364);
        int sum = 0;
        foreach (var count in hatchData.counts)
        {
            try
            {
                DateTime date = DateTime.ParseExact(count.date, "yyyy-MM-dd", null);
                if (date >= yearAgo && date <= today)
                {
                    sum += count.count;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error parsing date: " + e.Message);
            }
        }
        return sum;
    }

    // Safe version of UpdateHatchCountUI that handles null references
    public void SafeUpdateHatchCountUI()
    {
        if (dailyText != null) dailyText.text = "Today: " + GetDailyHatchCount();
        if (weeklyText != null) weeklyText.text = "This Week: " + GetWeeklyHatchCount();
        if (yearlyText != null) yearlyText.text = "This Year: " + GetYearlyHatchCount();
    }

    private void UpdateGridVisibility()
    {
        DateTime today = DateTime.Today;
        foreach (var instance in animalInstances)
        {
            if (instance.animalObject == null) continue;
            
            try
            {
                DateTime hatchDate = DateTime.ParseExact(instance.hatchDate, "yyyy-MM-dd", null);
                bool shouldBeVisible = false;
                switch (currentView)
                {
                    case "Day":
                        shouldBeVisible = hatchDate == today;
                        break;
                    case "Week":
                        shouldBeVisible = hatchDate >= today.AddDays(-6) && hatchDate <= today;
                        break;
                    case "Year":
                        shouldBeVisible = hatchDate >= today.AddDays(-364) && hatchDate <= today;
                        break;
                    case "All":
                    default:
                        shouldBeVisible = true;
                        break;
                }
                instance.animalObject.SetActive(shouldBeVisible);
                Debug.Log($"{instance.animalObject.name} hatched on {instance.hatchDate} - Visible: {shouldBeVisible}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error updating visibility for {instance.animalObject.name}: {e.Message}");
                // Default to visible if there's an error
                instance.animalObject.SetActive(true);
            }
        }
    }

    private void SetView(string view)
    {
        currentView = view;
        UpdateGridVisibility();
        SafeUpdateHatchCountUI();
    }
}

// Add this component to handle isometric sorting
public class IsometricSorting : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateSortingOrder();
    }
    
    void Update()
    {
        UpdateSortingOrder();
    }
    
    void UpdateSortingOrder()
    {
        if (spriteRenderer != null)
        {
            // The higher the y-coordinate in world space, the further back the object should appear
            // Multiply by -100 to get more precision and avoid sorting conflicts
            spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
        }
    }
}