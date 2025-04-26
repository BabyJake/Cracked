using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BlockEvents : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        // Block event from propagating
        eventData.Use();
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Block event from propagating
        eventData.Use();
    }
}