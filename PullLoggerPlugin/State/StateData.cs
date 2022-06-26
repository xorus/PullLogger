using System;

namespace PullLogger.State;

public class StateData
{
    public event EventHandler<PullLoggerConfig?>? PullLoggerChanged = null;
    public event EventHandler<PullingEventArgs>? PullingEvent = null;

    public record PullingEventArgs(bool InCombat, bool? IsClear = null);

    public bool RunUpdate = true;
    public ushort CurrentTerritoryType { get; set; }
    public string CurrentTerritoryName { get; set; } = "";
    public bool Pulling { get; set; }
    public DateTime PullStart { get; set; }
    public DateTime PullEnd { get; set; }
    public PullLoggerConfig? CurrentPullLogger { get; set; }
    public void InvokePullLoggerChanged() => PullLoggerChanged?.Invoke(this, CurrentPullLogger);
    public void InvokePullingEvent(PullingEventArgs args) => PullingEvent?.Invoke(this, args);
}