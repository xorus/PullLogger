using System;

namespace PullLogger;

public readonly record struct PullRecord(
    PullEvent EventName,
    DateTime? Time,
    int? Pull,
    ushort? TerritoryType,
    TimeSpan? Duration,
    string? ContentName,
    bool? IsClear
);

public enum PullEvent
{
    Start,
    End,
    Pull,
    RetconnedPull,
    InvalidPull
}