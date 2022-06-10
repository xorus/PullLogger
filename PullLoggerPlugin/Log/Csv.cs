using System;
using System.Globalization;
using System.IO;
using Dalamud.Logging;

namespace PullLogger.Log;

public class Csv : ILogBackend
{
    public string FileName { get; set; } = "";

    public void Log(PullRecord record)
    {
        if (!File.Exists(FileName))
            File.AppendAllText(FileName, "Event,Time,Pull,Duration,Clear,TerritoryType,ContentName\n");

        var time = record.Time;
        time ??= DateTime.Now;
        var strPull = record.Pull?.ToString() ?? "";
        var strDuration = record.Duration?.TotalSeconds.ToString(CultureInfo.InvariantCulture) ?? "";
        var strTerritoryType = record.TerritoryType.ToString() ?? "";
        var strContent = record.ContentName ?? "";
        var strClear = record.IsClear == true ? "0" : "1";
        var line =
            $"{record.EventName},{time:yyyy-MM-dd HH:mm:ss},{strPull},{strDuration},{strClear},{strTerritoryType},{strContent}\n";
        PluginLog.Information("CSV << " + line);
        File.AppendAllText(FileName, line);
    }
}