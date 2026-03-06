using System.Collections.Generic;
using Scurry.Data;

namespace Scurry.Encounter
{
    [System.Serializable]
    public class EncounterResult
    {
        public bool success;
        public bool recalled;
        public Dictionary<ResourceType, int> resourcesGathered = new Dictionary<ResourceType, int>();
        public List<CardDefinitionSO> woundedHeroes = new List<CardDefinitionSO>();
        public List<CardDefinitionSO> exhaustedHeroes = new List<CardDefinitionSO>();
        public List<CardDefinitionSO> rewardCards = new List<CardDefinitionSO>();
        public RelicDefinitionSO rewardRelic;

        public int GetResource(ResourceType type)
        {
            return resourcesGathered.TryGetValue(type, out int val) ? val : 0;
        }

        public void AddResource(ResourceType type, int amount)
        {
            if (resourcesGathered.ContainsKey(type))
                resourcesGathered[type] += amount;
            else
                resourcesGathered[type] = amount;
        }

        public override string ToString()
        {
            string resources = "";
            foreach (var kvp in resourcesGathered)
                resources += $"{kvp.Key}={kvp.Value} ";
            return $"EncounterResult(success={success}, recalled={recalled}, resources=[{resources.Trim()}], wounded={woundedHeroes.Count}, exhausted={exhaustedHeroes.Count})";
        }
    }
}
