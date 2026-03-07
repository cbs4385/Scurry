using UnityEngine;
using System.Text;
using Steamworks;

namespace Scurry.Steam
{
    public class SteamManager : MonoBehaviour
    {
        private static SteamManager _instance;
        public static SteamManager Instance => _instance;
        public static bool Initialized { get; private set; }

        private const uint APP_ID = 480; // Spacewar test app — replace with real App ID before release

        private Callback<UserStatsReceived_t> userStatsReceivedCallback;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.Log("[SteamManager] Awake: duplicate instance — destroying self");
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitSteam();
        }

        private void InitSteam()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            try
            {
                if (SteamAPI.RestartAppIfNecessary(new AppId_t(APP_ID)))
                {
                    Debug.Log("[SteamManager] InitSteam: RestartAppIfNecessary returned true — Steam will relaunch the app");
                    Application.Quit();
                    return;
                }

                Initialized = SteamAPI.Init();
                if (Initialized)
                {
                    Debug.Log("[SteamManager] InitSteam: Steam API initialized successfully");
                    userStatsReceivedCallback = Callback<UserStatsReceived_t>.Create(OnUserStatsReceived);
                }
                else
                {
                    Debug.LogWarning("[SteamManager] InitSteam: Steam API failed to initialize — Steam may not be running or steam_appid.txt missing");
                }
            }
            catch (System.DllNotFoundException e)
            {
                Debug.LogWarning($"[SteamManager] InitSteam: Steam native library not found — {e.Message}");
                Initialized = false;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SteamManager] InitSteam: unexpected error — {e.Message}");
                Initialized = false;
            }
#else
            Debug.Log("[SteamManager] InitSteam: Steam not supported on this platform");
            Initialized = false;
#endif
        }

        private void OnUserStatsReceived(UserStatsReceived_t result)
        {
            if (result.m_eResult == EResult.k_EResultOK)
            {
                Debug.Log("[SteamManager] OnUserStatsReceived: stats loaded successfully");
            }
            else
            {
                Debug.LogWarning($"[SteamManager] OnUserStatsReceived: failed with result={result.m_eResult}");
            }
        }

        private void Update()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (Initialized)
                SteamAPI.RunCallbacks();
#endif
        }

        private void OnApplicationQuit()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (Initialized)
            {
                Debug.Log("[SteamManager] OnApplicationQuit: shutting down Steam API");
                SteamAPI.Shutdown();
                Initialized = false;
            }
#endif
        }

        // --- Achievement Helpers ---

        public static void UnlockAchievement(string achievementId)
        {
            if (!Initialized)
            {
                Debug.Log($"[SteamManager] UnlockAchievement: Steam not initialized — skipping '{achievementId}'");
                return;
            }

#if UNITY_EDITOR || UNITY_STANDALONE
            try
            {
                bool alreadyAchieved;
                SteamUserStats.GetAchievement(achievementId, out alreadyAchieved);
                if (alreadyAchieved)
                {
                    Debug.Log($"[SteamManager] UnlockAchievement: '{achievementId}' already unlocked on Steam");
                    return;
                }

                bool set = SteamUserStats.SetAchievement(achievementId);
                bool stored = SteamUserStats.StoreStats();
                Debug.Log($"[SteamManager] UnlockAchievement: '{achievementId}' — set={set}, stored={stored}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SteamManager] UnlockAchievement: error — {e.Message}");
            }
#endif
        }

        public static void ResetAchievement(string achievementId)
        {
            if (!Initialized) return;

#if UNITY_EDITOR || UNITY_STANDALONE
            try
            {
                SteamUserStats.ClearAchievement(achievementId);
                SteamUserStats.StoreStats();
                Debug.Log($"[SteamManager] ResetAchievement: cleared '{achievementId}'");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SteamManager] ResetAchievement: error — {e.Message}");
            }
#endif
        }

        // --- Rich Presence ---

        public static void SetPresence(string key, string value)
        {
            if (!Initialized) return;

#if UNITY_EDITOR || UNITY_STANDALONE
            try
            {
                SteamFriends.SetRichPresence(key, value);
                Debug.Log($"[SteamManager] SetPresence: {key}='{value}'");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SteamManager] SetPresence: error — {e.Message}");
            }
#endif
        }

        public static void SetRichPresenceStatus(string status)
        {
            SetPresence("steam_display", "#Status");
            SetPresence("status", status);
        }

        public static void ClearRichPresence()
        {
            if (!Initialized) return;

#if UNITY_EDITOR || UNITY_STANDALONE
            try
            {
                SteamFriends.ClearRichPresence();
                Debug.Log("[SteamManager] ClearRichPresence: cleared");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SteamManager] ClearRichPresence: error — {e.Message}");
            }
#endif
        }

        // --- Cloud Save ---

        public static bool CloudSave(string fileName, string json)
        {
            if (!Initialized)
            {
                Debug.Log($"[SteamManager] CloudSave: Steam not initialized — skipping '{fileName}'");
                return false;
            }

#if UNITY_EDITOR || UNITY_STANDALONE
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(json);
                bool success = SteamRemoteStorage.FileWrite(fileName, data, data.Length);
                Debug.Log($"[SteamManager] CloudSave: '{fileName}' ({data.Length} bytes) — success={success}");
                return success;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SteamManager] CloudSave: error — {e.Message}");
                return false;
            }
#else
            return false;
#endif
        }

        public static string CloudLoad(string fileName)
        {
            if (!Initialized)
            {
                Debug.Log($"[SteamManager] CloudLoad: Steam not initialized — skipping '{fileName}'");
                return null;
            }

#if UNITY_EDITOR || UNITY_STANDALONE
            try
            {
                if (!SteamRemoteStorage.FileExists(fileName))
                {
                    Debug.Log($"[SteamManager] CloudLoad: '{fileName}' not found in cloud");
                    return null;
                }

                int size = SteamRemoteStorage.GetFileSize(fileName);
                if (size <= 0)
                {
                    Debug.LogWarning($"[SteamManager] CloudLoad: '{fileName}' has size {size}");
                    return null;
                }

                byte[] data = new byte[size];
                int bytesRead = SteamRemoteStorage.FileRead(fileName, data, size);
                if (bytesRead != size)
                {
                    Debug.LogWarning($"[SteamManager] CloudLoad: read {bytesRead}/{size} bytes for '{fileName}'");
                    return null;
                }

                string json = Encoding.UTF8.GetString(data);
                Debug.Log($"[SteamManager] CloudLoad: '{fileName}' loaded ({data.Length} bytes)");
                return json;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SteamManager] CloudLoad: error — {e.Message}");
                return null;
            }
#else
            return null;
#endif
        }
    }
}
