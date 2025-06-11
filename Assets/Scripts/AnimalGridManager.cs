using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using System.Linq;

public class AnimalGridManager : MonoBehaviour
{
    [Header("Tilemap & Tiles")]
    public Tilemap tilemap;
    public TileBase grassBlockTile;

    [Header("Animal Prefabs")]
    public List<GameObject> animalPrefabs;
    public Transform animalParent;
    public GameObject gravePrefab;

    [Header("UI Elements")]
    public Text dailyText;
    public Text weeklyText;
    public Text monthlyText;
    public Text yearlyText;
    public Button dailyButton;
    public Button weeklyButton;
    public Button monthlyButton;
    public Button yearlyButton;

    [Header("Isometric Settings")]
    public float animalYOffset = 0.1f;
    public float padding = 1.5f; // Padding around the grid
    public float minOrthographicSize = 5f; // Minimum camera size
    public float maxOrthographicSize = 15f; // Maximum camera size
    public float cameraAdjustSpeed = 2f; // Speed at which camera adjusts

    private int gridSize = 3;
    private const string PendingAnimalKey = "PendingAnimal";
    private const string UnlockedGravesKey = "UnlockedGraves";
    private HatchData hatchData;
    private List<AnimalInstance> animalInstances = new List<AnimalInstance>();
    private string currentView = "All";
    private const string NewlyHatchedAnimalsKey = "NewlyHatchedAnimals";
    private Camera mainCamera;
    private float targetOrthographicSize;
    
