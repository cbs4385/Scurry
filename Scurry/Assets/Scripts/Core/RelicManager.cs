using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;

namespace Scurry.Core
{
    public class RelicManager : MonoBehaviour
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
                Debug.Log("[RelicManager] Awake: duplicate instance — destroying self");
                Destroy(gameObject);
                return;
            }
            _instance = this;
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
            Debug.Log($"[RelicManager] AddRelic: added '{relic.relicName}' (effect={relic.effect}, value={relic.effectValue}), total relics={activeRelics.Count}");
        }

        public bool HasRelic(string relicName)
        {
            foreach (var r in activeRelics)
            {
                if (r.relicName == relicName) return true;
            }
            return false;
        }

        public bool HasRelicEffect(RelicEffect effect)
        {
            foreach (var r in activeRelics)
            {
                if (r.effect == effect) return true;
            }
            return false;
        }

        public int GetEffectValue(RelicEffect effect)
        {
            int total = 0;
            foreach (var r in activeRelics)
            {
                if (r.effect == effect)
                    total += r.effectValue;
            }
            Debug.Log($"[RelicManager] GetEffectValue: effect={effect}, total={total}");
            return total;
        }

        public int GetCombatBonus()
        {
            return GetEffectValue(RelicEffect.BonusCombat);
        }

        public int GetMoveBonus()
        {
            return GetEffectValue(RelicEffect.BonusMove);
        }

        public int GetHPBonus()
        {
            return GetEffectValue(RelicEffect.BonusHP);
        }

        public int GetShopDiscount()
        {
            return GetEffectValue(RelicEffect.ShopDiscount);
        }

        public bool CanIgnoreFirstPatrol()
        {
            return HasRelicEffect(RelicEffect.IgnoreFirstPatrol);
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
    }
}
