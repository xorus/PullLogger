using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Logging;
using PullLogger.Db;
using PullLogger.State;

namespace PullLogger.Log;

// https://johandorper.com/log/thread-safe-file-writing-csharp
public sealed class Csv : ILogBackend
{
    private readonly Database _db;
    private readonly StateData _sd;
    public string FileName { get; set; } = "";

    public Csv(Container container)
    {
        _sd = container.Resolve<StateData>();
        _db = container.Resolve<Database>();
    }

    private static async Task LogAsync(string fileName, PullRecord record)
    {
        var sw = new Stopwatch();
        sw.Start();
        if (!File.Exists(fileName))
            await File.AppendAllTextAsync(fileName, "Event,Time,Pull,Duration,Clear,TerritoryType,ContentName\n");

        // add lock for thread safety

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
        PluginLog.Debug("CSV << " + line);
        await File.AppendAllTextAsync(fileName, line);
        PluginLog.Debug("writing took " + sw.Elapsed);
    }

    public void Log(PullRecord record)
    {
        // avoid lag caused by slow IO like writing the log to a NAS
        // TODO: handle onedrive errors
        LogAsync(FileName, record).ConfigureAwait(false);
        DbLog(record);
    }

    // will eventually replace the whole logger thing
    private void DbLog(PullRecord record)
    {
        _db.Pulls.Add(new Pull()
        {
            EventName = record.EventName,
            Duration = record.Duration,
            Character = _sd.Character ?? "<unknown>",
            Time = record.Time ?? DateTime.Now,
            ContentName = record.ContentName,
            TerritoryType = record.TerritoryType,
            RealPullNumber = record.Pull,
            IsClear = record.IsClear
        });
        _db.SaveChangesAsync().ConfigureAwait(false);
    }

    private static string EventToString(PullEvent eventName)
    {
        return eventName switch
        {
            PullEvent.Start => "start",
            PullEvent.End => "end",
            PullEvent.Pull => "pull",
            PullEvent.RetconnedPull => "retcon",
            PullEvent.InvalidPull => "invalid",
            _ => throw new ArgumentOutOfRangeException(nameof(eventName), eventName, null)
        };
    }

    private void ReplaceLastTypeBy(string find, string replace)
    {
        if (!File.Exists(FileName)) throw new RetconError { Reason = "file " + FileName + " does not exist" };
        try
        {
            var lines = File.ReadAllLines(FileName);
            var findText = find + ",";
            for (var i = lines.Length - 1; i >= 0; i--)
            {
                if (!lines[i].StartsWith(findText)) continue;
                lines[i] = replace + "," + lines[i][findText.Length..];
                File.WriteAllLines(FileName, lines);
                return;
            }
        }
        catch (Exception e)
        {
            throw new RetconError { Reason = e.Message };
        }

        throw new RetconError { Reason = "no relevant pull found" };
    }

    public void Retcon()
    {
        ReplaceLastTypeBy("pull", "retcon");
        var last = _db.Pulls.AsQueryable().Where(x => x.EventName == PullEvent.Pull).OrderByDescending(x => x.Time)
            .First();
        if (last == null) throw new RetconError { Reason = "no relevant pull found" };
        last.EventName = PullEvent.RetconnedPull;
        _db.SaveChangesAsync();
    }

    public void UnRetcon()
    {
        ReplaceLastTypeBy("retcon", "pull");

        var last = _db.Pulls.AsQueryable().Where(x => x.EventName == PullEvent.RetconnedPull)
            .OrderByDescending(x => x.Time)
            .First();
        if (last == null) throw new RetconError { Reason = "no relevant pull found" };
        last.EventName = PullEvent.Pull;
        _db.SaveChangesAsync();
    }
}