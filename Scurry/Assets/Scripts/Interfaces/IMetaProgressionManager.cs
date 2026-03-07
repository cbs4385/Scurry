using Scurry.Data;

namespace Scurry.Interfaces
{
    public interface IMetaProgressionManager
    {
        MetaProgressionData Data { get; }
        int ScrapbookCompletion { get; }
        int BestiaryCompletion { get; }
        int Reputation { get; }
    }
}
