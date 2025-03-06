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
        hatchData = LoadHatchData();
        FillGrid();
        string pendingAnimal = PlayerPrefs.GetString(PendingAnimalKey, "");
        if (!string.IsNullOrEmpty(pendingAnimal))
        {
            HatchAnimal(pendingAnimal);
            PlayerPrefs.DeleteKey(PendingAnimalKey);
            PlayerPrefs.Save();
        }
        UpdateHatchCountUI();
        UpdateGridVisibility();

        dailyButton.onClick.AddListener(() => SetView("Day"));
        weeklyButton.onClick.AddListener(() => SetView("Week"));
        yearlyButton.onClick.AddListener(() => SetView("Year"));
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
        bool placed = PlaceAnimalOnRandomCell(animalName, out Vector3Int placedPosition, out GameObject spawnedAnimal);
        while (!placed)
        {
            ExpandGrid();
            FillGrid();
            placed = PlaceAnimalOnRandomCell(animalName, out placedPosition, out spawnedAnimal);
        }
        string today = DateTime.Today.ToString("yyyy-MM-dd");
        animalInstances.Add(new AnimalInstance { animalObject = spawnedAnimal, gridPosition = placedPosition, hatchDate = today });
        RecordHatching();
        UpdateHatchCountUI();
        UpdateGridVisibility();
    }

    bool PlaceAnimalOnRandomCell(string animalName, out Vector3Int placedPosition, out GameObject spawnedAnimal)
    {
        placedPosition = Vector3Int.zero;
        spawnedAnimal = null;
        List<Vector3Int> emptyCells = GetEmptyCells();
        if (emptyCells.Count == 0)
        {
            return false;
        }
        
        placedPosition = emptyCells[UnityEngine.Random.Range(0, emptyCells.Count)]; // Fixed ambiguity
        Vector3 worldPos = tilemap.GetCellCenterWorld(placedPosition);

        GameObject animalPrefab = GetAnimalPrefabByName(animalName);
        if (animalPrefab != null)
        {
            spawnedAnimal = Instantiate(animalPrefab, worldPos, Quaternion.identity, animalParent);
            spawnedAnimal.name = animalPrefab.name;
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
            if (instance.gridPosition == pos && instance.animalObject.activeSelf)
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
            return JsonUtility.FromJson<HatchData>(json);
        }
        return new HatchData();
    }

    private void SaveHatchData()
    {
        string json = JsonUtility.ToJson(hatchData);
        PlayerPrefs.SetString("HatchData", json);
        PlayerPrefs.Save();
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
            DateTime date = DateTime.ParseExact(count.date, "yyyy-MM-dd", null);
            if (date >= weekAgo && date <= today)
            {
                sum += count.count;
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
            DateTime date = DateTime.ParseExact(count.date, "yyyy-MM-dd", null);
            if (date >= yearAgo && date <= today)
            {
                sum += count.count;
            }
        }
        return sum;
    }

    public void UpdateHatchCountUI()
    {
        dailyText.text = "Today: " + GetDailyHatchCount();
        weeklyText.text = "This Week: " + GetWeeklyHatchCount();
        yearlyText.text = "This Year: " + GetYearlyHatchCount();
    }

    private void UpdateGridVisibility()
    {
        DateTime today = DateTime.Today;
        foreach (var instance in animalInstances)
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
        }
    }

    private void SetView(string view)
    {
        currentView = view;
        UpdateGridVisibility();
        UpdateHatchCountUI();
    }
}