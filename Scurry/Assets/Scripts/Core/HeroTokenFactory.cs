using UnityEngine;
using Scurry.Data;
using Scurry.Map;
using Scurry.Interfaces;

namespace Scurry.Core
{
    public class HeroTokenFactory : IHeroTokenFactory
    {
        public HeroToken Create(CardDefinitionSO def, int tokenId)
        {
            Debug.Log($"[HeroTokenFactory] Create: creating hero token (def={def?.cardName}, tokenId={tokenId})");
            return new HeroToken(def, tokenId);
        }
    }
}
