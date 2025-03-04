using UnityEngine;
using System.Collections.Generic;
using Unity.Notifications.Android;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SimpleTimer : MonoBehaviour
{
    public TMP_Text timerText;
    public GameObject eggPrefab;
    public GameObject Disc;
    public GameObject Disc2;
    public GameObject unlockPopup;
    public TMP_Text timerButtonLabel;
    public GameObject menu;

    public GameObject giveUpPopup;
    public Button yesButton;
    public Button noButton;

    private float timeRemaining;
    private bool isTimerRunning;
    private bool sessionFailed = false;

    public List<GameObject> animalPrefabs;
    public Transform spawnPoint;
    private GameObject currentEgg;
    private GameObject currentAnimal; // New field to track the spawned animal

    public CircularTimer circularTimer;

    void Start()
    {
        sessionFailed = false;
        timerButtonLabel.text = "Start Timer";

        if (circularTimer != null)
        {
            circularTimer.currentMinutes = 0;
        }

        SpawnEgg();

        if (giveUpPopup != null) giveUpPopup.SetActive(false);
        if (yesButton != null) yesButton.onClick.AddListener(OnYesClicked);
        if (noButton != null) noButton.onClick.AddListener(OnNoClicked);
    }

    void SpawnEgg()
    {
        Vector3 eggPosition = new Vector3(0f, -2.316304f, 0f);
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
            if (giveUpPopup != null && isTimerRunning)
            {
                giveUpPopup.SetActive(true);
            }
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
        menu.SetActive(false);
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
            currentAnimal = Instantiate(animalPrefabs[randomIndex], spawnPoint.position, Quaternion.identity); // Store the reference
            currentAnimal.name = animalPrefabs[randomIndex].name;
            Debug.Log($"Spawned: {currentAnimal.name}");

            UnlockAnimal(currentAnimal.name);
            PlayerPrefs.SetString("PendingAnimal", currentAnimal.name);
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
        timerButtonLabel.text = "Start Timer";
    }

    public void ResetAll()
    {
        isTimerRunning = false;
        timeRemaining = 0;

        if (circularTimer != null)
        {
            circularTimer.currentMinutes = 0;
            circularTimer.dialImage.fillAmount = 0f;
            circularTimer.UpdateKnobPosition();
            circularTimer.UpdateTimeText();
        }

        if (currentEgg != null)
        {
            Destroy(currentEgg);
            Debug.Log("Egg destroyed by Reset action.");
        }

        if (currentAnimal != null) // Destroy the spawned animal if it exists
        {
            Destroy(currentAnimal);
            Debug.Log("Animal despawned by Reset action.");
            currentAnimal = null; // Clear the reference
        }

        SpawnEgg();

        timerButtonLabel.text = "Start Timer";
        Disc.SetActive(true);
        Disc2.SetActive(true);
        unlockPopup.SetActive(false);
        menu.SetActive(true); // Show menu again, assuming it's visible at start

        UpdateTimerDisplay();
    }

    public void OnYesClicked()
    {
        ResetAll();
        if (giveUpPopup != null) giveUpPopup.SetActive(false);
    }

    public void OnNoClicked()
    {
        if (giveUpPopup != null) giveUpPopup.SetActive(false);
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

    void OnDestroy()
    {
        if (yesButton != null) yesButton.onClick.RemoveListener(OnYesClicked);
        if (noButton != null) noButton.onClick.RemoveListener(OnNoClicked);
    }
}