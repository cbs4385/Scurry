using System.Collections.Generic;
using UnityEngine;

namespace Scurry.Data
{
    [System.Serializable]
    public class LocalizationEntry
    {
        public string key;
        [TextArea] public string value;
    }

    [CreateAssetMenu(fileName = "LocalizationTable", menuName = "Scurry/Localization Table")]
    public class LocalizationTableSO : ScriptableObject
    {
        public string languageCode = "en";
        public string languageName = "English";
        public List<LocalizationEntry> entries = new List<LocalizationEntry>();

        private Dictionary<string, string> _cache;

        public string GetString(string key, string fallback = "")
        {
            if (_cache == null) BuildCache();
            return _cache.TryGetValue(key, out string val) ? val : fallback;
        }

        public void BuildCache()
        {
            _cache = new Dictionary<string, string>(entries.Count);
            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.key))
                    _cache[entry.key] = entry.value;
            }
            Debug.Log($"[LocalizationTableSO] BuildCache: language={languageCode}, entries={_cache.Count}");
        }

        public void InvalidateCache()
        {
            _cache = null;
        }
    }
}
