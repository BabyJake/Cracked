  using UnityEngine;
using System.Collections.Generic;
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
    public GameObject eggMenu;

    public GameObject giveUpPopup;
    public Button yesButton;
    public Button noButton;

    private float timeRemaining;
    private bool isTimerRunning;
    private bool sessionFailed = false;
    private bool isProcessingGiveUp = false;
    private bool hasCreatedGraveThisSession = false;

    public List<GameObject> animalPrefabs;
    public Transform spawnPoint;
    private GameObject currentEgg;
    private GameObject currentAnimal;

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
        if (eggMenu != null) eggMenu.SetActive(false);

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
        hasCreatedGraveThisSession = false;
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

        if (Input.GetMouseButtonDown(0) && currentEgg != null)
        {
            Vector2 tapPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(tapPosition, Vector2.zero);

            if (hit.collider != null && hit.collider.gameObject == currentEgg && !eggMenu.activeSelf)
            {
                eggMenu.SetActive(true);
                Debug.Log("Egg menu opened");
            }
            else if (eggMenu.activeSelf && !IsTapOnMenu(tapPosition))
            {
                eggMenu.SetActive(false);
                Debug.Log("Egg menu closed by tapping outside");
            }
        }
    }

    bool IsTapOnMenu(Vector2 tapPosition)
    {
        Vector2 screenTap = Camera.main.WorldToScreenPoint(tapPosition);
        RectTransform menuRect = eggMenu.GetComponent<RectTransform>();
        return RectTransformUtility.RectangleContainsScreenPoint(menuRect, screenTap, Camera.main);
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
            eggMenu.SetActive(false);
        }
    }

    void SpawnRandomAnimal()
    {
        if (animalPrefabs.Count > 0 && spawnPoint != null)
        {
            int randomIndex = Random.Range(0, animalPrefabs.Count);
            currentAnimal = Instantiate(animalPrefabs[randomIndex], spawnPoint.position, Quaternion.identity);
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
        if (isProcessingGiveUp) return;
        isProcessingGiveUp = true;

        isTimerRunning = false;
        timeRemaining = 0;

        if (currentEgg != null)
        {
            Destroy(currentEgg);
            Debug.Log("Egg destroyed by Give Up action.");

            if (!hasCreatedGraveThisSession)
            {
                string graveId = AddGraveToUnlockedList();
                Debug.Log($"Created grave with ID {graveId}");
                hasCreatedGraveThisSession = true;
            }
        }

        ResetToDefaultState();
        isProcessingGiveUp = false;
    }

    private void ResetToDefaultState()
    {
        timerButtonLabel.text = "Start Timer";
        eggMenu.SetActive(false);
        Disc.SetActive(true);
        Disc2.SetActive(true);
        menu.SetActive(true);
        UpdateTimerDisplay();
        SpawnEgg();

        if (circularTimer != null)
        {
            circularTimer.currentMinutes = 0;
            circularTimer.dialImage.fillAmount = 0f;
            circularTimer.UpdateKnobPosition();
            circularTimer.UpdateTimeText();
        }
    }

    string AddGraveToUnlockedList()
    {
        string graveId = "Grave_" + System.DateTime.Now.Ticks;
        
        string existingGraves = PlayerPrefs.GetString("UnlockedGraves", "");
        existingGraves += string.IsNullOrEmpty(existingGraves) ? graveId : "," + graveId;
        PlayerPrefs.SetString("UnlockedGraves", existingGraves);
        
        PlayerPrefs.SetString(graveId + "_date", System.DateTime.Today.ToString("yyyy-MM-dd"));
        PlayerPrefs.Save();
        
        Debug.Log($"Added grave with ID {graveId} to persistent storage");
        return graveId;
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

        if (currentAnimal != null)
        {
            Destroy(currentAnimal);
            Debug.Log("Animal despawned by Reset action.");
            currentAnimal = null;
        }

        SpawnEgg();

        timerButtonLabel.text = "Start Timer";
        Disc.SetActive(true);
        Disc2.SetActive(true);
        unlockPopup.SetActive(false);
        menu.SetActive(true);
        eggMenu.SetActive(false);

        UpdateTimerDisplay();
    }

    public void OnYesClicked()
    {
        GiveUp();
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
        if (!hasFocus && isTimerRunning && !isProcessingGiveUp)
        {
            ApplyPenalty();
        }
    }

    void OnApplicationPause(bool isPaused)
    {
        if (isPaused && isTimerRunning && !isProcessingGiveUp)
        {
            ApplyPenalty();
        }
    }

    void ApplyPenalty()
    {
        if (isProcessingGiveUp) return;
        isProcessingGiveUp = true;

        isTimerRunning = false;
        timeRemaining = 0;

        if (currentEgg != null)
        {
            Destroy(currentEgg);
            Debug.Log("Egg destroyed due to distraction.");

            if (!hasCreatedGraveThisSession)
            {
                string graveId = AddGraveToUnlockedList();
                Debug.Log($"Created penalty grave with ID {graveId}");
                hasCreatedGraveThisSession = true;
            }
        }
        eggMenu.SetActive(false);

        ResetToDefaultState();
        isProcessingGiveUp = false;
    }

    void OnDestroy()
    {
        if (yesButton != null) yesButton.onClick.RemoveListener(OnYesClicked);
        if (noButton != null) noButton.onClick.RemoveListener(OnNoClicked);
    }
}