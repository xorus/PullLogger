using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;
using PullLogger.Interface;

namespace PullLogger.Dalamud;

public class DalamudNotification : IToaster
{
    private readonly UiBuilder _uiBuilder;

    public DalamudNotification(UiBuilder uiBuilder)
    {
        _uiBuilder = uiBuilder;
    }

    public void AddNotification(string content, string? title = null, NotificationType type = NotificationType.None,
        uint msDelay = 3000) =>
        _uiBuilder.AddNotification(content, title, type, msDelay);
}