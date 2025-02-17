using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class CircularTimer : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("References")]
    public Image dialImage;     // The UI Image set to Filled (Radial 360)
    public TMP_Text timeText;   // The text that displays the time

    [Header("Settings")]
    public float maxMinutes = 60f; // Maximum time represented when the dial is full

    [HideInInspector]
    public float currentMinutes = 0f; // Current selected time

    private RectTransform dialRect;

    void Start()
    {
        // Get the RectTransform of the dialImage
        dialRect = dialImage.GetComponent<RectTransform>();

        // Initialize the dial (0 fill amount means 0 minutes)
        dialImage.fillAmount = 0f;
        UpdateTimeText();
    }

    // Called when the user touches down on the dial
    public void OnPointerDown(PointerEventData eventData)
    {
        UpdateDial(eventData);
    }

    // Called when the user drags on the dial
    public void OnDrag(PointerEventData eventData)
    {
        UpdateDial(eventData);
    }

    // Called when the user lifts their finger (optional, if you want to trigger an action)
    public void OnPointerUp(PointerEventData eventData)
    {
        // You could trigger your timer to start here, for example:
        // SimpleTimerInstance.StartTimerWithMinutes(currentMinutes);
    }

    // Update the dial fill amount based on the pointer position
private void UpdateDial(PointerEventData eventData)
{
    Vector2 localPoint;
    // Convert the pointer's screen position to a local position within the dial's RectTransform.
    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dialRect, eventData.position, eventData.pressEventCamera, out localPoint))
    {
        // Calculate the raw angle in degrees.
        // Mathf.Atan2 returns 0° along the positive X-axis (3 o’clock).
        float rawAngle = Mathf.Atan2(localPoint.y, localPoint.x) * Mathf.Rad2Deg;
        
        // Re-map the angle so that 0° is at the top (12 o'clock)
        // and the angle increases in the clockwise direction.
        // Using: fillAngle = 90 - rawAngle.
        float fillAngle = 90f - rawAngle;
        if (fillAngle < 0f)
            fillAngle += 360f; // Ensure the angle is within [0, 360)
        
        // Now we want the dial to snap to discrete increments.
        // We want values from 10 to 120 minutes, in 5 minute steps.
        // That gives 22 intervals (or 23 discrete positions: 0,1,2,...,22).
        // Each step on the circle is: 360 / 22 degrees.
        float angleStep = 360f / 22f;
        
        // Calculate which step (0 to 22) this angle corresponds to, rounding to the nearest step.
        float stepIndex = Mathf.Round(fillAngle / angleStep);
        stepIndex = Mathf.Clamp(stepIndex, 0f, 22f);
        
        // Snap the fill angle to the nearest step.
        float snappedAngle = stepIndex * angleStep;
        dialImage.fillAmount = snappedAngle / 360f;
        
        // Map the discrete step index to the timer value.
        // With step 0 = 10 minutes and each step adding 5 minutes:
        currentMinutes = 10f + stepIndex * 5f;
        
        UpdateTimeText();
    }
}



    // Update the displayed time (formatted as mm:ss)
    private void UpdateTimeText()
    {
        int minutes = Mathf.FloorToInt(currentMinutes);
        int seconds = Mathf.FloorToInt((currentMinutes - minutes) * 60);
        timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
