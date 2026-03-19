using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;
using Scurry.Interfaces;

namespace Scurry.Core
{
    public class RelicManager : MonoBehaviour, IRelicManager
    {
        private static RelicManager _instance;
        public static RelicManager Instance => _instance;

        private List<RelicDefinitionSO> activeRelics = new List<RelicDefinitionSO>();

        public IReadOnlyList<RelicDefinitionSO> ActiveRelics => activeRelics;
        public int RelicCount => activeRelics.Count;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                ServiceLocator.Register<IRelicManager>(_instance);
                Debug.Log("[RelicManager] Awake: duplicate instance — re-registered existing and destroying component only");
                Destroy(this);
                return;
            }
            _instance = this;
            if (gameObject.name == "PersistentManagers")
                DontDestroyOnLoad(gameObject);
            ServiceLocator.Register<IRelicManager>(this);
            Debug.Log("[RelicManager] Awake: initialized");
        }

        public void ClearRelics()
        {
            Debug.Log($"[RelicManager] ClearRelics: clearing {activeRelics.Count} relics");
            activeRelics.Clear();
        }

        public void AddRelic(RelicDefinitionSO relic)
        {
            if (relic == null) return;
            if (activeRelics.Contains(relic))
            {
                Debug.Log($"[RelicManager] AddRelic: already have '{relic.relicName}' — skipping");
                return;
            }
            activeRelics.Add(relic);
            Debug.Log($"[RelicManager] AddRelic: added '{relic.relicName}' (value={relic.effectValue}), total relics={activeRelics.Count}");
        }

        public bool HasRelic(string relicName)
        {
            foreach (var r in activeRelics)
            {
                if (r.relicName == relicName) return true;
            }
            return false;
        }

        public int GetRelicEffectValue(string relicName)
        {
            foreach (var r in activeRelics)
            {
                if (r.relicName == relicName)
                {
                    Debug.Log($"[RelicManager] GetRelicEffectValue: relic='{relicName}', value={r.effectValue}");
                    return r.effectValue;
                }
            }
            return 0;
        }

        public List<string> GetRelicNames()
        {
            var names = new List<string>();
            foreach (var r in activeRelics)
                names.Add(r.relicName);
            return names;
        }

        public void RestoreRelics(List<string> relicNames)
        {
            activeRelics.Clear();
            Debug.Log($"[RelicManager] RestoreRelics: restoring {relicNames.Count} relics");

#if UNITY_EDITOR
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:RelicDefinitionSO"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var relic = UnityEditor.AssetDatabase.LoadAssetAtPath<RelicDefinitionSO>(path);
                if (relic != null && relicNames.Contains(relic.relicName))
                {
                    activeRelics.Add(relic);
                    Debug.Log($"[RelicManager] RestoreRelics: restored '{relic.relicName}'");
                }
            }
#endif
            Debug.Log($"[RelicManager] RestoreRelics: restored {activeRelics.Count} of {relicNames.Count} relics");
        }

        private void OnDestroy()
        {
            if (_instance != this)
            {
                Debug.Log($"[RelicManager] OnDestroy: duplicate instance destroyed, skipping unregister (self={GetInstanceID()})");
                return;
            }
            _instance = null;
            ServiceLocator.Unregister<IRelicManager>();
            Debug.Log("[RelicManager] OnDestroy: unregistered from ServiceLocator");
        }
    }
}
