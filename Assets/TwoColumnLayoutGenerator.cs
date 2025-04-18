using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TwoColumnLayoutGenerator : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public RectTransform contentParent; // Parent with Vertical Layout Group
    public GameObject itemPrefab; // Your UI item prefab
    public int numberOfItems = 10; // How many items to create

    void Start()
    {
        GenerateLayout(numberOfItems);
    }

    void GenerateLayout(int count)
    {
        GameObject currentRow = null;
        for (int i = 0; i < count; i++)
        {
            // Create new row every 2 items
            if (i % 2 == 0)
            {
                currentRow = CreateRow();
                currentRow.transform.SetParent(contentParent, false);
            }

            GameObject newItem = Instantiate(itemPrefab);
            newItem.transform.SetParent(currentRow.transform, false);
        }
    }

    GameObject CreateRow()
    {
        GameObject row = new GameObject("Row", typeof(RectTransform));
        var rect = row.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);

        // Add Horizontal Layout Group
        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = 10;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // Add Content Size Fitter
        var fitter = row.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return row;
    }
}
