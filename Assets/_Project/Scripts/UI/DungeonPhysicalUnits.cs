using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace DungeonBuilder.M0
{
    public static class DungeonPhysicalUnits
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern float DungeonLordScreenPointsScale();
#endif
        public static float PixelsPerUnit()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var resources = activity.Call<AndroidJavaObject>("getResources"))
            using (var metrics = resources.Call<AndroidJavaObject>("getDisplayMetrics"))
                return metrics.Get<float>("density");
#elif UNITY_IOS && !UNITY_EDITOR
            return DungeonLordScreenPointsScale();
#else
            return 1;
#endif
        }
        public static DungeonTargetPlatform Platform => Application.platform == RuntimePlatform.Android ?
            DungeonTargetPlatform.Android : Application.platform == RuntimePlatform.IPhonePlayer ?
            DungeonTargetPlatform.IOS : DungeonTargetPlatform.Desktop;
    }
}
