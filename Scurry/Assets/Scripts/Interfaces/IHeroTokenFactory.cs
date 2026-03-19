using Scurry.Data;
using Scurry.Map;

namespace Scurry.Interfaces
{
    public interface IHeroTokenFactory
    {
        HeroToken Create(CardDefinitionSO def, int tokenId);
    }
}
