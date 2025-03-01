using TMPro;
using UnityEngine;
using System.Collections.Generic;
using Unity.Notifications.Android;
using UnityEngine.SceneManagement;

public class SimpleTimer : MonoBehaviour
{
    public TMP_Text timerText;
    public GameObject eggPrefab; // Assign the egg prefab in the Inspector
    public GameObject Disc;
    public GameObject Disc2;
    public GameObject unlockPopup;
    public TMP_Text timerButtonLabel; 

    private float timeRemaining;
    private bool isTimerRunning;
    private bool sessionFailed = false;

    public List<GameObject> animalPrefabs; // Assign animal prefabs in Inspector
    public Transform spawnPoint; // Spawn location for the egg
    private GameObject currentEgg; // Store the instantiated egg

    public CircularTimer circularTimer;

    void Start()
    {
        sessionFailed = false;
        timerButtonLabel.text = "Start Timer";

        if (circularTimer != null)
        {
            circularTimer.currentMinutes = 0;
        }

        // Spawn the egg at the start of the game
        SpawnEgg();
    }

    void SpawnEgg()
    {
        Vector3 eggPosition = new Vector3(0f, -2.316304f, 0f); // Set your desired X, Y, Z coordinates
        if (eggPrefab != null)
        {
            currentEgg = Instantiate(eggPrefab, eggPosition, Quaternion.identity);
            Debug.Log("Egg spawned at: " + eggPosition);
        }
        else
        {
            Debug.LogError("Egg prefab is not assigned!");
        }
    }


    public void OnTimerButtonPressed()
    {
        if (!isTimerRunning)
        {
            StartTimer();
            timerButtonLabel.text = "Give Up";
        }
        else
        {
            GiveUp();
            timerButtonLabel.text = "Start Timer";
        }
    }

    public void StartTimer()
    {
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
        if (currentEgg != null)
        {
            Destroy(currentEgg);
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
            newAnimal.name = animalPrefabs[randomIndex].name;
            Debug.Log($"Spawned: {newAnimal.name}");

            UnlockAnimal(newAnimal.name);
            PlayerPrefs.SetString("PendingAnimal", newAnimal.name);
            PlayerPrefs.Save();
        }
        else
        {
            Debug.LogError("No animal prefabs assigned or spawn point missing!");
        }
        unlockPopup.SetActive(true);
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

    public void GiveUp()
    {
        isTimerRunning = false;
        timeRemaining = 0;
        if (currentEgg != null)
        {
            Destroy(currentEgg);
            Debug.Log("Egg destroyed by Give Up action.");
        }
    }

    public void AddToZoo()
    {
        SceneManager.LoadScene("Sanctuary");
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
        if (currentEgg != null) Destroy(currentEgg);
        Debug.Log("Egg destroyed due to distraction!");
    }
}
