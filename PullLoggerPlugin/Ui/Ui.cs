using System;
using System.Globalization;
using ImGuiNET;

namespace PullLogger.Ui;

internal class Ui : IDisposable
{
    private readonly State.StateData _stateData;

    public Ui(Container container)
    {
        _stateData = container.Resolve<State.StateData>();
    }

    public void Dispose()
    {
    }

    public void Draw()
    {
        if (_stateData.CurrentPullLogger is not { Visible: true }) return;

        if (ImGui.Begin(
                "PullLogger Pull Counter", //, ref _state.IsPullCounterOpen, ImGuiWindowFlags.AlwaysAutoResize))
                ImGuiWindowFlags.AlwaysAutoResize |
                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoResize))
            ImGui.Text("Pull " + _stateData.CurrentPullLogger.PullCount.ToString(CultureInfo.InvariantCulture));

        ImGui.End();
    }
}