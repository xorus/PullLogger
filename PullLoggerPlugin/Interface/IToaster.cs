using Dalamud.Interface.Internal.Notifications;

namespace PullLogger.Interface;

public interface IToaster
{
    public void AddNotification(string content, string? title = null, NotificationType type = NotificationType.None,
        uint msDelay = 3000);
}