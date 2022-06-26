using PullLogger.Interface;

namespace Standalone.Mocks;

public class TerritoryResolver : ITerritoryResolver
{
    public string Name(ushort territoryType)
    {
        return territoryType.ToString();
    }
}