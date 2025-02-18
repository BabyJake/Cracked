using TMPro;
using UnityEngine;
using System.Collections.Generic;
using Unity.Notifications.Android;

public class SimpleTimer : MonoBehaviour
{
    public TMP_Text timerText;
    public GameObject egg; // Reference to the egg
    public GameObject Disc;
    public GameObject Disc2;
    
    // Reference to the button's label that will toggle between "Start Timer" and "Give Up"
    public TMP_Text timerButtonLabel; 

    private float timeRemaining;
    private bool isTimerRunning;
    private bool sessionFailed = false;

    public List<GameObject> animalPrefabs; // List of animal prefabs (Assign in Inspector)
    public Transform spawnPoint; // Spawn location for the animal

    // Reference to the radial slider script that holds the current time value.
    public CircularTimer circularTimer;  // Assign this in the Inspector

    void Start()
    {
        sessionFailed = false;
        // Set initial button text.
        timerButtonLabel.text = "Start Timer";
    
    if (circularTimer != null)
    {
        circularTimer.currentMinutes = 10;  // Ensure the circular timer starts at 1 minute
    }

    }

    // This method should be hooked up to your button's OnClick event.
    public void OnTimerButtonPressed()
    {
        if (!isTimerRunning)
        {
            // If timer is not running, start it.
            StartTimer();
            timerButtonLabel.text = "Give Up";
        }
        else
        {
            // If timer is running, the button now means "Give Up"
            GiveUp();
            timerButtonLabel.text = "Start Timer";
        }
    }

    public void StartTimer()
    {
        // Use the value from the radial slider.
        float minutes = circularTimer.currentMinutes;
        timeRemaining = minutes * 60;
        isTimerRunning = true;
        sessionFailed = false;
        Disc2.SetActive(false);
        Disc.SetActive(false);
    }

    void Update()
    {
        if (isTimerRunning && !sessionFailed)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerDisplay();

            if (timeRemaining <= 0)
            {
                isTimerRunning = false;
                timeRemaining = 0;
                Debug.Log("Timer complete! Egg hatched!");
                HatchEgg();
                // Reset the button text once the timer finishes.
                timerButtonLabel.text = "Start Timer";
            }
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
        if (egg != null)
        {
            Destroy(egg);
            Debug.Log("Egg hatched! You earned a new animal.");
            SpawnRandomAnimal();
        }
    }

    void SpawnRandomAnimal()
    {
        if (animalPrefabs.Count > 0 && spawnPoint != null)
        {
            int randomIndex = Random.Range(0, animalPrefabs.Count);
            GameObject newAnimal = Instantiate(animalPrefabs[randomIndex], spawnPoint.position, Quaternion.identity);
            
            // Rename to remove "(Clone)" from the instantiated prefab name.
            newAnimal.name = animalPrefabs[randomIndex].name;
            Debug.Log($"Spawned: {newAnimal.name}");

            // Save the unlocked animal.
            UnlockAnimal(newAnimal.name);
        }
        else
        {
            Debug.LogError("No animal prefabs assigned or spawn point missing!");
        }
    }

    void UnlockAnimal(string animalName)
    {
        string existingAnimals = PlayerPrefs.GetString("UnlockedAnimals", "");
        if (!existingAnimals.Contains(animalName))
        {
            existingAnimals += animalName + ",";
            PlayerPrefs.SetString("UnlockedAnimals", existingAnimals);
            PlayerPrefs.Save();
            Debug.Log($"Unlocked: {animalName}");
        }
        else
        {
            Debug.Log($"Animal already unlocked: {animalName}");
        }
    }

    // This method is called when the user gives up (by pressing the button while timer is running)
    public void GiveUp()
    {
        isTimerRunning = false;
        timeRemaining = 0;
        if (egg != null)
        {
            Destroy(egg);
            Debug.Log("Egg destroyed by Give Up action.");
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) ApplyPenalty();
    }

    void OnApplicationPause(bool isPaused)
    {
        if (isPaused) ApplyPenalty();
    }

    void ApplyPenalty()
    {
        isTimerRunning = false;
        timeRemaining = 0;
        if (egg != null) Destroy(egg);
        Debug.Log("Egg destroyed due to distraction!");
    }
}
