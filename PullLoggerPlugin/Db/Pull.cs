using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PullLogger.Db;

/// <summary>
/// Represents a pull event
/// </summary>
public class Pull
{
    public Guid Id { get; set; }
    public PullEvent EventName { get; set; }
    public DateTime? Time { get; set; }

    /// <summary>
    /// Automatically updated pull number that will be continuous even if you retroactively invalidate pulls.
    /// </summary>
    public int? AutoPullNumber { get; set; }

    /// <summary>Pull number at the time this was inserted in the database.</summary>
    public int? RealPullNumber { get; set; }

    public ushort? TerritoryType { get; set; }
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// Stored for display in case I can't access the content names or if they get changed somehow 
    /// </summary>
    public string? ContentName { get; set; }

    public bool? IsClear { get; set; }

    /// <summary>
    /// Sort-of-alt-friendlyness
    /// Formatted as "Character Name@World"
    /// </summary>
    public string Character { get; set; } = "<unknown>";

    public Source Source { get; set; } = Source.Auto;
}

public enum Source
{
    Auto,
    ImportCsv,
}

public enum PullEvent
{
    Start,
    End,
    Pull,
    RetconnedPull,
    InvalidPull
}