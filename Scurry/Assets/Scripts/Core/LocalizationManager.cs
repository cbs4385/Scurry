using UnityEngine;
using Scurry.Data;

namespace Scurry.Core
{
    public class LocalizationManager : MonoBehaviour
    {
        [SerializeField] private LocalizationTableSO[] availableTables;
        [SerializeField] private string defaultLanguage = "en";

        private static LocalizationManager _instance;
        private LocalizationTableSO _activeTable;
        private LocalizationTableSO _fallbackTable;

        public static string CurrentLanguage => _instance?._activeTable?.languageCode ?? "en";

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[LocalizationManager] Awake: duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log($"[LocalizationManager] Awake: availableTables={availableTables?.Length ?? 0}, defaultLanguage={defaultLanguage}");
            InitializeTables();
        }

        private void InitializeTables()
        {
            // Find fallback (English) table
            if (availableTables != null)
            {
                foreach (var table in availableTables)
                {
                    if (table != null && table.languageCode == "en")
                    {
                        _fallbackTable = table;
                        _fallbackTable.BuildCache();
                        break;
                    }
                }
            }

            SetLanguage(defaultLanguage);
        }

        public static void SetLanguage(string languageCode)
        {
            if (_instance == null)
            {
                Debug.LogError("[LocalizationManager] SetLanguage: no instance available");
                return;
            }

            Debug.Log($"[LocalizationManager] SetLanguage: requested={languageCode}");

            if (_instance.availableTables != null)
            {
                foreach (var table in _instance.availableTables)
                {
                    if (table != null && table.languageCode == languageCode)
                    {
                        _instance._activeTable = table;
                        table.BuildCache();
                        Debug.Log($"[LocalizationManager] SetLanguage: active table set to '{table.languageName}' ({table.languageCode})");
                        return;
                    }
                }
            }

            Debug.LogWarning($"[LocalizationManager] SetLanguage: language '{languageCode}' not found, falling back to English");
            _instance._activeTable = _instance._fallbackTable;
        }

        /// <summary>
        /// Get a localized string by key. Returns the key itself if not found.
        /// </summary>
        public static string Get(string key)
        {
            if (_instance == null || _instance._activeTable == null)
            {
                Debug.LogWarning($"[LocalizationManager] Get: no active table, returning key '{key}'");
                return key;
            }

            string result = _instance._activeTable.GetString(key, null);
            if (result == null && _instance._fallbackTable != null && _instance._fallbackTable != _instance._activeTable)
            {
                result = _instance._fallbackTable.GetString(key, null);
            }

            if (result == null)
            {
                Debug.LogWarning($"[LocalizationManager] Get: key '{key}' not found in any table");
                return key;
            }

            return result;
        }

        /// <summary>
        /// Get a localized string with string.Format placeholders.
        /// Example: Loc.Format("ui.hp.label", currentHP, maxHP) → "Colony HP: 10/50"
        /// </summary>
        public static string Format(string key, params object[] args)
        {
            string template = Get(key);
            try
            {
                return string.Format(template, args);
            }
            catch (System.FormatException ex)
            {
                Debug.LogError($"[LocalizationManager] Format: failed for key='{key}', template='{template}', args={args.Length} — {ex.Message}");
                return template;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                Debug.Log("[LocalizationManager] OnDestroy: clearing static instance");
                _instance = null;
            }
        }
    }

    /// <summary>
    /// Shorthand static accessor for localization. Use Loc.Get("key") or Loc.Format("key", args).
    /// </summary>
    public static class Loc
    {
        public static string Get(string key) => LocalizationManager.Get(key);
        public static string Format(string key, params object[] args) => LocalizationManager.Format(key, args);
    }
}
