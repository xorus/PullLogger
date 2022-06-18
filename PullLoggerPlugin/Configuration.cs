using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace PullLogger;

[Serializable]
public class Configuration : IPluginConfiguration
{
    [NonSerialized] private DalamudPluginInterface? _pluginInterface;
    public bool OnlyInDuty = false;
    [NonSerialized] public EventHandler? OnSave = null;

    public List<PullLoggerConfig> PullLoggers = new();
    public string LoggerFilePath { get; set; } = "";
    public int Version { get; set; } = 0;

    public void Initialize(DalamudPluginInterface pluginInterface)
    {
        _pluginInterface = pluginInterface;
    }

    public void Save()
    {
        SaveNoEvent();
        OnSave?.Invoke(this, EventArgs.Empty);
    }

    public void SaveNoEvent()
    {
        _pluginInterface!.SavePluginConfig(this);
    }
}

[Serializable]
public class PullLoggerConfig
{
    public bool Enabled { get; set; } = true;
    public bool Visible { get; set; } = true;
    public ushort TerritoryType { get; set; } = 0;
    public bool LogCombatStart { get; set; } = false;
    public bool LogCombatEnd { get; set; } = false;
    public bool LogRecap { set; get; } = true;
    public int PullCount { get; set; } = 0;
    public string FilePath { get; set; } = "";
    public DateTime? LastPull { get; set; } = null;
}