using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ShopItemSO", menuName = "Scriptable Objects/New Shop Item", order = -1)]
public class ShopItemSO : ScriptableObject
{
    public string title;
    public string description;
    public int baseCost;
    public GameObject itemPrefab;
    public Image itemImage;
    [System.Serializable]
    public class AnimalSpawnChance
    {
        public GameObject animalPrefab;
        [Range(0f, 100f)]
        public float spawnChance; // Percentage chance (0-100)
    }

    public List<AnimalSpawnChance> animalSpawnChances = new List<AnimalSpawnChance>();
}