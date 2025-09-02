using UnityEngine;

public class ScreenTimeManager : MonoBehaviour
{
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void requestAuthorization();
    
    public void RequestScreenTimePermission()
    {
        #if UNITY_IOS && !UNITY_EDITOR
            requestAuthorization();
            Debug.Log("Screen time success");
        #else
            Debug.Log("Screen Time authorization only available on iOS");
        #endif
    }
    
    void Start()
    {
        // Request permission when the app starts
        RequestScreenTimePermission();
    }
}