    // Store original positions for "All" view restoration
    private Dictionary<string, Vector3Int> originalPositions = new Dictionary<string, Vector3Int>();

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
        public bool isNewlyHatched;
        public bool isGrave;
        public string id;
    }

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main camera not found!");
            return;
        }

        targetOrthographicSize = mainCamera.orthographicSize;
        Debug.Log("Current date: " + DateTime.Today.ToString("yyyy-MM-dd"));
        hatchData = LoadHatchData();

        if (tilemap.layoutGrid != null)
        {
            tilemap.layoutGrid.cellLayout = GridLayout.CellLayout.Isometric;
        }

        FillGrid();

        string unlockedAnimalsStr = PlayerPrefs.GetString("UnlockedAnimals", "");
        Debug.Log("UnlockedAnimals: " + unlockedAnimalsStr);

        SafeUpdateHatchCountUI();

        string pendingAnimal = PlayerPrefs.GetString(PendingAnimalKey, "");
        if (!string.IsNullOrEmpty(pendingAnimal))
        {
            Debug.Log("Processing pending animal: " + pendingAnimal);
            AddAnimalToUnlockedList(pendingAnimal);
            AddAnimalToNewlyHatchedList(pendingAnimal);
            PlayerPrefs.DeleteKey(PendingAnimalKey);
            PlayerPrefs.Save();
        }

        ProcessUnlockedAnimals();
        ProcessUnlockedGraves();
        UpdateGridVisibility();
        AdjustCameraToFitAllObjects();

        if (dailyButton != null) dailyButton.onClick.AddListener(() => SetView("Day"));
        if (weeklyButton != null) weeklyButton.onClick.AddListener(() => SetView("Week"));
        if (monthlyButton != null) monthlyButton.onClick.AddListener(() => SetView("Month"));
        if (yearlyButton != null) yearlyButton.onClick.AddListener(() => SetView("Year"));
    }

    void Update()
    {
        // Smoothly adjust camera size
        if (mainCamera != null && Mathf.Abs(mainCamera.orthographicSize - targetOrthographicSize) > 0.01f)
        {
            mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, targetOrthographicSize, Time.deltaTime * cameraAdjustSpeed);
        }
    }

    private void AdjustCameraToFitAllObjects()
    {
        if (mainCamera == null) return;

        Bounds bounds = new Bounds();
        bool hasBounds = false;

        // Include all active animal instances
        foreach (var instance in animalInstances)
        {
            if (instance.animalObject != null && instance.animalObject.activeSelf)
            {
                if (!hasBounds)
                {
                    bounds = new Bounds(instance.animalObject.transform.position, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(instance.animalObject.transform.position);
                }
            }
        }

        // If no objects found, use grid bounds
        if (!hasBounds)
        {
            int halfSize = gridSize / 2;
            Vector3 minPos = tilemap.GetCellCenterWorld(new Vector3Int(-halfSize, -halfSize, 0));
            Vector3 maxPos = tilemap.GetCellCenterWorld(new Vector3Int(halfSize - 1, halfSize - 1, 0));
            bounds = new Bounds((minPos + maxPos) * 0.5f, maxPos - minPos);
        }

        // Add padding to bounds
        bounds.Expand(padding);

        // Calculate required orthographic size
        float screenRatio = (float)Screen.width / Screen.height;
        float targetSize = bounds.size.y * 0.5f;
        float targetSizeX = bounds.size.x * 0.5f / screenRatio;
        targetSize = Mathf.Max(targetSize, targetSizeX);

        // Clamp the size between min and max values
        targetOrthographicSize = Mathf.Clamp(targetSize, minOrthographicSize, maxOrthographicSize);
    }

    // Call this method whenever the grid changes
    private void OnGridChanged()
    {
        AdjustCameraToFitAllObjects();
    }

    private void AddAnimalToNewlyHatchedList(string animalName)
    {
        string newlyHatchedStr = PlayerPrefs.GetString(NewlyHatchedAnimalsKey, "");
        if (string.IsNullOrEmpty(newlyHatchedStr))
        {
            newlyHatchedStr = animalName;
        }
        else
        {
            newlyHatchedStr += "," + animalName;
        }
        PlayerPrefs.SetString(NewlyHatchedAnimalsKey, newlyHatchedStr);
        PlayerPrefs.Save();
    }

    private bool IsAnimalNewlyHatched(string animalName)
    {
        string newlyHatchedStr = PlayerPrefs.GetString(NewlyHatchedAnimalsKey, "");
        if (string.IsNullOrEmpty(newlyHatchedStr)) return false;
        return newlyHatchedStr.Split(',').Contains(animalName);
    }

    private void AddAnimalToUnlockedList(string animalName)
    {
        string unlockedAnimalsStr = PlayerPrefs.GetString("UnlockedAnimals", "");
        if (!unlockedAnimalsStr.Contains(animalName))
        {
            unlockedAnimalsStr += string.IsNullOrEmpty(unlockedAnimalsStr) ? animalName : "," + animalName;
            PlayerPrefs.SetString("UnlockedAnimals", unlockedAnimalsStr);
            PlayerPrefs.Save();
        }
    }

    private void ProcessUnlockedAnimals()
    {
        string unlockedAnimalsStr = PlayerPrefs.GetString("UnlockedAnimals", "");
        if (!string.IsNullOrEmpty(unlockedAnimalsStr))
        {
            string[] animalNames = unlockedAnimalsStr.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
            foreach (string animalName in animalNames)
            {
                if (GetAnimalPrefabByName(animalName) != null)
                {
                    PlaceUnlockedAnimal(animalName, IsAnimalNewlyHatched(animalName));
                }
            }
        }
    }

    private void ProcessUnlockedGraves()
    {
        string unlockedGravesStr = PlayerPrefs.GetString(UnlockedGravesKey, "");
        if (!string.IsNullOrEmpty(unlockedGravesStr))
        {
            string[] graveIds = unlockedGravesStr.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
            foreach (string graveId in graveIds)
            {
                if (string.IsNullOrWhiteSpace(graveId) || GraveExistsInGrid(graveId)) continue;
                
                string hatchDate = PlayerPrefs.GetString(graveId + "_date", DateTime.Today.ToString("yyyy-MM-dd"));
                PlaceGraveWithId(graveId, hatchDate);
            }
        }
    }

    private bool GraveExistsInGrid(string graveId)
    {
        return animalInstances.Exists(i => i.isGrave && i.id == graveId);
    }

    private void PlaceUnlockedAnimal(string animalName, bool isNewlyHatched)
    {
        if (AnimalExistsInGrid(animalName)) return;

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
            string hatchDate = isNewlyHatched ? DateTime.Today.ToString("yyyy-MM-dd") : DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd");
            if (isNewlyHatched) RecordHatching();
            
            var instance = new AnimalInstance
            {
                animalObject = spawnedAnimal,
                gridPosition = placedPosition,
                hatchDate = hatchDate,
                isNewlyHatched = isNewlyHatched,
                isGrave = false,
                id = animalName
            };
            
            animalInstances.Add(instance);
            
            // Store original position for restoration
            originalPositions[GetInstanceKey(instance)] = placedPosition;
            
            Debug.Log($"Placed animal: {animalName} at {placedPosition}");
            OnGridChanged();
        }
    }

    private void PlaceGrave()
    {
        string graveId = "Grave_" + DateTime.Now.Ticks;
        string unlockedGravesStr = PlayerPrefs.GetString(UnlockedGravesKey, "");
        
        if (!unlockedGravesStr.Contains(graveId))
        {
            unlockedGravesStr += string.IsNullOrEmpty(unlockedGravesStr) ? graveId : "," + graveId;
            PlayerPrefs.SetString(UnlockedGravesKey, unlockedGravesStr);
            PlayerPrefs.SetString(graveId + "_date", DateTime.Today.ToString("yyyy-MM-dd"));
            PlayerPrefs.Save();
            PlaceGraveWithId(graveId, DateTime.Today.ToString("yyyy-MM-dd"));
        }
    }
    
    private void PlaceGraveWithId(string graveId, string hatchDate)
    {
        bool placed = PlaceGraveOnRandomCell(out Vector3Int placedPosition, out GameObject spawnedGrave);
        int attempts = 0;
        const int maxAttempts = 10;

        while (!placed && attempts < maxAttempts)
        {
            attempts++;
            ExpandGrid();
            FillGrid();
            placed = PlaceGraveOnRandomCell(out placedPosition, out spawnedGrave);
        }

        if (placed)
        {
            // Get the egg type for this grave
            string eggType = PlayerPrefs.GetString(graveId + "_eggType", "CommonEgg");
            
            // Set the grave color based on egg type
            SpriteRenderer graveRenderer = spawnedGrave.GetComponent<SpriteRenderer>();
            if (graveRenderer != null)
            {
                if (eggType == "RareEgg")
                {
                    graveRenderer.color = new Color(0.3411765f, 0.8175273f, 1f, 1f); // Blue color for rare egg
                }
                // For common eggs, we'll use the default color (white)
            }

            var instance = new AnimalInstance
            {
                animalObject = spawnedGrave,
                gridPosition = placedPosition,
                hatchDate = hatchDate,
                isNewlyHatched = false,
                isGrave = true,
                id = graveId
            };
            
            animalInstances.Add(instance);
            
            // Store original position for restoration
            originalPositions[GetInstanceKey(instance)] = placedPosition;
            
            Debug.Log($"Placed grave {graveId} at {placedPosition} with egg type {eggType}");
            OnGridChanged();
        }
        else
        {
            Debug.LogError("Failed to place grave after max attempts");
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
        Vector3 worldPos = tilemap.GetCellCenterWorld(placedPosition);
        worldPos.y += animalYOffset;

        GameObject animalPrefab = GetAnimalPrefabByName(animalName);
        if (animalPrefab != null)
        {
            spawnedAnimal = Instantiate(animalPrefab, worldPos, Quaternion.identity, animalParent);
            spawnedAnimal.name = animalPrefab.name;
            SpriteRenderer renderer = spawnedAnimal.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = -placedPosition.y;
            }
            if (spawnedAnimal.GetComponent<IsometricSorting>() == null)
            {
                spawnedAnimal.AddComponent<IsometricSorting>();
            }
            return true;
        }
        return false;
    }

    bool PlaceGraveOnRandomCell(out Vector3Int placedPosition, out GameObject spawnedGrave)
    {
        placedPosition = Vector3Int.zero;
        spawnedGrave = null;
        List<Vector3Int> emptyCells = GetEmptyCells();
        if (emptyCells.Count == 0) return false;

        placedPosition = emptyCells[UnityEngine.Random.Range(0, emptyCells.Count)];
        Vector3 worldPos = tilemap.GetCellCenterWorld(placedPosition);
        worldPos.y += animalYOffset;

        if (gravePrefab != null)
        {
            spawnedGrave = Instantiate(gravePrefab, worldPos, Quaternion.identity, animalParent);
            spawnedGrave.name = "Grave";
            SpriteRenderer renderer = spawnedGrave.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = -placedPosition.y;
            }
            if (spawnedGrave.GetComponent<IsometricSorting>() == null)
            {
                spawnedGrave.AddComponent<IsometricSorting>();
            }
            return true;
        }
        return false;
    }

    bool AnimalExistsInGrid(string animalName)
    {
        return animalInstances.Exists(i => i.animalObject != null && i.animalObject.name.Contains(animalName) && !i.isGrave);
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
        return animalInstances.Exists(i => i.gridPosition == pos && i.animalObject != null && i.animalObject.activeSelf);
    }

    void ExpandGrid()
    {
        gridSize += 1;
        Debug.Log("Grid expanded to: " + gridSize + "x" + gridSize);
    }

    GameObject GetAnimalPrefabByName(string animalName)
    {
        return animalPrefabs.Find(prefab => prefab.name == animalName);
    }

    private string GetInstanceKey(AnimalInstance instance)
    {
        return instance.id + (instance.isGrave ? "_grave" : "_animal");
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
        return string.IsNullOrEmpty(json) ? new HatchData() : JsonUtility.FromJson<HatchData>(json);
    }

    private void SaveHatchData()
    {
        PlayerPrefs.SetString("HatchData", JsonUtility.ToJson(hatchData));
        PlayerPrefs.Save();
    }

    public int GetDailyHatchCount() => hatchData.counts.Find(c => c.date == DateTime.Today.ToString("yyyy-MM-dd"))?.count ?? 0;
    public int GetWeeklyHatchCount() => SumHatchCount(DateTime.Today.AddDays(-6));
    public int GetMonthlyHatchCount() => SumHatchCount(DateTime.Today.AddMonths(-1));
    public int GetYearlyHatchCount() => SumHatchCount(DateTime.Today.AddDays(-364));

    private int SumHatchCount(DateTime startDate)
    {
        int sum = 0;
        foreach (var count in hatchData.counts)
        {
            if (DateTime.Parse(count.date) >= startDate && DateTime.Parse(count.date) <= DateTime.Today)
            {
                sum += count.count;
            }
        }
        return sum;
    }

    public void SafeUpdateHatchCountUI()
    {
        if (dailyText != null) dailyText.text = "Today: " + GetDailyHatchCount();
        if (weeklyText != null) weeklyText.text = "This Week: " + GetWeeklyHatchCount();
        if (monthlyText != null) monthlyText.text = "This Month: " + GetMonthlyHatchCount();
        if (yearlyText != null) yearlyText.text = "This Year: " + GetYearlyHatchCount();
    }

    private void UpdateGridVisibility()
    {
        DateTime today = DateTime.Today;
        List<AnimalInstance> visibleInstances = new List<AnimalInstance>();

        // First pass: determine which instances should be visible
        foreach (var instance in animalInstances)
        {
            if (instance.animalObject == null) continue;
            
            DateTime hatchDate = DateTime.Parse(instance.hatchDate);
            bool shouldBeVisible = currentView switch
            {
                "Day" => hatchDate.Date == today.Date,
                "Week" => hatchDate.Date >= today.AddDays(-6).Date && hatchDate.Date <= today.Date,
                "Month" => hatchDate.Date >= today.AddMonths(-1).Date && hatchDate.Date <= today.Date,
                "Year" => hatchDate.Date >= today.AddDays(-364).Date && hatchDate.Date <= today.Date,
                _ => true
            };
            
            if (shouldBeVisible)
            {
                visibleInstances.Add(instance);
            }
        }

        if (currentView == "All")
        {
            // Restore all instances to their original positions
            RestoreOriginalPositions();
        }
        else
        {
            // Reorganize visible instances in a filtered view
            ReorganizeFilteredView(visibleInstances);
        }

        OnGridChanged();
    }

    private void RestoreOriginalPositions()
    {
        // First deactivate all instances
        foreach (var instance in animalInstances)
        {
            if (instance.animalObject != null)
            {
                instance.animalObject.SetActive(false);
            }
        }

        // Clear and refill the grid to original size
        tilemap.ClearAllTiles();
        
        // Calculate required grid size for all instances
        int requiredSize = Mathf.CeilToInt(Mathf.Sqrt(animalInstances.Count));
        gridSize = Mathf.Max(3, requiredSize + 1); // Add some extra space
        FillGrid();

        // Restore original positions and activate all instances
        foreach (var instance in animalInstances)
        {
            if (instance.animalObject == null) continue;

            string key = GetInstanceKey(instance);
            if (originalPositions.ContainsKey(key))
            {
                Vector3Int originalPos = originalPositions[key];
                Vector3 worldPos = tilemap.GetCellCenterWorld(originalPos);
                worldPos.y += animalYOffset;
                
                instance.animalObject.transform.position = worldPos;
                instance.gridPosition = originalPos;
                
                // Update sorting order
                SpriteRenderer renderer = instance.animalObject.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.sortingOrder = -originalPos.y;
                }
            }
            
            instance.animalObject.SetActive(true);
        }
    }

    private void ReorganizeFilteredView(List<AnimalInstance> visibleInstances)
    {
        // First deactivate all instances
        foreach (var instance in animalInstances)
        {
            if (instance.animalObject != null)
            {
                instance.animalObject.SetActive(false);
            }
        }

        if (visibleInstances.Count == 0)
        {
            // If no visible instances, just clear the grid
            tilemap.ClearAllTiles();
            gridSize = 3;
            FillGrid();
            return;
        }

        // Calculate new grid size based on number of visible instances
        int requiredSize = Mathf.CeilToInt(Mathf.Sqrt(visibleInstances.Count));
        gridSize = Mathf.Max(3, requiredSize + 1); // Add some buffer space

        // Clear and refill the grid
        tilemap.ClearAllTiles();
        FillGrid();

        // Get all empty cells for placement
        List<Vector3Int> emptyCells = GetEmptyCells();
        
        // Shuffle the empty cells for random placement
        for (int i = 0; i < emptyCells.Count; i++)
        {
            Vector3Int temp = emptyCells[i];
            int randomIndex = UnityEngine.Random.Range(i, emptyCells.Count);
            emptyCells[i] = emptyCells[randomIndex];
            emptyCells[randomIndex] = temp;
        }

        // Place visible instances randomly on empty cells
        for (int i = 0; i < visibleInstances.Count && i < emptyCells.Count; i++)
        {
            var instance = visibleInstances[i];
            Vector3Int newPos = emptyCells[i];
            Vector3 worldPos = tilemap.GetCellCenterWorld(newPos);
            worldPos.y += animalYOffset;
            
            // Update the instance's position
            instance.animalObject.transform.position = worldPos;
            instance.gridPosition = newPos;

            // Update sorting order
            SpriteRenderer renderer = instance.animalObject.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = -newPos.y;
            }
            
            // Activate the instance
            instance.animalObject.SetActive(true);
        }
    }

    private void SetView(string view)
    {
        currentView = view;
        UpdateGridVisibility();
        SafeUpdateHatchCountUI();
        Debug.Log($"View changed to: {view}");
    }
}

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
            spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
        }
    }
}