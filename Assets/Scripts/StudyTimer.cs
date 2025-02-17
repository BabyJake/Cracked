using TMPro;
using UnityEngine;
using System.Collections.Generic;
using Unity.Notifications.Android;

public class SimpleTimer : MonoBehaviour
{
    // Remove or disable the TMP_InputField if no longer used.
    // public TMP_InputField timerInput; 

    public TMP_Text timerText;
    public GameObject egg; // Reference to the egg
    public GameObject Disc;
    public GameObject Disc2;

    private float timeRemaining;
    private bool isTimerRunning;
    private bool sessionFailed = false;

    public List<GameObject> animalPrefabs; // List of animal prefabs (Assign in Inspector)
    public Transform spawnPoint; // Spawn location for the animal

    // NEW: Reference to the radial slider script that holds the current time value.
    public CircularTimer circularTimer;  // Assign this in the Inspector

    void Start()
    {
        sessionFailed = false;
    }

    public void StartTimer()
    {
        // Instead of reading from a TMP_InputField, use the value from the circular slider.
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
