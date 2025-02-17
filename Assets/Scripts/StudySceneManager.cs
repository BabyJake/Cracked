using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StudySceneManager : MonoBehaviour
{
    public TMP_InputField timerInput;
    public string eggSceneName = "EggScene";

   /* public void StartEggTimer()
    {
        if (float.TryParse(timerInput.text, out float minutes))
        {
            // Save the timer duration for the egg scene
            PlayerPrefs.SetFloat("StudyTime", minutes);
            SceneManager.LoadScene(eggSceneName);
            Debug.Log("trying");
        }
    }*/
}