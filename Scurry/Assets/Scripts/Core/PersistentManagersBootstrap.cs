using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scurry.Core
{
    public class PersistentManagersBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Debug.Log("[PersistentManagersBootstrap] Awake: marking persistent managers root as DontDestroyOnLoad");
            DontDestroyOnLoad(gameObject);

            Debug.Log("[PersistentManagersBootstrap] Awake: loading MainMenu scene");
            SceneManager.LoadScene("MainMenu");
        }
    }
}
