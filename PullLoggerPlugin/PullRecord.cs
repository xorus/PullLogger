using System;

namespace PullLogger;

public readonly record struct PullRecord(
    string EventName,
    DateTime? Time,
    int? Pull,
    ushort? TerritoryType,
    TimeSpan? Duration,
    string? ContentName,
    bool? IsClear
);