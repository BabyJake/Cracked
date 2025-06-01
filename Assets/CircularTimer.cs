using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class CircularTimer : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("References")]
    public Image dialImage;     // The UI Image set to Filled (Radial 360)
    public TMP_Text timeText;   // The text that displays the time
    public RectTransform knob;  // The knob visual element

    [Header("Timer Settings")]
    public float maxMinutes = 60f; // Maximum time represented when the dial is full

    [Header("Knob Settings")]
    public float knobDistanceFromCenter = 0.8f; // How far from center the knob should be (0-1 range)
    public Vector2 knobOffset = Vector2.zero;   // Additional offset for fine-tuning knob position
    public float knobAngleOffset = 0f;          // Angle offset in degrees (positive = clockwise)

    [HideInInspector]
    public float currentMinutes = 0f; // Current selected time

    private RectTransform dialRect;
    private SimpleTimer simpleTimer;

    void Start()
    {
        // Get the RectTransform of the dialImage
        dialRect = dialImage.GetComponent<RectTransform>();
        
        // Get reference to SimpleTimer
        simpleTimer = Object.FindAnyObjectByType<SimpleTimer>();

        // Initialize the dial (0 fill amount means 0 minutes)
        dialImage.fillAmount = 0f;
        UpdateKnobPosition();
        UpdateTimeText();
    }

    // Called when the user touches down on the dial
    public void OnPointerDown(PointerEventData eventData)
    {
        // Only update dial if not interacting with egg
        if (simpleTimer == null || !simpleTimer.isInteractingWithEgg)
        {
            UpdateDial(eventData);
        }
    }

    // Called when the user drags on the dial
    public void OnDrag(PointerEventData eventData)
    {
        // Only update dial if not interacting with egg
        if (simpleTimer == null || !simpleTimer.isInteractingWithEgg)
        {
            UpdateDial(eventData);
        }
    }

    // Called when the user lifts their finger (optional, if you want to trigger an action)
    public void OnPointerUp(PointerEventData eventData)
    {
        // Only update dial if not interacting with egg
        if (simpleTimer == null || !simpleTimer.isInteractingWithEgg)
        {
            // You could trigger your timer to start here, for example:
            // SimpleTimerInstance.StartTimerWithMinutes(currentMinutes);
        }
    }

    // Update the dial fill amount based on the pointer position
    public void UpdateDial(PointerEventData eventData)
    {
        Vector2 localPoint;
        // Convert the pointer's screen position to a local position within the dial's RectTransform.
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dialRect, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            // Calculate the raw angle in degrees.
            // Mathf.Atan2 returns 0° along the positive X-axis (3 o'clock).
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
            
            UpdateKnobPosition();
            UpdateTimeText();
        }
    }

    // Update the knob position based on current fill amount
    public void UpdateKnobPosition()
    {
        if (knob != null)
        {
            // For Radial 360 fill, start at top (90°) and go clockwise
            // fillAmount * 360f gives the clockwise angle from 0
            float fillAngleDegrees = dialImage.fillAmount * 360f;
            // Start at 90° (top) and subtract fill angle to go clockwise from top
            float knobAngleDegrees = 90f - fillAngleDegrees + knobAngleOffset;
            if (knobAngleDegrees < 0f) knobAngleDegrees += 360f; // Keep angle positive
            
            // Convert to radians for trig functions
            float angleInRadians = knobAngleDegrees * Mathf.Deg2Rad;
            
            // Calculate radius based on the knob's parent rect size
            float radius = Mathf.Min(dialRect.rect.width, dialRect.rect.height) * 0.5f * knobDistanceFromCenter;
            
            // Calculate the position on the circle
            float x = Mathf.Cos(angleInRadians) * radius;
            float y = Mathf.Sin(angleInRadians) * radius;
            
            // Set the knob position with additional offset
            knob.anchoredPosition = new Vector2(x, y) + knobOffset;
        }
    }

    // Update the displayed time (formatted as mm:ss)
    public void UpdateTimeText()
    {
        int minutes = Mathf.FloorToInt(currentMinutes);
        int seconds = Mathf.FloorToInt((currentMinutes - minutes) * 60);
        timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}