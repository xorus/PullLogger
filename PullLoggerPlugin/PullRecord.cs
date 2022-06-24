using System;

namespace PullLogger;

public readonly record struct PullRecord(
    PullEvent EventName,
    DateTime? Time,
    int? Pull,
    ushort? TerritoryType,
    TimeSpan? Duration,
    string? ContentName,
    bool? IsClear,
    bool IsValid = true
);

public enum PullEvent
{
    Start,
    End,
    Pull,
    RetCon
}