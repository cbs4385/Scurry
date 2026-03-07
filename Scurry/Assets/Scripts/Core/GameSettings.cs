using UnityEngine;
using Scurry.Interfaces;

namespace Scurry.Core
{
    public class GameSettings : MonoBehaviour, IGameSettings
    {
        private const string PREFS_KEY = "GameSettings";

        // Singleton
        private static GameSettings instance;
        public static GameSettings Instance => instance;

        // Battle Speed: 0=Normal, 1=Fast, 2=Instant
        [Header("Battle Speed")]
        [Range(0, 2)]
        [SerializeField] private int battleSpeed = 0;

        [Header("Accessibility")]
        [SerializeField] private bool colorBlindMode = false;
        [SerializeField] private int textSizeModifier = 0; // -2 to +4
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float musicVolume = 0.7f;
        [SerializeField] private float sfxVolume = 1f;

        // Speed multipliers
        private static readonly float[] SpeedMultipliers = { 1f, 0.4f, 0.01f };
        private static readonly string[] SpeedLabels = { "Normal", "Fast", "Instant" };

        // Public accessors
        public int BattleSpeed => battleSpeed;
        public string BattleSpeedLabel => SpeedLabels[Mathf.Clamp(battleSpeed, 0, 2)];
        public float BattleWaitMultiplier => SpeedMultipliers[Mathf.Clamp(battleSpeed, 0, 2)];
        public bool ColorBlindMode => colorBlindMode;
        public int TextSizeModifier => textSizeModifier;
        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            ServiceLocator.Register<IGameSettings>(this);
            Load();
            Debug.Log($"[GameSettings] Awake: speed={BattleSpeedLabel}, colorBlind={colorBlindMode}, textMod={textSizeModifier}");
        }

        public void SetBattleSpeed(int speed)
        {
            battleSpeed = Mathf.Clamp(speed, 0, 2);
            Debug.Log($"[GameSettings] SetBattleSpeed: {BattleSpeedLabel} (multiplier={BattleWaitMultiplier})");
            Save();
        }

        public void CycleBattleSpeed()
        {
            SetBattleSpeed((battleSpeed + 1) % 3);
        }

        public void SetColorBlindMode(bool enabled)
        {
            colorBlindMode = enabled;
            Debug.Log($"[GameSettings] SetColorBlindMode: {enabled}");
            Save();
        }

        public void SetTextSizeModifier(int modifier)
        {
            textSizeModifier = Mathf.Clamp(modifier, -2, 4);
            Debug.Log($"[GameSettings] SetTextSizeModifier: {textSizeModifier}");
            Save();
        }

        public void SetMasterVolume(float vol)
        {
            masterVolume = Mathf.Clamp01(vol);
            AudioListener.volume = masterVolume;
            Save();
        }

        public void SetMusicVolume(float vol)
        {
            musicVolume = Mathf.Clamp01(vol);
            Save();
        }

        public void SetSfxVolume(float vol)
        {
            sfxVolume = Mathf.Clamp01(vol);
            Save();
        }

        // --- Color-blind safe colors ---

        public Color GetNodeColor(Data.NodeType nodeType, bool visited, bool available)
        {
            if (visited) return colorBlindMode ? new Color(0.4f, 0.4f, 0.4f) : new Color(0.3f, 0.3f, 0.3f);
            if (!available) return new Color(0.2f, 0.2f, 0.2f);

            if (colorBlindMode)
            {
                // Use shapes/patterns instead of color alone — use distinct value ranges
                switch (nodeType)
                {
                    case Data.NodeType.ResourceEncounter: return new Color(0.2f, 0.4f, 0.8f); // Blue
                    case Data.NodeType.EliteEncounter: return new Color(0.8f, 0.6f, 0.0f); // Orange
                    case Data.NodeType.Boss: return new Color(0.8f, 0.2f, 0.2f); // Red
                    case Data.NodeType.Shop: return new Color(0.9f, 0.9f, 0.2f); // Yellow
                    case Data.NodeType.HealingShrine: return new Color(0.2f, 0.8f, 0.8f); // Cyan
                    case Data.NodeType.UpgradeShrine: return new Color(0.6f, 0.3f, 0.8f); // Purple
                    case Data.NodeType.CardDraft: return new Color(0.9f, 0.5f, 0.9f); // Pink
                    case Data.NodeType.Event: return new Color(0.5f, 0.5f, 0.8f); // Light blue
                    case Data.NodeType.RestSite: return new Color(0.3f, 0.7f, 0.3f); // Green
                    default: return Color.white;
                }
            }
            else
            {
                switch (nodeType)
                {
                    case Data.NodeType.ResourceEncounter: return new Color(0.3f, 0.6f, 0.3f);
                    case Data.NodeType.EliteEncounter: return new Color(0.8f, 0.5f, 0.2f);
                    case Data.NodeType.Boss: return new Color(0.8f, 0.2f, 0.2f);
                    case Data.NodeType.Shop: return new Color(0.9f, 0.8f, 0.2f);
                    case Data.NodeType.HealingShrine: return new Color(0.3f, 0.8f, 0.3f);
                    case Data.NodeType.UpgradeShrine: return new Color(0.5f, 0.3f, 0.8f);
                    case Data.NodeType.CardDraft: return new Color(0.7f, 0.4f, 0.7f);
                    case Data.NodeType.Event: return new Color(0.4f, 0.4f, 0.7f);
                    case Data.NodeType.RestSite: return new Color(0.2f, 0.6f, 0.5f);
                    default: return Color.white;
                }
            }
        }

        // --- Adjusted text size ---

        public int AdjustedFontSize(int baseFontSize)
        {
            return Mathf.Max(8, baseFontSize + textSizeModifier * 2);
        }

        // --- Persistence ---

        private void Save()
        {
            string json = JsonUtility.ToJson(new SettingsData
            {
                battleSpeed = battleSpeed,
                colorBlindMode = colorBlindMode,
                textSizeModifier = textSizeModifier,
                masterVolume = masterVolume,
                musicVolume = musicVolume,
                sfxVolume = sfxVolume
            });
            PlayerPrefs.SetString(PREFS_KEY, json);
            PlayerPrefs.Save();
        }

        private void Load()
        {
            if (PlayerPrefs.HasKey(PREFS_KEY))
            {
                var data = JsonUtility.FromJson<SettingsData>(PlayerPrefs.GetString(PREFS_KEY));
                battleSpeed = data.battleSpeed;
                colorBlindMode = data.colorBlindMode;
                textSizeModifier = data.textSizeModifier;
                masterVolume = data.masterVolume;
                musicVolume = data.musicVolume;
                sfxVolume = data.sfxVolume;
                AudioListener.volume = masterVolume;
            }
        }

        [System.Serializable]
        private class SettingsData
        {
            public int battleSpeed;
            public bool colorBlindMode;
            public int textSizeModifier;
            public float masterVolume;
            public float musicVolume;
            public float sfxVolume;
        }
    }
}
