using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Dalamud.Logging;

namespace PullLogger.Log;

// https://johandorper.com/log/thread-safe-file-writing-csharp
public sealed class Csv : ILogBackend
{
    public string FileName { get; set; } = "";

    private static async Task LogAsync(string fileName, PullRecord record)
    {
        var sw = new Stopwatch();
        sw.Start();
        if (!File.Exists(fileName))
            await File.AppendAllTextAsync(fileName, "Event,Time,Pull,Duration,Clear,TerritoryType,ContentName\n");

        // add lock for thread safety

        var eventStr = EventToString(record.EventName, record.IsValid);
        var time = record.Time;
        time ??= DateTime.Now;
        var strPull = record.Pull?.ToString() ?? "";
        var strDuration = record.Duration?.TotalSeconds.ToString(CultureInfo.InvariantCulture) ?? "";
        var strTerritoryType = record.TerritoryType.ToString() ?? "";
        var strContent = record.ContentName ?? "";
        var strClear = record.IsClear == true ? "1" : "0";
        var line =
            $"{eventStr},{time:yyyy-MM-dd HH:mm:ss},{strPull},{strDuration},{strClear},{strTerritoryType},{strContent}\n";
        PluginLog.Debug("CSV << " + line);
        await File.AppendAllTextAsync(fileName, line);
        PluginLog.Debug("writing took " + sw.Elapsed);
    }

    public void Log(PullRecord record)
    {
        // avoid lag caused by slow IO like writing the log to a NAS
        LogAsync(FileName, record).ConfigureAwait(false);
    }

    private static string EventToString(PullEvent eventName, bool isValid = true)
    {
        return eventName switch
        {
            PullEvent.Start => "start",
            PullEvent.End => "end",
            PullEvent.Pull => isValid ? "pull" : "invalid",
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