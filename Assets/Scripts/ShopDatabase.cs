using UnityEngine;

[CreateAssetMenu(fileName = "ShopDatabase", menuName = "Shop/ShopDatabase")]
public class ShopDatabase : ScriptableObject
{
    public ShopItemSO[] shopItemsSO;
}