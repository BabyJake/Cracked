using DG.Tweening;
using UnityEngine;

public class MenuSlide : MonoBehaviour
{
    // Reference to your menu's RectTransform
    [SerializeField] private RectTransform menuPanel;
    
    // Duration of the animation in seconds
    [SerializeField] private float animationDuration = 0.5f;
    
    private void Start()
    {
        // Optional: Set initial position
        menuPanel.anchoredPosition = new Vector2(-216f, menuPanel.anchoredPosition.y);
    }
    
    // Call this to slide the menu in
    public Tween SlideIn()
    {
        return menuPanel.DOAnchorPosX(133.8f, animationDuration).SetEase(Ease.OutQuad);
    }
    
    // Call this to slide the menu out
    public Tween SlideOut()
    {
        return menuPanel.DOAnchorPosX(-216f, animationDuration).SetEase(Ease.InQuad);
    }
}