using System;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Logging;
using Dalamud.Utility;
using PullLogger.Interface;

namespace PullLogger.Log;

public sealed class Logger : IDisposable
{
    private readonly Configuration _configuration;
    private readonly Csv _csv;
    private readonly State.StateData _stateData;
    private PullLoggerConfig? _cpl;
    private PullLoggerConfig? _lastCpl;

    private bool _combatStartLogged;
    private readonly IToaster _toaster;

    public Logger(Container container)
    {
        _stateData = container.Resolve<State.StateData>();
        _stateData.PullingEvent += OnPullingEvent;
        _configuration = container.Resolve<Configuration>();
        _csv = container.Resolve<Csv>();
        _stateData.PullLoggerChanged += StateDataOnPullLoggerChanged;
        _toaster = container.Resolve<IToaster>();

        UpdatePullLogger();
    }

    private void StateDataOnPullLoggerChanged(object? sender, PullLoggerConfig? e) => UpdatePullLogger();

    private void UpdatePullLogger()
    {
        // var sw = new Stopwatch();
        // sw.Start();
        var cpl = _stateData.CurrentPullLogger;
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
        _stateData.PullingEvent -= OnPullingEvent;
        _stateData.PullLoggerChanged -= StateDataOnPullLoggerChanged;
    }

    private void OnPullingEvent(object? sender, State.StateData.PullingEventArgs args)
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
                    ContentName = _stateData.CurrentTerritoryName,
                    TerritoryType = _stateData.CurrentTerritoryType
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
                ContentName = _stateData.CurrentTerritoryName,
                TerritoryType = _stateData.CurrentTerritoryType,
                IsClear = args.IsClear
            });

        if (_cpl.LogRecap)
        {
            var duration = _stateData.PullEnd - _stateData.PullStart;
            var valid = _cpl.AutoInvalidate <= 0 || duration.TotalSeconds > _cpl.AutoInvalidate;
            _csv.Log(new PullRecord
            {
                EventName = valid ? PullEvent.Pull : PullEvent.InvalidPull,
                Time = _stateData.PullStart,
                Pull = _cpl.PullCount,
                Duration = _stateData.PullEnd - _stateData.PullStart,
                ContentName = _stateData.CurrentTerritoryName,
                TerritoryType = _stateData.CurrentTerritoryType,
                IsClear = args.IsClear
            });
            if (!valid)
            {
                _toaster.AddNotification(
                    "The last pull lasted " + duration.TotalSeconds.ToString("N1") +
                    "s, which is below the configured threshold.", "Pull was invalidated",
                    NotificationType.Warning, 6000U);
                _cpl.PullCount--;
            }
        }

        _cpl.LastPull = DateTime.Now;
        _configuration.Save();
        _combatStartLogged = false;
        // sw.Stop();
        // PluginLog.Debug("Logger took " + sw.Elapsed);
    }

    /**
     * does not update the current logger to allow you retconing a pull immediately after leaving the instance
     */
    public void Retcon()
    {
        if (_cpl == null && _lastCpl == null) UpdatePullLogger();
        var cpl = _cpl ?? _lastCpl;
        if (cpl == null) throw new RetconError("could not find relevant instance config");

        _csv.Retcon();
        cpl.PullCount -= 1;
        _configuration.Save();
    }

    /**
     * does not update the current logger to allow you retconing a pull immediately after leaving the instance
     */
    public void UnRetcon()
    {
        if (_cpl == null && _lastCpl == null) UpdatePullLogger();
        var cpl = _cpl ?? _lastCpl;
        if (cpl == null) throw new RetconError("could not find relevant instance config");

        _csv.UnRetcon();
        cpl.PullCount += 1;
        _configuration.Save();
    }

    private void UpdateFileName(PullLoggerConfig pl) => _csv.FileName = pl.FilePath;
}