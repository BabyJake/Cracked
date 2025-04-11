using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "ShopItemSO", menuName = "Scriptable Objects/New Shop Item", order = -1)]
public class ShopItemSO : ScriptableObject
{
    public string title;
    public string description;
    public int baseCost;
    public GameObject itemPrefab;
    public Image itemImage;
}