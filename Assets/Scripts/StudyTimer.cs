using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Linq;
public class SimpleTimer : MonoBehaviour
{
    public TMP_Text timerText;
    public TMP_Text coinText;
    public GameObject eggPrefab;
    public GameObject Disc;
    public GameObject Disc2;
    public GameObject unlockPopup;
    public TMP_Text unlockCoinRewardText;
    public TMP_Text animalNameText;
    public TMP_Text timerButtonLabel;
    public GameObject menu;
    public GameObject eggMenu;
    
    public GameObject giveUpPopup;
    public Button yesButton;
    public Button noButton;

    private float timeRemaining;
    public bool isTimerRunning { get; private set; }
    private bool sessionFailed = false;
    private bool isProcessingGiveUp = false;
    private bool hasCreatedGraveThisSession = false;
    private float sessionStartTime;
    private int coinsPerMinute = 5;
    private int lastCoinsEarned = 0;

    public List<GameObject> animalPrefabs;
    public Transform spawnPoint;
    private GameObject currentEgg;
    private GameObject currentAnimal;
    private GameObject currentEggPrefab;
    private ShopItemSO currentEggSO; // Reference to current egg's ShopItemSO

    public CircularTimer circularTimer;

    public static int TotalCoins
    {
        get { return PlayerPrefs.GetInt("TotalCoins", 0); }
        set
        {
            PlayerPrefs.SetInt("TotalCoins", value);
            PlayerPrefs.Save();
        }
    }

    void Awake()
    {
        currentEggPrefab = eggPrefab;
    }

    void Start()
    {
        sessionFailed = false;
        timerButtonLabel.text = "Start Timer";

        if (circularTimer != null)
        {
            circularTimer.currentMinutes = 0;
        }

        // Initialize default egg
        PurchasedEggManager eggManager = FindObjectOfType<PurchasedEggManager>();
        if (eggManager != null && eggManager.defaultEggSO != null)
        {
            currentEggSO = eggManager.defaultEggSO;
            currentEggPrefab = eggManager.defaultEggSO.itemPrefab;
        }

        SpawnEgg();
        if (eggMenu != null) eggMenu.SetActive(false);
        if (unlockPopup != null) unlockPopup.SetActive(false);

        if (giveUpPopup != null) giveUpPopup.SetActive(false);
        if (yesButton != null) yesButton.onClick.AddListener(OnYesClicked);
        if (noButton != null) noButton.onClick.AddListener(OnNoClicked);

        UpdateCoinDisplay();
    }

    public void ChangeEggPrefab(GameObject newEggPrefab, ShopItemSO eggSO)
    {
        if (newEggPrefab != null && eggSO != null)
        {
            currentEggPrefab = newEggPrefab;
            currentEggSO = eggSO;
            Debug.Log($"Changed egg to {eggSO.title} with prefab {newEggPrefab.name}");
        }
        else
        {
            Debug.LogError("Attempted to set null egg prefab or ShopItemSO!");
        }
    }

    public void RespawnCurrentEgg()
    {
        if (!isTimerRunning && currentEgg != null)
        {
            Destroy(currentEgg);
            SpawnEgg();
        }
    }

