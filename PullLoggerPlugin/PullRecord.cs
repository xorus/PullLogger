using System;
using PullLogger.Db;

namespace PullLogger
{
    public readonly record struct PullRecord(
        PullEvent EventName,
        DateTime? Time,
        int? Pull,
        ushort? TerritoryType,
        TimeSpan? Duration,
        string? ContentName,
        bool? IsClear
    );
}