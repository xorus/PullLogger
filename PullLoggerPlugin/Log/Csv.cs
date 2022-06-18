using System;
using System.Globalization;
using System.IO;
using Dalamud.Logging;

namespace PullLogger.Log;

public sealed class Csv : ILogBackend
{
    public string FileName { get; set; } = "";

    public void Log(PullRecord record)
    {
        if (!File.Exists(FileName))
            File.AppendAllText(FileName, "Event,Time,Pull,Duration,Clear,TerritoryType,ContentName\n");

        var eventStr = EventToString(record.EventName);
        var time = record.Time;
        time ??= DateTime.Now;
        var strPull = record.Pull?.ToString() ?? "";
        var strDuration = record.Duration?.TotalSeconds.ToString(CultureInfo.InvariantCulture) ?? "";
        var strTerritoryType = record.TerritoryType.ToString() ?? "";
        var strContent = record.ContentName ?? "";
        var strClear = record.IsClear == true ? "1" : "0";
        var line =
            $"{eventStr},{time:yyyy-MM-dd HH:mm:ss},{strPull},{strDuration},{strClear},{strTerritoryType},{strContent}\n";
        PluginLog.Information("CSV << " + line);
        File.AppendAllText(FileName, line);
    }

    private static string EventToString(PullEvent eventName)
    {
        return eventName switch
        {
            PullEvent.Start => "start",
            PullEvent.End => "end",
            PullEvent.Pull => "pull",
            PullEvent.RetCon => "retcon",
            _ => throw new ArgumentOutOfRangeException(nameof(eventName), eventName, null)
        };
    }

    public void RetCon()
    {
        if (!File.Exists(FileName)) throw new RetconError { Reason = "file " + FileName + " does not exist" };
        try
        {
            var lines = File.ReadAllLines(FileName);
            var findText = EventToString(PullEvent.Pull) + ",";
            for (var i = lines.Length - 1; i >= 0; i--)
            {
                if (!lines[i].StartsWith(findText)) continue;
                lines[i] = EventToString(PullEvent.RetCon) + "," + lines[i][findText.Length..];
                File.WriteAllLines(FileName, lines);
                return;
            }
        }
        catch (Exception e)
        {
            throw new RetconError { Reason = e.Message };
        }

        throw new RetconError { Reason = "could not find a pull to retcon" };
    }
}
