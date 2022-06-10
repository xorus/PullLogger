using System;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Logging;
using PullLogger.Events;

namespace PullLogger;

public class State : IDisposable
{
    private readonly Condition _condition;
    private readonly Configuration _configuration;
    private readonly Container _container;
    private readonly EndViaDirectorUpdateHook _duh;
    private readonly PartyList _partyList;
    private readonly TerritoryResolver _territoryResolver;

    private bool _runUpdate = true;
    // public event EventHandler<PullLoggerConfig?> PullLoggerChanged;

    public State(Container container)
    {
        _container = container;
        _partyList = container.Resolve<PartyList>();
        _container.Resolve<Framework>().Update += OnUpdate;
        _configuration = container.Resolve<Configuration>();
        _territoryResolver = container.Resolve<TerritoryResolver>();
        _condition = container.Resolve<Condition>();

        // condition
        var condition = container.Resolve<Condition>();
        Pulling = condition[ConditionFlag.InCombat];
        if (Pulling) EnterCombat();
        condition.ConditionChange += ConditionOnConditionChange;

        // end via director
        _duh = container.Resolve<EndViaDirectorUpdateHook>();
        _duh.EndEvent += DirectorEndEvent;

        // clientState
        var clientState = container.Resolve<ClientState>();
        clientState.TerritoryChanged += ClientStateOnTerritoryChanged;
        ClientStateOnTerritoryChanged(this, clientState.TerritoryType);
        FindPullLogger();

        // config
        _configuration.OnSave += OnConfigurationSave;
    }

    public ushort CurrentTerritoryType { get; private set; }
    public string CurrentTerritoryName { get; private set; } = "";
    public bool Pulling { get; private set; }
    public TimeSpan PullDuration { get; private set; }
    public DateTime PullStart { get; private set; }
    public DateTime PullEnd { get; private set; }
    public PullLoggerConfig? CurrentPullLogger { get; private set; }

    public void Dispose()
    {
        _duh.EndEvent -= DirectorEndEvent;
        _container.Resolve<Framework>().Update -= OnUpdate;
        _container.Resolve<ClientState>().TerritoryChanged -= ClientStateOnTerritoryChanged;
        _configuration.OnSave -= OnConfigurationSave;
    }

    public event EventHandler<PullingEventArgs>? PullingEvent;

    private void OnConfigurationSave(object? sender, EventArgs e)
    {
        FindPullLogger();
    }

    private void DirectorEndEvent(object? sender, EndEventArgs e)
    {
        PluginLog.Information("invoke combat end via director with " + e.IsClear);
        EndCombat(e);
    }

    private void ClientStateOnTerritoryChanged(object? sender, ushort territoryType)
    {
        if (CurrentTerritoryType == territoryType) return;
        _duh.ResetAvailability();
        CurrentTerritoryType = territoryType;
        CurrentTerritoryName = _territoryResolver.Name(territoryType);
        FindPullLogger();
    }

    private void FindPullLogger()
    {
        CurrentPullLogger = _configuration.PullLoggers.Find(x => x.TerritoryType.Equals(CurrentTerritoryType));
    }

    private void EnterCombat()
    {
        PullStart = DateTime.Now;
        PluginLog.Information("invoke combat start via clientstate");
        PullingEvent?.Invoke(this, new PullingEventArgs(true));
    }

    private void EndCombat(EndEventArgs? e = null)
    {
        UpdateTime();
        PullingEvent?.Invoke(this, new PullingEventArgs(false, e?.IsClear));
    }

    private void UpdateTime()
    {
        PullEnd = DateTime.Now;
        PullDuration = PullEnd - PullStart;
    }

    private void OnUpdate(Framework framework)
    {
        if (!_runUpdate) return;

        var prevInCombat = Pulling;

        Pulling = _condition[ConditionFlag.InCombat];
        // if not in combat, check if any party member is pulling (does not work in solo)
        if (!Pulling)
            foreach (var actor in _partyList)
            {
                if (actor.GameObject is not Character character ||
                    (character.StatusFlags & StatusFlags.InCombat) == 0) continue;
                Pulling = true;
                break;
            }

        if (Pulling) UpdateTime();
        // ReSharper disable once ConvertIfStatementToSwitchStatement - sounds good, but it makes it unreadable
        if (!prevInCombat && Pulling) EnterCombat();
        if (!_duh.Available && prevInCombat && !Pulling)
        {
            EndCombat();
            PluginLog.Information("end via client ");
        }
    }

    private void ConditionOnConditionChange(ConditionFlag flag, bool value)
    {
        if (flag == ConditionFlag.BoundByDuty && _configuration.OnlyInDuty) _runUpdate = value;
    }

    public record PullingEventArgs(bool InCombat, bool? IsClear = null);
}