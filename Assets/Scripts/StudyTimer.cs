using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using DG.Tweening;

public class SimpleTimer : MonoBehaviour
{
    public TMP_Text timerText;
    public TMP_Text coinText;
    public GameObject eggPrefab;
    public GameObject Disc;
    public GameObject Disc2;
    public GameObject unlockPopup;
    public GameObject deathPopup;
    public TMP_Text unlockCoinRewardText;
    public TMP_Text animalNameText;
    public TMP_Text timerButtonLabel;
    public GameObject menu;
    public GameObject eggMenu;
    private BottomMenuSlide eggMenuSlide;

    [Header("Animal Settings")]
    public float animalHatchScale = 2f; // Adjust this in the Unity Inspector

    public GameObject giveUpPopup;
    public Button yesButton;
    public Button noButton;

    private float timeRemaining;
    public bool isTimerRunning { get; private set; }
    public bool isInteractingWithEgg { get; private set; } = false;
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
    private ShopItemSO currentEggSO;

    public CircularTimer circularTimer;

    private float savedTimeRemaining;
    private float savedSessionStartTime;
    private bool wasTimerRunning;

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
        if (eggMenu != null)
        {
            eggMenuSlide = eggMenu.GetComponent<BottomMenuSlide>();
            if (eggMenuSlide == null)
            {
                Debug.LogError("BottomMenuSlide component missing on eggMenu!");
            }
        }
    }

    void Start()
    {
        //PlayerPrefs.DeleteAll();
        sessionFailed = false;
        timerButtonLabel.text = "Start Timer";

        if (circularTimer != null)
        {
            circularTimer.currentMinutes = 0;
        }

        PurchasedEggManager eggManager = FindObjectOfType<PurchasedEggManager>();
        if (eggManager != null && eggManager.defaultEggSO != null)
        {
            currentEggSO = eggManager.defaultEggSO;
            currentEggPrefab = eggManager.defaultEggSO.itemPrefab;
        }

        SpawnEgg();

        if (eggMenu != null && eggMenuSlide != null) eggMenuSlide.HideInstant();
        if (unlockPopup != null) unlockPopup.SetActive(false);
        if (giveUpPopup != null) giveUpPopup.SetActive(false);
        if (yesButton != null) yesButton.onClick.AddListener(OnYesClicked);
        if (noButton != null) noButton.onClick.AddListener(OnNoClicked);

        UpdateCoinDisplay();
        RestoreTimerState();
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
        }
        else if (eggPrefab != null)
        {
            currentEgg = Instantiate(eggPrefab, eggPosition, Quaternion.identity);
            currentEggSO = null;
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
                lastCoinsEarned = AwardCoinsForSession(true);
                HatchEgg();
                timerButtonLabel.text = "Start Timer";
            }
        }

        if (Input.GetMouseButtonDown(0) && currentEgg != null && !isTimerRunning)
        {
            Vector2 tapPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(tapPosition, Vector2.zero);

            if (hit.collider != null && hit.collider.gameObject == currentEgg && !eggMenu.activeSelf)
            {
                isInteractingWithEgg = true;
                eggMenuSlide.SlideIn();
            }
            else if (eggMenu.activeSelf && !IsTapOnMenu(tapPosition))
            {
                if (eggMenuSlide != null)
                {
                    eggMenuSlide.SlideOut().OnComplete(() =>
                    {
                        eggMenu.SetActive(false);
                        isInteractingWithEgg = false;
                    });
                }
            }
        }
    }

    int AwardCoinsForSession(bool completed)
    {
        float sessionDuration = Time.time - sessionStartTime;
        int minutes = Mathf.FloorToInt(sessionDuration / 60f);
        int coinsEarned = minutes * coinsPerMinute;
        if (completed) coinsEarned += Mathf.RoundToInt(circularTimer.currentMinutes * 2);
        coinsEarned = Mathf.Max(coinsEarned, completed ? 1 : 0);
        if (coinsEarned > 0)
        {
            TotalCoins += coinsEarned;
            UpdateCoinDisplay();
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
            SpawnRandomAnimal();
            if (eggMenuSlide != null)
            {
                eggMenuSlide.SlideOut().OnComplete(() =>
                {
                    eggMenu.SetActive(false);
                    isInteractingWithEgg = false;
                });
            }
            else
            {
                eggMenu.SetActive(false);
                isInteractingWithEgg = false;
            }
        }
    }

    void SpawnRandomAnimal()
    {
        if (currentEggSO != null && currentEggSO.animalSpawnChances.Count > 0 && spawnPoint != null)
        {
            // Calculate total spawn chance
            float totalChance = 0f;
            foreach (var animalChance in currentEggSO.animalSpawnChances)
            {
                totalChance += animalChance.spawnChance;
            }

            // Generate random value between 0 and total chance
            float randomValue = Random.Range(0f, totalChance);
            float currentChance = 0f;

            // Find the selected animal based on spawn chances
            GameObject selectedAnimal = null;
            foreach (var animalChance in currentEggSO.animalSpawnChances)
            {
                currentChance += animalChance.spawnChance;
                if (randomValue <= currentChance)
                {
                    selectedAnimal = animalChance.animalPrefab;
                    break;
                }
            }

            if (selectedAnimal != null)
            {
                currentAnimal = Instantiate(selectedAnimal, spawnPoint.position, Quaternion.identity);
                // Scale up the animal for the hatching scene using the public variable
                currentAnimal.transform.localScale = new Vector3(animalHatchScale, animalHatchScale, animalHatchScale);
                string animalName = selectedAnimal.name.Replace("(Clone)", "").Trim();
                animalNameText.text = animalName;
                unlockCoinRewardText.text = $"+{lastCoinsEarned}";
                unlockPopup.SetActive(true);
                UnlockAnimal(animalName);
                
                // Store the animal name for the grid manager
                PlayerPrefs.SetString("PendingAnimal", animalName);
                PlayerPrefs.Save();
                Debug.Log($"Spawned animal: {animalName} and stored as pending");
            }
        }
        else if (animalPrefabs.Count > 0 && spawnPoint != null)
        {
            // Fallback to random selection if no spawn chances are defined
            GameObject randomAnimal = animalPrefabs[Random.Range(0, animalPrefabs.Count)];
            currentAnimal = Instantiate(randomAnimal, spawnPoint.position, Quaternion.identity);
            // Scale up the animal for the hatching scene using the public variable
            currentAnimal.transform.localScale = new Vector3(animalHatchScale, animalHatchScale, animalHatchScale);
            string animalName = randomAnimal.name.Replace("(Clone)", "").Trim();
            animalNameText.text = animalName;
            unlockCoinRewardText.text = $"+{lastCoinsEarned}";
            unlockPopup.SetActive(true);
            UnlockAnimal(animalName);
            
            // Store the animal name for the grid manager
            PlayerPrefs.SetString("PendingAnimal", animalName);
            PlayerPrefs.Save();
            Debug.Log($"Spawned random animal: {animalName} and stored as pending");
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

        GameObject eggClone = GameObject.Find("egg_0(Clone)");
        if (eggClone != null)
        {
            eggClone.SetActive(false);
        }

        if (!hasCreatedGraveThisSession)
        {
            string graveId = AddGraveToUnlockedList();
            hasCreatedGraveThisSession = true;
        }

        if (deathPopup != null)
        {
            deathPopup.SetActive(true);
        }

        ResetToDefaultState();
        isProcessingGiveUp = false;
    }

    private void ResetToDefaultState()
    {
        timerButtonLabel.text = "Start Timer";
        eggMenu.SetActive(false);
        isInteractingWithEgg = false;
        Disc.SetActive(true);
        Disc2.SetActive(true);
        menu.SetActive(true);
        UpdateTimerDisplay();

        if (currentEgg != null)
        {
            currentEgg.SetActive(true);
        }
        else
        {
            SpawnEgg();
        }

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
        return graveId;
    }

    public void ResetAll()
    {
        isTimerRunning = false;
        timeRemaining = 0;
        isInteractingWithEgg = false;

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
        }

        if (currentAnimal != null)
        {
            Destroy(currentAnimal);
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

    private void SaveTimerState()
    {
        savedTimeRemaining = timeRemaining;
        savedSessionStartTime = sessionStartTime;
        wasTimerRunning = isTimerRunning;
        PlayerPrefs.SetFloat("SavedTimeRemaining", savedTimeRemaining);
        PlayerPrefs.SetFloat("SavedSessionStartTime", savedSessionStartTime);
        PlayerPrefs.SetInt("WasTimerRunning", wasTimerRunning ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void RestoreTimerState()
    {
        if (PlayerPrefs.HasKey("WasTimerRunning") && PlayerPrefs.GetInt("WasTimerRunning") == 1)
        {
            savedTimeRemaining = PlayerPrefs.GetFloat("SavedTimeRemaining");
            savedSessionStartTime = PlayerPrefs.GetFloat("SavedSessionStartTime");
            wasTimerRunning = true;
            
            // Calculate elapsed time while app was closed
            float elapsedTime = Time.time - savedSessionStartTime;
            timeRemaining = Mathf.Max(0, savedTimeRemaining - elapsedTime);
            
            if (timeRemaining > 0)
            {
                isTimerRunning = true;
                sessionStartTime = Time.time;
                timerButtonLabel.text = "Give Up";
            }
            else
            {
                isTimerRunning = false;
                timeRemaining = 0;
                lastCoinsEarned = AwardCoinsForSession(true);
                HatchEgg();
                timerButtonLabel.text = "Start Timer";
            }
        }
    }

    // This is called only from native iOS code when app is truly backgrounded
    public void OnAppTrueBackgrounded()
    {
        if (isTimerRunning && !isProcessingGiveUp)
        {
            SaveTimerState();
            ApplyPenalty();
        }
    }

    private void ApplyPenalty()
    {
        if (isProcessingGiveUp) return;
        isProcessingGiveUp = true;

        if (isTimerRunning)
        {
            lastCoinsEarned = AwardCoinsForSession(false);
            isTimerRunning = false;
            timeRemaining = 0;

            if (currentEgg != null)
            {
                currentEgg.SetActive(false);
                if (!hasCreatedGraveThisSession)
                {
                    string graveId = AddGraveToUnlockedList();
                    hasCreatedGraveThisSession = true;
                }
            }

            if (deathPopup != null) deathPopup.SetActive(true);
            eggMenu.SetActive(false);
            isInteractingWithEgg = false;
            ResetToDefaultState();
        }

        isProcessingGiveUp = false;
    }

    void OnDestroy()
    {
        if (yesButton != null) yesButton.onClick.RemoveListener(OnYesClicked);
        if (noButton != null) noButton.onClick.RemoveListener(OnNoClicked);
    }

    public void OK()
    {
        deathPopup.SetActive(false);
    }
}
