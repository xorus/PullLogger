using ImGuiNET;

namespace PullLogger.Ui;

public static class CoolBeans
{
    public static void PushTextRight(string text)
    {
        var size = ImGui.CalcTextSize(text);
        var w = ImGui.GetCursorPosX() + (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X - size.X);
        ImGui.SetCursorPosX(w);
        ImGui.Text(text);
    }
}