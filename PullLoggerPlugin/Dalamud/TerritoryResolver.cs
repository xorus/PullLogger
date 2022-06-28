using System.Collections.Generic;
using Dalamud.Data;
using Dalamud.Logging;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using PullLogger.Interface;

namespace PullLogger.Dalamud;

public class TerritoryResolver : ITerritoryResolver
{
    private readonly ExcelSheet<TerritoryType>? _territoryTypeSheet;

    public TerritoryResolver(Container container)
    {
        _territoryTypeSheet = container.Resolve<DataManager>().GetExcelSheet<TerritoryType>();
    }

    public string Name(ushort territoryType)
    {
        var row = _territoryTypeSheet?.GetRow(territoryType);
        if (row == null) return "<unknown territory>";

        var contentFinderName = row.ContentFinderCondition.Value?.Name;
        if (contentFinderName != null && contentFinderName != "") return contentFinderName;

        var placeName = row.PlaceName.Value?.Name;
        if (placeName != null && placeName != "") return placeName;
        return "<unknown place name>";
    }

    public void SaveAll()
    {
        if (_territoryTypeSheet is null) return;
        var found = new Dictionary<ushort, string>();
        foreach (var row in _territoryTypeSheet)
        {
            var contentFinderName = row.ContentFinderCondition.Value?.Name;
            if (contentFinderName != null && contentFinderName != "")
            {
                found.Add((ushort)row.RowId, contentFinderName);
                PluginLog.Information((ushort)row.RowId + " - " + contentFinderName);
            }
        }
    }
}