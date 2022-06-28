using System.Numerics;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Action = System.Action;

namespace PullLogger.Ui;

public class ConfirmPopup
{
    public string Title;
    private readonly Action _drawContent;
    private readonly string _yesTitle;
    private readonly Action _confirm;
    private readonly string _noTitle;
    private bool _visible;

    public bool Visible
    {
        get => _visible;
        set => _visible = value;
    }


    public ConfirmPopup(
        string title,
        Action drawContent,
        string yesTitle,
        Action confirm,
        string noTitle = "No"
    )
    {
        Title = title + "###PullLogger_ConfirmPopup";
        _drawContent = drawContent;
        _yesTitle = yesTitle;
        _confirm = confirm;
        _noTitle = noTitle;
    }

    public void Draw()
    {
        var center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Always, new Vector2(0.5f, 0.5f));
        if (!ImGui.BeginPopupModal(Title, ref _visible, ImGuiWindowFlags.AlwaysAutoResize)) return;
        _drawContent();
        ImGui.Separator();

        var w = ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X;
        ImGui.SetItemDefaultFocus();
        if (ImGui.Button(_noTitle, new Vector2(w / 2, 0))) _visible = false;
        ImGui.SameLine();
        if (ImGui.Button(_yesTitle, new Vector2(w / 2, 0)))
        {
            _confirm();
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    public void Open()
    {
        _visible = true;
        ImGui.OpenPopup(Title);
    }
}