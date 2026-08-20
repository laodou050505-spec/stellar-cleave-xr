using ByteDance.PICO.XR;
using UnityEngine;

namespace CosmicSlice
{
    [DefaultExecutionOrder(-10000)]
    public sealed class PicoDesktopRuntimeGate : MonoBehaviour
    {
        private void Awake()
        {
#if !UNITY_ANDROID || UNITY_EDITOR
            var managers = FindObjectsByType<PXR_Manager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var manager in managers)
            {
                manager.enabled = false;
            }
#endif
        }
    }
}
