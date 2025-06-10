using System.Runtime.InteropServices;
using UnityEngine;

public static class iOSBackgroundState
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern bool _ShouldIgnoreNextPause();

    [DllImport("__Internal")]
    private static extern void _InitializeSleepDetection();
#else
    private static bool _ShouldIgnoreNextPause() => false;
    private static void _InitializeSleepDetection() { }
#endif

    public static bool IgnoreNextPause => _ShouldIgnoreNextPause();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void Init()
    {
        _InitializeSleepDetection();
    }
}
