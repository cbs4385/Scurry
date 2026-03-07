using Scurry.Data;

namespace Scurry.Interfaces
{
    public interface IGameSettings
    {
        int BattleSpeed { get; }
        string BattleSpeedLabel { get; }
        float BattleWaitMultiplier { get; }
        bool ColorBlindMode { get; }
        int TextSizeModifier { get; }
        void CycleBattleSpeed();
        void SetColorBlindMode(bool enabled);
        void SetTextSizeModifier(int modifier);
        UnityEngine.Color GetNodeColor(NodeType nodeType, bool visited, bool available);
        int AdjustedFontSize(int baseFontSize);
    }
}
