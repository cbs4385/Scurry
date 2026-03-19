using UnityEngine;
using UnityEngine.EventSystems;

namespace Scurry.UI
{
    /// <summary>
    /// Shared UI utilities. Ensures EventSystem exists for any scene with programmatic UI.
    /// </summary>
    public static class UIHelper
    {
        /// <summary>
        /// Ensures an EventSystem exists in the scene.
        /// Without this, no Canvas UI clicks will register.
        /// </summary>
        public static void EnsureEventSystem()
        {
            // Find ALL EventSystems — destroy extras if more than one
            var allES = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            if (allES.Length > 1)
            {
                Debug.LogWarning($"[UIHelper] EnsureEventSystem: found {allES.Length} EventSystems, destroying extras");
                for (int i = 1; i < allES.Length; i++)
                    Object.Destroy(allES[i].gameObject);
                return; // At least one exists
            }

            if (allES.Length == 1) return; // Exactly one exists

            // None found — create one
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();

            // Try Input System module first, fall back to StandaloneInputModule
            var inputSystemType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemType != null)
            {
                go.AddComponent(inputSystemType);
                Debug.Log("[UIHelper] EnsureEventSystem: created EventSystem + InputSystemUIInputModule");
            }
            else
            {
                go.AddComponent<StandaloneInputModule>();
                Debug.Log("[UIHelper] EnsureEventSystem: created EventSystem + StandaloneInputModule");
            }
        }
    }
}
