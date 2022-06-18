using System;

namespace PullLogger;

public class RetconError : Exception
{
    public RetconError()
    {
    }

    public RetconError(string reason)
    {
        Reason = reason;
    }

    public string Reason { get; init; } = "unknown";
}