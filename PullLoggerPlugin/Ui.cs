using System;
using System.Globalization;
using ImGuiNET;

namespace PullLogger;

internal class Ui : IDisposable
{
    private readonly State _state;

    public Ui(Container container)
    {
        _state = container.Resolve<State>();
    }

    public bool Visible { get; set; } = false;

    public void Dispose()
    {
    }

    public void Draw()
    {
        // if (!Visible)
        // {
        //     return;
        // }

        if (_state.CurrentPullLogger is not { Visible: true }) return;

        if (ImGui.Begin(
                "PullLogger Pull Counter", //, ref _state.IsPullCounterOpen, ImGuiWindowFlags.AlwaysAutoResize))
                ImGuiWindowFlags.AlwaysAutoResize |
                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoResize))
            ImGui.Text("Pull " + _state.CurrentPullLogger.PullCount.ToString(CultureInfo.InvariantCulture));

        ImGui.End();
    }
}