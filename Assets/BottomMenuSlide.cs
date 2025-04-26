using UnityEngine;
using DG.Tweening;

public class BottomMenuSlide : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private RectTransform menuRect; // The menu's RectTransform
    [SerializeField] private float hiddenPosY = -1000f; // Position when hidden (below screen)
    [SerializeField] private float visiblePosY = 0f; // Position when visible (on screen)
    [SerializeField] private float tweenDuration = 0.5f; // Animation duration
    [SerializeField] private Ease easeType = Ease.OutQuad; // Animation easing type

    private Vector2 initialPos; // To store the initial position
    private bool isInitialized = false;

    void Awake()
    {
        // Ensure the menuRect is assigned
        if (menuRect == null)
        {
            menuRect = GetComponent<RectTransform>();
        }

        // Store the initial position
        initialPos = menuRect.anchoredPosition;
        Initialize();
    }

    // Initialize the menu to its hidden position
    private void Initialize()
    {
        if (!isInitialized)
        {
            menuRect.anchoredPosition = new Vector2(initialPos.x, hiddenPosY);
            isInitialized = true;
        }
    }

    // Slide the menu in (from bottom to visible position)
    public Tween SlideIn()
    {
        gameObject.SetActive(true);
        return menuRect.DOAnchorPosY(visiblePosY, tweenDuration)
            .SetEase(easeType)
            .SetUpdate(true); // Ensure animation runs even when game is paused
    }

    // Slide the menu out (from visible to bottom)
    public Tween SlideOut()
    {
        return menuRect.DOAnchorPosY(hiddenPosY, tweenDuration)
            .SetEase(easeType)
            .SetUpdate(true); // Ensure animation runs even when game is paused
    }

    // Optional: Call this to instantly show the menu without animation
    public void ShowInstant()
    {
        gameObject.SetActive(true);
        menuRect.anchoredPosition = new Vector2(initialPos.x, visiblePosY);
    }

    // Optional: Call this to instantly hide the menu without animation
    public void HideInstant()
    {
        menuRect.anchoredPosition = new Vector2(initialPos.x, hiddenPosY);
        gameObject.SetActive(false);
    }
}