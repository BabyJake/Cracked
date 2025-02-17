using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class CircularDial : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("References")]
    public RectTransform dialCenter;   // The center of your dial (DialBackground)
    public TMP_Text timeText;          // Text to display the chosen time

    [Header("Settings")]
    public float maxMinutes = 60f;     // Max time for a full 360-degree rotation
    public float minMinutes = 0f;      // Minimum time
    public float currentMinutes = 20f; // Starting value

    private float currentAngle = 0f;

    void Start()
    {
        // Convert starting minutes to angle
        currentAngle = MinutesToAngle(currentMinutes);
        UpdateDial(currentAngle);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateDialFromPointer(eventData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        UpdateDialFromPointer(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Optionally trigger the start of your timer here
        // e.g., SimpleTimerInstance.StartTimerWithMinutes(currentMinutes);
    }

    private void UpdateDialFromPointer(PointerEventData eventData)
    {
        // Convert screen point to local coordinates
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dialCenter,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPos
        );

        // Compute angle in degrees (0 degrees at "3 o'clock")
        float angle = Mathf.Atan2(localPos.y, localPos.x) * Mathf.Rad2Deg;

        // Shift reference so 0 degrees is at "12 o'clock" if desired
        // By default, angle=0 is at 3 o'clock. We want 0 at 12 o'clock:
        angle = 90f - angle;

        // Keep angle in [0, 360)
        if (angle < 0) angle += 360f;

        currentAngle = angle;

        // Convert angle to minutes
        float rawMinutes = AngleToMinutes(currentAngle);
        currentMinutes = Mathf.Clamp(rawMinutes, minMinutes, maxMinutes);

        // Update dial handle & text
        float clampedAngle = MinutesToAngle(currentMinutes);
        UpdateDial(clampedAngle);
    }

    private float AngleToMinutes(float angle)
    {
        // 360 degrees -> maxMinutes
        return (angle / 360f) * maxMinutes;
    }

    private float MinutesToAngle(float minutes)
    {
        return (minutes / maxMinutes) * 360f;
    }

    private void UpdateDial(float angle)
    {
        // Rotate the handle. If this script is on DialHandle, we do:
        transform.localEulerAngles = new Vector3(0f, 0f, -angle);

        // Update the text to show mm:ss
        if (timeText != null)
        {
            int totalSeconds = Mathf.RoundToInt(currentMinutes * 60);
            int m = totalSeconds / 60;
            int s = totalSeconds % 60;
            timeText.text = $"{m:00}:{s:00}";
        }
    }
}
