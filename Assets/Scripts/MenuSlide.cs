using DG.Tweening;
using UnityEngine;

public class MenuSlide : MonoBehaviour
{
    [SerializeField] private RectTransform menuPanel;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private float offScreenX = -216f;  // Configurable off-screen position
    [SerializeField] private float onScreenX = 133.8f;  // Configurable on-screen position

    private Tween activeTween;  // Track the current tween

    private void Start()
    {
        // Ensure the menu starts off-screen
        menuPanel.anchoredPosition = new Vector2(offScreenX, menuPanel.anchoredPosition.y);
    }

    public Tween SlideIn()
    {
        // Kill any existing tween to avoid overlap
        activeTween?.Kill();
        activeTween = menuPanel.DOAnchorPosX(onScreenX, animationDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);  // Ensures animation runs even when paused
        return activeTween;
    }

    public Tween SlideOut()
    {
        activeTween?.Kill();
        activeTween = menuPanel.DOAnchorPosX(offScreenX, animationDuration)
            .SetEase(Ease.InQuad)
            .SetUpdate(true);
        return activeTween;
    }

    // Optional: Instantly reset to off-screen position (e.g., for initialization)
    public void ResetPosition()
    {
        activeTween?.Kill();
        menuPanel.anchoredPosition = new Vector2(offScreenX, menuPanel.anchoredPosition.y);
    }
}