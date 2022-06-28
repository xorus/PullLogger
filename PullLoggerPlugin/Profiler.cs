using System.Collections.Generic;
using System.Diagnostics;
using Dalamud.Logging;

namespace PullLogger;

public static class Profiler
{
    private static readonly Dictionary<string, Stopwatch> Stopwatches = new();

    public static void Start(string name)
    {
        if (Stopwatches.ContainsKey(name)) Stopwatches[name].Restart();
        else Stopwatches.Add(name, Stopwatch.StartNew());
    }

    public static void Stop(string name)
    {
        if (!Stopwatches.ContainsKey(name)) return;
        var sw = Stopwatches[name];
        sw.Stop();
        PluginLog.Debug($"[Profiling] {name} took {sw.ElapsedMilliseconds}ms");
    }
}