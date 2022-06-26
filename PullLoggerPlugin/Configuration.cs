using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace PullLogger;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public bool OnlyInDuty = false;
    [NonSerialized] public EventHandler? OnSave = null;

    public List<PullLoggerConfig> PullLoggers = new();
    [NonSerialized] private Action<IPluginConfiguration>? _save = null;
    // public string LoggerFilePath { get; set; } = "";
    public int Version { get; set; } = 0;

    public void Initialize(Action<IPluginConfiguration> save)
    {
        _save = save;
    }

    public void Save()
    {
        SaveNoEvent();
        OnSave?.Invoke(this, EventArgs.Empty);
    }

    public void SaveNoEvent()
    {
        _save!(this);
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
    public float AutoInvalidate { get; set; } = 0f;
}