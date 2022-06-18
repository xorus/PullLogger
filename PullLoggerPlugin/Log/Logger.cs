using System;
using Dalamud.Logging;
using Dalamud.Utility;

namespace PullLogger.Log;

public sealed class Logger : IDisposable
{
    private readonly Configuration _configuration;
    private readonly Csv _csv;
    private readonly State _state;
    private PullLoggerConfig? _lastPullLogger;

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

    private PullLoggerConfig? UpdatePullLogger()
    {
        var cpl = _state.CurrentPullLogger;
        _lastPullLogger = cpl;
        if (cpl == null) return null;
        if (_csv.FileName != cpl.FilePath) UpdateFileName(cpl);
        if (_csv.FileName.IsNullOrEmpty())
            PluginLog.Warning($"No file name for current pull logger {cpl.TerritoryType}");

        return cpl;
    }

    private void OnPullingEvent(object? sender, State.PullingEventArgs args)
    {
        PluginLog.Information("pull" + args.InCombat);
        var cpl = UpdatePullLogger();
        if (cpl == null) return;

        if (!_combatStartLogged && args.InCombat)
        {
            // We're in combat, so log the start of the combat
            if (cpl.LogCombatStart)
                _csv.Log(new PullRecord
                {
                    EventName = PullEvent.Start,
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
                EventName = PullEvent.End,
                ContentName = _state.CurrentTerritoryName,
                TerritoryType = _state.CurrentTerritoryType,
                IsClear = args.IsClear
            });

        if (cpl.LogRecap)
            _csv.Log(new PullRecord
            {
                EventName = PullEvent.Pull,
                Time = _state.PullStart,
                Pull = cpl.PullCount,
                Duration = _state.PullEnd - _state.PullStart,
                ContentName = _state.CurrentTerritoryName,
                TerritoryType = _state.CurrentTerritoryType,
                IsClear = args.IsClear
            });

        _combatStartLogged = false;
    }

    /**
     * does not update the current logger to allow you retconing a pull immediately after leaving the instance
     */
    public void RetCon()
    {
        if (_lastPullLogger == null && UpdatePullLogger() == null)
        {
            throw new RetconError("could not find relevant instance config");
        }

        if (_lastPullLogger == null) return; // should not be null at this point

        _csv.RetCon();
        _lastPullLogger.PullCount -= 1;
        _configuration.Save();
    }

    private void UpdateFileName(PullLoggerConfig pl) => _csv.FileName = pl.FilePath;
}