using System;
using System.Diagnostics;
using Dalamud.Logging;
using Dalamud.Utility;

namespace PullLogger.Log;

public sealed class Logger : IDisposable
{
    private readonly Configuration _configuration;
    private readonly Csv _csv;
    private readonly State _state;
    private PullLoggerConfig? _cpl;
    private PullLoggerConfig? _lastCpl;

    private bool _combatStartLogged;

    public Logger(Container container)
    {
        _state = container.Resolve<State>();
        _state.PullingEvent += OnPullingEvent;
        _configuration = container.Resolve<Configuration>();
        _csv = container.Resolve<Csv>();
        _state.PullLoggerChanged += StateOnPullLoggerChanged;

        UpdatePullLogger();
    }

    private void StateOnPullLoggerChanged(object? sender, PullLoggerConfig? e) => UpdatePullLogger();

    private void UpdatePullLogger()
    {
        // var sw = new Stopwatch();
        // sw.Start();
        var cpl = _state.CurrentPullLogger;
        _cpl = cpl;
        if (cpl == null) return;
        if (_csv.FileName != cpl.FilePath) UpdateFileName(cpl);
        if (_csv.FileName.IsNullOrEmpty())
            PluginLog.Warning($"No file name for current pull logger {cpl.TerritoryType}");
        _lastCpl = cpl;
        // PluginLog.Debug("UPL took " + sw.Elapsed);
    }


    public void Dispose()
    {
        _state.PullingEvent -= OnPullingEvent;
        _state.PullLoggerChanged -= StateOnPullLoggerChanged;
    }

    private void OnPullingEvent(object? sender, State.PullingEventArgs args)
    {
        if (_cpl == null) return;
        // var sw = new Stopwatch();
        // sw.Start();
        if (!_combatStartLogged && args.InCombat)
        {
            // We're in combat, so log the start of the combat
            if (_cpl.LogCombatStart)
                _csv.Log(new PullRecord
                {
                    EventName = PullEvent.Start,
                    ContentName = _state.CurrentTerritoryName,
                    TerritoryType = _state.CurrentTerritoryType
                });

            _combatStartLogged = true;
            // increment pull count
            _cpl.PullCount++;
            _configuration.Save();
        }

        if (!_combatStartLogged || args.InCombat) return;
        // log combat end
        if (_cpl.LogCombatEnd)
            _csv.Log(new PullRecord
            {
                EventName = PullEvent.End,
                ContentName = _state.CurrentTerritoryName,
                TerritoryType = _state.CurrentTerritoryType,
                IsClear = args.IsClear
            });

        if (_cpl.LogRecap)
            _csv.Log(new PullRecord
            {
                EventName = PullEvent.Pull,
                Time = _state.PullStart,
                Pull = _cpl.PullCount,
                Duration = _state.PullEnd - _state.PullStart,
                ContentName = _state.CurrentTerritoryName,
                TerritoryType = _state.CurrentTerritoryType,
                IsClear = args.IsClear
            });
        _cpl.LastPull = DateTime.Now;
        _configuration.Save();
        _combatStartLogged = false;
        // sw.Stop();
        // PluginLog.Debug("Logger took " + sw.Elapsed);
    }

    /**
     * does not update the current logger to allow you retconing a pull immediately after leaving the instance
     */
    public void RetCon()
    {
        var cpl = _cpl ?? _lastCpl;
        if (cpl == null) throw new RetconError("could not find relevant instance config");

        _csv.RetCon();
        cpl.PullCount -= 1;
        _configuration.Save();
    }

    private void UpdateFileName(PullLoggerConfig pl) => _csv.FileName = pl.FilePath;
}