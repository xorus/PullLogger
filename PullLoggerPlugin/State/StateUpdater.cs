using System;
using System.Diagnostics;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Logging;
using PullLogger.Dalamud;
using PullLogger.Events;
using PullLogger.Interface;

namespace PullLogger.State;

public class StateUpdater : IDisposable
{
    private readonly Condition _condition;
    private readonly Configuration _configuration;
    private readonly Container _container;
    private readonly EndViaDirectorUpdateHook _duh;
    private readonly PartyList _partyList;
    private readonly ITerritoryResolver _territoryResolver;

    private readonly StateData _state;
    private readonly ClientState _clientState;

    public StateUpdater(Container container)
    {
        _state = container.Resolve<StateData>();
        _container = container;
        _partyList = container.Resolve<PartyList>();
        _container.Resolve<Framework>().Update += OnUpdate;
        _configuration = container.Resolve<Configuration>();
        _territoryResolver = container.Resolve<ITerritoryResolver>();
        _condition = container.Resolve<Condition>();
        _clientState = container.Resolve<ClientState>();

        // condition
        var condition = container.Resolve<Condition>();
        _state.Pulling = condition[ConditionFlag.InCombat];
        if (_state.Pulling) EnterCombat();
        condition.ConditionChange += ConditionOnConditionChange;

        // end via director
        _duh = container.Resolve<EndViaDirectorUpdateHook>();
        _duh.EndEvent += DirectorEndEvent;

        // clientState
        _clientState.TerritoryChanged += ClientStateOnTerritoryChanged;
        ClientStateOnTerritoryChanged(this, _clientState.TerritoryType);
        FindPullLogger();

        _clientState.Logout += ClientStateOnLogout;
        UpdateCharacter();

        // config
        _configuration.OnSave += OnConfigurationSave;
    }

    private void ClientStateOnLogout(object? sender, EventArgs e) => _state.Character = null;

    public void Dispose()
    {
        _duh.EndEvent -= DirectorEndEvent;
        _clientState.Logout -= ClientStateOnLogout;
        _container.Resolve<Framework>().Update -= OnUpdate;
        _container.Resolve<ClientState>().TerritoryChanged -= ClientStateOnTerritoryChanged;
        _configuration.OnSave -= OnConfigurationSave;
    }

    private void OnConfigurationSave(object? sender, EventArgs e)
    {
        FindPullLogger();
    }

    private void DirectorEndEvent(object? sender, EndEventArgs e)
    {
        // PluginLog.Information("invoke combat end via director with " + e.IsClear);
        EndCombat(e);
    }

    private void ClientStateOnTerritoryChanged(object? sender, ushort territoryType)
    {
        if (_state.CurrentTerritoryType == territoryType) return;
        _duh.ResetAvailability();
        _state.CurrentTerritoryType = territoryType;
        _state.CurrentTerritoryName = _territoryResolver.Name(territoryType);
        FindPullLogger();
    }

    private void UpdateCharacter()
    {
        if (_clientState.LocalPlayer is null)
        {
            _state.Character = null;
            return;
        }

        _state.Character = _clientState.LocalPlayer.Name + "@" +
                           (_clientState.LocalPlayer.HomeWorld.GameData?.Name ?? "");
    }

    private void FindPullLogger()
    {
        var oldPl = _state.CurrentPullLogger;
        _state.CurrentPullLogger =
            _configuration.PullLoggers.Find(x => x.TerritoryType.Equals(_state.CurrentTerritoryType));
        if (oldPl != _state.CurrentPullLogger) _state.InvokePullLoggerChanged();
    }

    private void EnterCombat()
    {
        _state.PullStart = DateTime.Now;

        var sw = new Stopwatch();
        sw.Start();
        _state.InvokePullingEvent(new StateData.PullingEventArgs(true));
        PluginLog.Debug("invoking start event took " + sw.Elapsed);
    }

    private void EndCombat(EndEventArgs? e = null)
    {
        _state.PullEnd = DateTime.Now;
        var sw = new Stopwatch();
        sw.Start();
        _state.InvokePullingEvent(new StateData.PullingEventArgs(false, e?.IsClear));
        PluginLog.Debug("invoking end event took " + sw.Elapsed);
    }

    private void OnUpdate(Framework framework)
    {
        if (_state.Character is null) UpdateCharacter();
        if (_state.CurrentPullLogger == null) return;
        var prevInCombat = _state.Pulling;

        _state.Pulling = _condition[ConditionFlag.InCombat];
        // if not in combat, check if any party member is pulling (does not work in solo)
        if (!_state.Pulling)
            foreach (var actor in _partyList)
            {
                if (actor.GameObject is not Character character ||
                    (character.StatusFlags & StatusFlags.InCombat) == 0) continue;
                _state.Pulling = true;
                break;
            }

        if (_state.Pulling) _state.PullEnd = DateTime.Now;
        // ReSharper disable once ConvertIfStatementToSwitchStatement - sounds good, but it makes it unreadable
        if (!prevInCombat && _state.Pulling) EnterCombat();
        if (!_duh.Available && prevInCombat && !_state.Pulling) EndCombat();
    }

    private void ConditionOnConditionChange(ConditionFlag flag, bool value)
    {
        if (flag == ConditionFlag.BoundByDuty && _configuration.OnlyInDuty) _state.RunUpdate = value;
    }
}