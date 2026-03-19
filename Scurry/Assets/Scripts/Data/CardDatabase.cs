using System.Collections.Generic;
using UnityEngine;

namespace Scurry.Data
{
    /// <summary>
    /// Runtime card database that loads all card definitions from CardDatabase.json.
    /// Use CardDatabase.Instance to access cards by ID.
    /// </summary>
    public class CardDatabase
    {
        private static CardDatabase _instance;
        public static CardDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CardDatabase();
                    _instance.Load();
                }
                return _instance;
            }
        }

        private Dictionary<int, CardDefinitionSO> _cards = new Dictionary<int, CardDefinitionSO>();
        private Dictionary<int, ColonyCardDefinitionSO> _colonyCards = new Dictionary<int, ColonyCardDefinitionSO>();
        private List<CardDefinitionSO> _allCards = new List<CardDefinitionSO>();
        private List<ColonyCardDefinitionSO> _allColonyCards = new List<ColonyCardDefinitionSO>();

        /// <summary>
        /// Clears the singleton so the next Instance access reloads from JSON.
        /// Used by tests to ensure fresh data after ScriptableObject destruction.
        /// </summary>
        public static void ResetInstance()
        {
            Debug.Log("[CardDatabase] ResetInstance: clearing singleton");
            _instance = null;
        }

        public IReadOnlyDictionary<int, CardDefinitionSO> Cards => _cards;
        public IReadOnlyDictionary<int, ColonyCardDefinitionSO> ColonyCards => _colonyCards;
        public IReadOnlyList<CardDefinitionSO> AllCards => _allCards;
        public IReadOnlyList<ColonyCardDefinitionSO> AllColonyCards => _allColonyCards;

        public CardDefinitionSO GetCard(int cardId)
        {
            if (_cards.TryGetValue(cardId, out var card))
                return card;
            Debug.LogWarning($"[CardDatabase] GetCard: cardId={cardId} not found");
            return null;
        }

        public ColonyCardDefinitionSO GetColonyCard(int cardId)
        {
            if (_colonyCards.TryGetValue(cardId, out var card))
                return card;
            Debug.LogWarning($"[CardDatabase] GetColonyCard: cardId={cardId} not found");
            return null;
        }

        public List<CardDefinitionSO> GetCardsByType(CardType type)
        {
            var result = new List<CardDefinitionSO>();
            foreach (var card in _allCards)
            {
                if (card.cardType == type)
                    result.Add(card);
            }
            Debug.Log($"[CardDatabase] GetCardsByType: type={type}, found={result.Count}");
            return result;
        }

        public List<ColonyCardDefinitionSO> GetColonyCardsByTier(ColonyTier tier)
        {
            var result = new List<ColonyCardDefinitionSO>();
            foreach (var card in _allColonyCards)
            {
                if (card.colonyTier == tier)
                    result.Add(card);
            }
            Debug.Log($"[CardDatabase] GetColonyCardsByTier: tier={tier}, found={result.Count}");
            return result;
        }

        private void Load()
        {
            Debug.Log("[CardDatabase] Load: loading CardDatabase.json from Resources");
            var json = Resources.Load<TextAsset>("CardDatabase");
            if (json == null)
            {
                Debug.LogError("[CardDatabase] Load: CardDatabase.json not found in Resources!");
                return;
            }

            var db = JsonUtility.FromJson<CardDatabaseJson>(json.text);
            if (db == null)
            {
                Debug.LogError("[CardDatabase] Load: failed to parse CardDatabase.json");
                return;
            }

            LoadHeroes(db.heroes);
            LoadColony(db.colony);
            LoadEquipment(db.equipment);
            LoadTactical(db.tactical);

            Debug.Log($"[CardDatabase] Load: complete. Cards={_cards.Count}, ColonyCards={_colonyCards.Count}");
        }

        private void LoadHeroes(HeroCardJson[] heroes)
        {
            if (heroes == null) return;
            foreach (var h in heroes)
            {
                var so = ScriptableObject.CreateInstance<CardDefinitionSO>();
                so.hideFlags = HideFlags.DontUnloadUnusedAsset;
                so.cardId = h.cardId;
                so.cardName = h.cardName;
                so.localizationKey = $"card.{h.cardName.ToLower().Replace(" ", "_")}";
                so.cardType = CardType.Hero;
                so.rarity = ParseRarity(h.rarity);
                so.deckCost = h.deckCost;
                so.heroRole = ParseHeroRole(h.role);
                so.combat = h.combat;
                so.move = h.move;
                so.hp = h.hp;
                so.carry = h.carry;
                so.initiative = h.initiative;
                so.specialAbility = ParseSpecialAbility(h.specialAbility);
                so.specialAbilityDescription = h.specialAbilityDescription;
                so.artwork = LoadCardSprite(h.cardId);

                _cards[h.cardId] = so;
                _allCards.Add(so);
                Debug.Log($"[CardDatabase] LoadHeroes: loaded {h.cardName} (id={h.cardId}, combat={h.combat}, move={h.move}, hp={h.hp}, carry={h.carry}, init={h.initiative})");
            }
        }

        private void LoadColony(ColonyCardJson[] colony)
        {
            if (colony == null) return;
            foreach (var c in colony)
            {
                var so = ScriptableObject.CreateInstance<ColonyCardDefinitionSO>();
                so.hideFlags = HideFlags.DontUnloadUnusedAsset;
                so.cardId = c.cardId;
                so.cardName = c.cardName;
                so.localizationKey = $"colony.{c.cardName.ToLower().Replace(" ", "_")}";
                so.description = c.description;
                so.rarity = ParseRarity(c.rarity);
                so.deckCost = c.deckCost;
                so.colonyTier = ParseColonyTier(c.tier);
                so.colonyEffect = ParseColonyEffect(c.colonyEffect);
                so.effectValue = c.effectValue;
                so.isStarter = c.isStarter;
                so.artwork = LoadCardSprite(c.cardId);

                _colonyCards[c.cardId] = so;
                _allColonyCards.Add(so);
                Debug.Log($"[CardDatabase] LoadColony: loaded {c.cardName} (id={c.cardId}, effect={c.colonyEffect}, value={c.effectValue})");
            }
        }

        private void LoadEquipment(EquipmentGroupJson equipment)
        {
            if (equipment == null) return;
            LoadEquipmentArray(equipment.weapons);
            LoadEquipmentArray(equipment.armor);
            LoadEquipmentArray(equipment.utility);
            LoadEquipmentArray(equipment.legendary);
        }

        private void LoadEquipmentArray(EquipmentCardJson[] items)
        {
            if (items == null) return;
            foreach (var e in items)
            {
                var so = ScriptableObject.CreateInstance<CardDefinitionSO>();
                so.hideFlags = HideFlags.DontUnloadUnusedAsset;
                so.cardId = e.cardId;
                so.cardName = e.cardName;
                so.localizationKey = $"card.{e.cardName.ToLower().Replace(" ", "_")}";
                so.cardType = CardType.Equipment;
                so.rarity = ParseRarity(e.rarity);
                so.deckCost = e.deckCost;
                so.equipmentSlot = ParseEquipmentSlot(e.slot);
                so.equipmentEffectDescription = e.description;
                so.effectValue1 = e.effectValue1;
                so.effectValue2 = e.effectValue2;
                so.effectValue3 = e.effectValue3;
                so.artwork = LoadCardSprite(e.cardId);

                _cards[e.cardId] = so;
                _allCards.Add(so);
                Debug.Log($"[CardDatabase] LoadEquipment: loaded {e.cardName} (id={e.cardId}, slot={e.slot}, v1={e.effectValue1}, v2={e.effectValue2})");
            }
        }

        private void LoadTactical(TacticalGroupJson tactical)
        {
            if (tactical == null) return;
            LoadTacticalArray(tactical.combatTactics);
            LoadTacticalArray(tactical.supportTactics);
            LoadTacticalArray(tactical.powerTactics);
        }

        private void LoadTacticalArray(TacticalCardJson[] tactics)
        {
            if (tactics == null) return;
            foreach (var t in tactics)
            {
                var so = ScriptableObject.CreateInstance<CardDefinitionSO>();
                so.hideFlags = HideFlags.DontUnloadUnusedAsset;
                so.cardId = t.cardId;
                so.cardName = t.cardName;
                so.localizationKey = $"card.{t.cardName.ToLower().Replace(" ", "_")}";
                so.cardType = CardType.Tactical;
                so.rarity = ParseRarity(t.rarity);
                so.deckCost = t.deckCost;
                so.tacticalType = ParseTacticalType(t.type);
                so.tacticalEffectDescription = t.description;
                so.effectValue1 = t.effectValue1;
                so.effectValue2 = t.effectValue2;
                so.effectValue3 = t.effectValue3;
                so.artwork = LoadCardSprite(t.cardId);

                _cards[t.cardId] = so;
                _allCards.Add(so);
                Debug.Log($"[CardDatabase] LoadTactical: loaded {t.cardName} (id={t.cardId}, type={t.type}, v1={t.effectValue1})");
            }
        }

        private Sprite LoadCardSprite(int cardId)
        {
            string path = $"Card Images/{cardId:D3}";
            var sprite = Resources.Load<Sprite>(path);
            if (sprite == null)
                Debug.LogWarning($"[CardDatabase] LoadCardSprite: no sprite at Resources/{path}");
            return sprite;
        }

        private static CardRarity ParseRarity(string s)
        {
            return s switch
            {
                "Common" => CardRarity.Common,
                "Uncommon" => CardRarity.Uncommon,
                "Rare" => CardRarity.Rare,
                "Legendary" => CardRarity.Legendary,
                _ => CardRarity.Common
            };
        }

        private static HeroRole ParseHeroRole(string s)
        {
            return s switch
            {
                "Recon" => HeroRole.Recon,
                "Ranged" => HeroRole.Ranged,
                "Fast" => HeroRole.Fast,
                "Melee" => HeroRole.Melee,
                "Tank" => HeroRole.Tank,
                "Gather" => HeroRole.Gather,
                "Support" => HeroRole.Support,
                "Leader" => HeroRole.Leader,
                _ => HeroRole.Melee
            };
        }

        private static SpecialAbility ParseSpecialAbility(string s)
        {
            if (string.IsNullOrEmpty(s)) return SpecialAbility.None;
            if (System.Enum.TryParse<SpecialAbility>(s, out var result))
                return result;
            Debug.LogWarning($"[CardDatabase] ParseSpecialAbility: unknown ability '{s}'");
            return SpecialAbility.None;
        }

        private static EquipmentSlot ParseEquipmentSlot(string s)
        {
            return s switch
            {
                "Offensive" => EquipmentSlot.Offensive,
                "Defensive" => EquipmentSlot.Defensive,
                "Utility" => EquipmentSlot.Utility,
                _ => EquipmentSlot.Offensive
            };
        }

        private static ColonyTier ParseColonyTier(string s)
        {
            return s switch
            {
                "FoodStorage" => ColonyTier.FoodStorage,
                "StructureDefense" => ColonyTier.StructureDefense,
                "Advanced" => ColonyTier.Advanced,
                _ => ColonyTier.FoodStorage
            };
        }

        private static ColonyEffect ParseColonyEffect(string s)
        {
            if (System.Enum.TryParse<ColonyEffect>(s, out var result))
                return result;
            Debug.LogWarning($"[CardDatabase] ParseColonyEffect: unknown effect '{s}'");
            return ColonyEffect.FoodProduction;
        }

        private static TacticalType ParseTacticalType(string s)
        {
            return s switch
            {
                "CombatTactic" => TacticalType.CombatTactic,
                "SupportTactic" => TacticalType.SupportTactic,
                "PowerTactic" => TacticalType.PowerTactic,
                _ => TacticalType.CombatTactic
            };
        }

        // --- JSON data classes for Unity's JsonUtility ---

        [System.Serializable]
        private class CardDatabaseJson
        {
            public HeroCardJson[] heroes;
            public ColonyCardJson[] colony;
            public EquipmentGroupJson equipment;
            public TacticalGroupJson tactical;
        }

        [System.Serializable]
        private class HeroCardJson
        {
            public int cardId;
            public string cardName;
            public string role;
            public string rarity;
            public int combat;
            public int move;
            public int hp;
            public int carry;
            public int initiative;
            public int deckCost;
            public string specialAbility;
            public string specialAbilityDescription;
        }

        [System.Serializable]
        private class ColonyCardJson
        {
            public int cardId;
            public string cardName;
            public string tier;
            public string rarity;
            public int deckCost;
            public string colonyEffect;
            public int effectValue;
            public string description;
            public bool isStarter;
        }

        [System.Serializable]
        private class EquipmentGroupJson
        {
            public EquipmentCardJson[] weapons;
            public EquipmentCardJson[] armor;
            public EquipmentCardJson[] utility;
            public EquipmentCardJson[] legendary;
        }

        [System.Serializable]
        private class EquipmentCardJson
        {
            public int cardId;
            public string cardName;
            public string slot;
            public string rarity;
            public int deckCost;
            public int effectValue1;
            public int effectValue2;
            public int effectValue3;
            public string description;
        }

        [System.Serializable]
        private class TacticalGroupJson
        {
            public TacticalCardJson[] combatTactics;
            public TacticalCardJson[] supportTactics;
            public TacticalCardJson[] powerTactics;
        }

        [System.Serializable]
        private class TacticalCardJson
        {
            public int cardId;
            public string cardName;
            public string type;
            public string rarity;
            public int deckCost;
            public int effectValue1;
            public int effectValue2;
            public int effectValue3;
            public string description;
        }
    }
}
