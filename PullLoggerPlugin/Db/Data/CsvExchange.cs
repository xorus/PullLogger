using System;
using System.IO;
using Dalamud.Logging;
using PullLogger.State;

namespace PullLogger.Db.Data;

public class CsvExchange
{
    private readonly Exchange _exchange;
    private readonly Database _db;
    private readonly StateData _sd;

    public CsvExchange(Container container, Exchange exchange)
    {
        _exchange = exchange;
        _db = container.Resolve<Database>();
        _sd = container.Resolve<StateData>();
    }

    public double ImportProgress { get; set; } = 0f;

    /// <summary>
    /// csv format:
    /// Event,Time,Pull,Duration,Clear,TerritoryType,ContentName
    /// </summary>
    /// <param name="file"></param>
    /// <exception cref="Exception"></exception>
    public void Import(string file)
    {
        Profiler.Start("Import " + file);
        var lines = File.ReadAllLines(file);
        var pulls = _db.Pulls;
        foreach (var line in lines)
        {
            var fields = line.Split(",");
            if (fields.Length != 7) continue;
            var eventStr = fields[0].ToLower();
            if (eventStr == "event") continue;

            DateTime time;
            try
            {
                time = DateTime.Parse(fields[1]);
            }
            catch (Exception)
            {
                PluginLog.Error($"cannot parse date {fields[1]}");
                continue;
            }

            var pull = new Pull
            {
                EventName = eventStr switch
                {
                    "start" => PullEvent.Start,
                    "end" => PullEvent.End,
                    "pull" => PullEvent.Pull,
                    "invalid" => PullEvent.InvalidPull,
                    "retcon" => PullEvent.RetconnedPull,
                    _ => throw new Exception("unknown event type " + fields[0])
                },
                Time = time,
                Character = _sd.Character ?? "<unknown>",
                TerritoryType = (ushort?)ToInt(fields[5]),
                ContentName = UnQuote(fields[6]),
                IsClear = ToBool(fields[4], false),
                RealPullNumber = ToInt(fields[2]),
                Duration = ToTimeSpan(fields[3])
            };

            if (_exchange.IsNotDuplicated(pull)) pulls.Add(pull);
        }

        _db.SaveChangesAsync();
        Profiler.Stop("Import " + file);
    }

    private static int? ToInt(string str)
    {
        if (str.Length == 0) return null;
        return int.Parse(str);
    }

    private static TimeSpan? ToTimeSpan(string str)
    {
        if (str.Length == 0) return null;
        return TimeSpan.FromSeconds(double.Parse(str));
    }

    private static bool ToBool(string str, bool def)
    {
        if (str.Length == 0) return def;
        return str switch
        {
            "0" => false,
            "1" => true,
            _ => bool.Parse(str)
        };
    }

    private static string UnQuote(string str) => str[0] == '"' ? str.Substring(1, -1) : str;
    private static string Quote(string str) => str.Contains(",") ? $"\"{str}\"" : str;
}