using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DG.Tweening;

public class MenuManager : MonoBehaviour
{
    [Header("References")]
    public GameObject menuPanel; // The sliding menu panel
    public Button menuButton;    // Button to open the menu
    [SerializeField] RectTransform PauseMenurect;
    [SerializeField] float topPosX, middlePosX;
    [SerializeField] float tweenDuration;
    public MenuSlide menuSlide;
    public GameObject Blackout;


    void Start()
    {
        menuPanel.SetActive(false);
        menuButton.onClick.AddListener(OpenMenu);
        Blackout.SetActive(false);
    }

    void Update()
    {
        if (menuPanel.activeSelf)
        {
            if (IsTouchOrClickOutsideMenu())
            {
                CloseMenu();
            }
        }
    }

    public void OpenMenu()
    {
        menuPanel.SetActive(true);
        menuSlide.SlideIn();
        Blackout.SetActive(true);
    }

    public void CloseMenu()
    {
        Blackout.SetActive(false);
        // Call SlideOut and use OnComplete to deactivate the panel after animation finishes
        menuSlide.SlideOut().OnComplete(() => {
            menuPanel.SetActive(false);
            
        });
    }

    void PausePanelIntro()
    {
        PauseMenurect.DOAnchorPosX(middlePosX, tweenDuration);
    }
    
    void PausePanelOutro()
    {
        PauseMenurect.DOAnchorPosX(topPosX, tweenDuration);
    }
    
    public void LoadZooScene()
    {
        SceneManager.LoadScene("Zoo");
    }
    public void LoadHome(){
        SceneManager.LoadScene("Menu");    
    }

    private bool IsTouchOrClickOutsideMenu()
    {
        if (Input.touchCount > 0) // Mobile touch input
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began && !IsPointerOverUIElement(menuPanel, touch.position))
            {
                return true;
            }
        }
        else if (Input.GetMouseButtonDown(0)) // Mouse input (for testing)
        {
            if (!IsPointerOverUIElement(menuPanel, Input.mousePosition))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsPointerOverUIElement(GameObject target, Vector2 position)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current) { position = position };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject == target || result.gameObject.transform.IsChildOf(target.transform))
            {
                return true;
            }
        }
        return false;
    }
}