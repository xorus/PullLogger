using System;
using Dalamud.Interface.Internal.Notifications;
using PullLogger.Interface;

namespace Standalone.Mocks;

public class ConsoleNotification : IToaster
{
    public void AddNotification(string content, string? title = null, NotificationType type = NotificationType.None,
        uint msDelay = 3000)
    {
        if (title != null) Console.WriteLine("----[ " + title + "]----");
        Console.WriteLine(type + " " + content);
    }
}