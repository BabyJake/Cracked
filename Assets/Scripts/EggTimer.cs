using TMPro;
using UnityEngine;


public class EggTimer : MonoBehaviour
{
    public TMP_Text timerText;
    public GameObject egg; // Your egg sprite
    public TMP_InputField timerInput;
    private float timeRemaining;

    void Start()
    {
        // Get study time from previous scene (default to 25 mins if missing)
        timeRemaining = PlayerPrefs.GetFloat("StudyTime", 25f) * 60;
    }

    void Update()
    {
        timeRemaining -= Time.deltaTime;
        UpdateTimerDisplay();

        if (timeRemaining <= 0)
        {
            HatchEgg();
        }
    }

    void UpdateTimerDisplay()
    {
        int minutes = (int)(timeRemaining / 60);
        int seconds = (int)(timeRemaining % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    void HatchEgg()
    {
        Destroy(egg); // Remove the egg
        Debug.Log("Egg hatched!");
        // Add your hatching logic here
    }
}