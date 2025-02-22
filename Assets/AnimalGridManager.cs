using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class AnimalGridManager : MonoBehaviour
{
    [Header("Tilemap & Tiles")]
    public Tilemap tilemap;
    public TileBase grassBlockTile;
    
    [Header("Animal Prefabs")]
    public List<GameObject> animalPrefabs; // List of animal prefabs
    public Transform animalParent; // Optional: Parent object for organizing spawned animals

    private int gridSize = 3;  // Starts as a 3x3 grid
    private const string PendingAnimalKey = "PendingAnimal";

    void Start()
    {
        // Build the initial grid of grass blocks
        FillGrid();

        // Try to place the pending animal on a random free cell
        PlacePendingAnimal();
    }

    /// <summary>
    /// Fills the grid area with grass block tiles.
    /// </summary>
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

    /// <summary>
    /// Checks for a pending animal in PlayerPrefs and places it on a random free cell.
    /// </summary>
    void PlacePendingAnimal()
    {
        string pendingAnimal = PlayerPrefs.GetString(PendingAnimalKey, "");
        if (!string.IsNullOrEmpty(pendingAnimal))
        {
            // Try to place on a free cell; if none available, expand the grid and try again.
            if (!PlaceAnimalOnRandomCell(pendingAnimal))
            {
                ExpandGrid();
                FillGrid();
                PlaceAnimalOnRandomCell(pendingAnimal);
            }
            // Clear the pending animal flag.
            PlayerPrefs.DeleteKey(PendingAnimalKey);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Places the animal prefab (matched by name) on a random empty cell in the grid.
    /// Returns true if successful.
    /// </summary>
    bool PlaceAnimalOnRandomCell(string animalName)
    {
        List<Vector3Int> emptyCells = GetEmptyCells();
        if (emptyCells.Count == 0)
        {
            return false;
        }
        
        Vector3Int randomCell = emptyCells[Random.Range(0, emptyCells.Count)];
        Vector3 worldPos = tilemap.GetCellCenterWorld(randomCell);

        GameObject animalPrefab = GetAnimalPrefabByName(animalName);
        if (animalPrefab != null)
        {
            GameObject newAnimal = Instantiate(animalPrefab, worldPos, Quaternion.identity, animalParent);
            newAnimal.name = animalPrefab.name;  // Remove (Clone) from the name.
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns a list of grid positions (Vector3Int) that have only the grass block tile (no animal placed).
    /// </summary>
    List<Vector3Int> GetEmptyCells()
    {
        List<Vector3Int> emptyCells = new List<Vector3Int>();
        int halfSize = gridSize / 2;
        for (int x = -halfSize; x < (-halfSize + gridSize); x++)
        {
            for (int y = -halfSize; y < (-halfSize + gridSize); y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                // Assume cell is "empty" if only the grass tile is present.
                // (You might need a more advanced check if animals are also on the tilemap.)
                if (tilemap.GetTile(pos) == grassBlockTile)
                {
                    emptyCells.Add(pos);
                }
            }
        }
        return emptyCells;
    }

    /// <summary>
    /// Expands the grid size by one (e.g., from 3x3 to 4x4, then 5x5, etc.).
    /// </summary>
    void ExpandGrid()
    {
        gridSize += 1;
        Debug.Log("Grid expanded to: " + gridSize + "x" + gridSize);
    }

    /// <summary>
    /// Finds the animal prefab by matching the name.
    /// </summary>
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
}