    void SpawnEgg()
    {
        Vector3 eggPosition = new Vector3(0f, -2.316304f, 0f);
        if (currentEggPrefab != null)
        {
            currentEgg = Instantiate(currentEggPrefab, eggPosition, Quaternion.identity);
            Debug.Log($"Egg spawned at: {eggPosition} using prefab: {currentEggPrefab.name}");
        }
        else if (eggPrefab != null)
        {
            currentEgg = Instantiate(eggPrefab, eggPosition, Quaternion.identity);
            currentEggSO = null;
            Debug.Log($"Egg spawned using default prefab at: {eggPosition}");
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
        sessionStartTime = Time.time;
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
                lastCoinsEarned = AwardCoinsForSession(true);
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

    int AwardCoinsForSession(bool completed)
    {
        float sessionDuration = Time.time - sessionStartTime;
        int minutes = Mathf.FloorToInt(sessionDuration / 60f);
        int coinsEarned = minutes * coinsPerMinute;
        if (completed)
        {
            coinsEarned += Mathf.RoundToInt(circularTimer.currentMinutes * 2);
        }
        coinsEarned = Mathf.Max(coinsEarned, completed ? 1 : 0);
        if (coinsEarned > 0)
        {
            TotalCoins += coinsEarned;
            UpdateCoinDisplay();
            Debug.Log($"Awarded {coinsEarned} coins. Total: {TotalCoins}");
        }
        return coinsEarned;
    }

    void UpdateCoinDisplay()
    {
        if (coinText != null)
        {
            coinText.text = $"Coins: {TotalCoins}";
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
        if (currentEggSO != null && currentEggSO.animalSpawnChances.Count > 0)
        {
            float totalChance = currentEggSO.animalSpawnChances.Sum(x => x.spawnChance);
            float randomValue = Random.Range(0f, totalChance);
            float cumulative = 0f;

            foreach (var spawnChance in currentEggSO.animalSpawnChances)
            {
                cumulative += spawnChance.spawnChance;
                if (randomValue <= cumulative)
                {
                    if (spawnChance.animalPrefab != null)
                    {
                        currentAnimal = Instantiate(spawnChance.animalPrefab, spawnPoint.position, Quaternion.identity);
                        currentAnimal.name = spawnChance.animalPrefab.name;
                        Debug.Log($"Spawned: {currentAnimal.name}");

                        UnlockAnimal(currentAnimal.name);
                        PlayerPrefs.SetString("PendingAnimal", currentAnimal.name);
                        PlayerPrefs.Save();

                        if (animalNameText != null)
                        {
                            animalNameText.text = $"You unlocked: {currentAnimal.name}!";
                        }
                        if (unlockCoinRewardText != null && lastCoinsEarned > 0)
                        {
                            unlockCoinRewardText.text = $"You earned {lastCoinsEarned} coins!";
                            unlockCoinRewardText.gameObject.SetActive(true);
                        }
                        else if (unlockCoinRewardText != null)
                        {
                            unlockCoinRewardText.gameObject.SetActive(false);
                        }
                        unlockPopup.SetActive(true);
                        return;
                    }
                    else
                    {
                        Debug.LogWarning("Animal prefab is null in spawn chances!");
                    }
                }
            }
        }
        else
        {
            if (animalPrefabs.Count > 0 && spawnPoint != null)
            {
                int randomIndex = Random.Range(0, animalPrefabs.Count);
                currentAnimal = Instantiate(animalPrefabs[randomIndex], spawnPoint.position, Quaternion.identity);
                currentAnimal.name = animalPrefabs[randomIndex].name;
                Debug.Log($"Spawned (fallback): {currentAnimal.name}");

                UnlockAnimal(currentAnimal.name);
                PlayerPrefs.SetString("PendingAnimal", currentAnimal.name);
                PlayerPrefs.Save();

                if (animalNameText != null)
                {
                    animalNameText.text = $"You unlocked: {currentAnimal.name}!";
                }
                if (unlockCoinRewardText != null && lastCoinsEarned > 0)
                {
                    unlockCoinRewardText.text = $"You earned {lastCoinsEarned} coins!";
                    unlockCoinRewardText.gameObject.SetActive(true);
                }
                else if (unlockCoinRewardText != null)
                {
                    unlockCoinRewardText.gameObject.SetActive(false);
                }
                unlockPopup.SetActive(true);
            }
            else
            {
                Debug.LogError("No animal prefabs assigned or spawn point missing!");
            }
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

    public void GiveUp()
    {
        if (isProcessingGiveUp) return;
        isProcessingGiveUp = true;

        if (isTimerRunning)
        {
            lastCoinsEarned = AwardCoinsForSession(false);
        }

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

    public static bool SpendCoins(int amount)
    {
        if (TotalCoins >= amount)
        {
            TotalCoins -= amount;
            return true;
        }
        return false;
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

        if (isTimerRunning)
        {
            lastCoinsEarned = AwardCoinsForSession(false);
        }

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