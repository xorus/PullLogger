using System;
using Dalamud.Logging;
using Dalamud.Utility;

namespace PullLogger.Log;

public class Logger : IDisposable
{
    private readonly Configuration _configuration;
    private readonly Csv _csv;
    private readonly State _state;

    private bool _combatStartLogged;

    public Logger(Container container)
    {
        _state = container.Resolve<State>();
        _state.PullingEvent += OnPullingEvent;
        _configuration = container.Resolve<Configuration>();
        _csv = container.Resolve<Csv>();
    }

    public void Dispose()
    {
        _state.PullingEvent -= OnPullingEvent;
    }

    private void OnPullingEvent(object? sender, State.PullingEventArgs args)
    {
        PluginLog.Information("pull" + args.InCombat);
        var cpl = _state.CurrentPullLogger;
        if (cpl == null) return;
        if (_csv.FileName != cpl.FilePath) UpdateFileName(cpl);
        if (_csv.FileName.IsNullOrEmpty())
            PluginLog.Warning($"No file name for current pull logger {cpl.TerritoryType}");

        if (!_combatStartLogged && args.InCombat)
        {
            // We're in combat, so log the start of the combat
            if (cpl.LogCombatStart)
                _csv.Log(new PullRecord
                {
                    EventName = "start",
                    ContentName = _state.CurrentTerritoryName,
                    TerritoryType = _state.CurrentTerritoryType
                });

            _combatStartLogged = true;
            // increment pull count
            cpl.PullCount++;
            _configuration.Save();
        }

        if (!_combatStartLogged || args.InCombat) return;
        // log combat end
        if (cpl.LogCombatEnd)
            _csv.Log(new PullRecord
            {
                EventName = "end",
                ContentName = _state.CurrentTerritoryName,
                TerritoryType = _state.CurrentTerritoryType,
                IsClear = args.IsClear
            });

        if (cpl.LogRecap)
            _csv.Log(new PullRecord
            {
                EventName = "pull",
                Pull = cpl.PullCount,
                Duration = _state.PullEnd - _state.PullStart,
                ContentName = _state.CurrentTerritoryName,
                TerritoryType = _state.CurrentTerritoryType,
                IsClear = args.IsClear
            });

        _combatStartLogged = false;
    }

    private void UpdateFileName(PullLoggerConfig pl)
    {
        _csv.FileName = pl.FilePath;
    }
}