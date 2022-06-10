using System;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using PullLogger.Events;

namespace PullLogger;

public class ConfigurationUi
{
    private readonly Configuration _configuration;
    private readonly EndViaDirectorUpdateHook _duh;
    private readonly TerritoryResolver _resolver;
    private readonly State _state;

    private int _selected;
    private bool _visible = true;

    public ConfigurationUi(Container container)
    {
        _configuration = container.Resolve<Configuration>();
        _state = container.Resolve<State>();
        _resolver = container.Resolve<TerritoryResolver>();
        _duh = container.Resolve<EndViaDirectorUpdateHook>();
    }

    public bool Visible
    {
        get => _visible;
        set => _visible = value;
    }

    public void Draw()
    {
        if (!Visible) return;

        if (ImGui.Begin("PullLogger configuration", ref _visible, ImGuiWindowFlags.AlwaysAutoResize))
        {
            // ImGui.Text($"{_state.Pulling} {_state.PullStart} {_state.PullDuration} {_state.PullEnd}");

            const int firstRowWidth = 100;
            const int iconBtnWidth = 35;
            if (ImGui.BeginTable("PullLoggers", 2,
                    ImGuiTableFlags.Resizable
                    | ImGuiTableFlags.BordersInnerV
                    | ImGuiTableFlags.PadOuterX
                    | ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableSetupColumn("AAA", ImGuiTableColumnFlags.WidthFixed, firstRowWidth);
                ImGui.TableSetupColumn("BBB", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                for (var i = 0; i < _configuration.PullLoggers.Count; i++)
                {
                    var name = _resolver.Name(_configuration.PullLoggers[i].TerritoryType);
                    if (ImGui.Selectable($"{name}###PullLoggerSelectable{i}", _selected == i)) _selected = i;
                }

                ImGui.TableNextColumn();
                var len = _configuration.PullLoggers.Count;
                var selectionValid = len > 0 && _selected >= 0 && _selected < len;
                if (len > 0 && _selected >= 0 && _selected < len)
                    DrawPullLoggerConfig(_configuration.PullLoggers[_selected]);

                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);
                if (ImGui.Button(FontAwesomeIcon.Plus.ToIconString(), new Vector2(iconBtnWidth, 0)))
                    _configuration.PullLoggers.Add(new PullLoggerConfig());

                ImGui.SameLine();
                if (!selectionValid) ImGui.PushStyleVar(ImGuiStyleVar.Alpha, .3f);
                if (ImGui.Button(FontAwesomeIcon.Minus.ToIconString(), new Vector2(iconBtnWidth, 0)) &&
                    selectionValid)
                {
                    _configuration.PullLoggers.RemoveAt(_selected);
                    _selected = Math.Max(0, _selected - 1);
                }

                if (!selectionValid) ImGui.PopStyleVar();

                ImGui.PopFont();
                ImGui.PopStyleVar();
                ImGui.EndTable();
            }

            ImGui.TextWrapped("Pull count increases at the start of every pull.");
            ImGui.TextWrapped("A pull is defined by the transition from non-combat state to combat state for " +
                              "your party.");
            ImGui.TextWrapped($"Current Territory: {_state.CurrentTerritoryType} - {_state.CurrentTerritoryName}");

            ImGui.Separator();
            if (ImGui.Button("Close")) Visible = false;
        }

        ImGui.End();
    }

    private void DrawPullLoggerConfig(PullLoggerConfig plc)
    {
        var visible = plc.Visible;
        if (ImGui.Checkbox("Visible", ref visible))
        {
            plc.Visible = visible;
            _configuration.Save();
        }

        ImGui.SameLine();

        var enabled = plc.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
        {
            plc.Enabled = enabled;
            _configuration.Save();
        }

        var territoryType = (int)plc.TerritoryType;
        if (ImGui.InputInt("Territory Type ID", ref territoryType))
        {
            if (territoryType < ushort.MinValue) territoryType = ushort.MinValue;
            if (territoryType > ushort.MaxValue) territoryType = ushort.MaxValue;
            plc.TerritoryType = (ushort)territoryType;
            _configuration.Save();
        }

        var pullCount = plc.PullCount;
        if (ImGui.InputInt("Current pull count", ref pullCount))
        {
            plc.PullCount = pullCount;
            _configuration.Save();
        }

        // --------------------------------------------------

        ImGui.Text("Events to log");

        var logCombatStart = plc.LogCombatStart;
        var logCombatEnd = plc.LogCombatEnd;
        var logRecap = plc.LogRecap;

        if (ImGui.Checkbox("Pull start", ref logCombatStart))
        {
            plc.LogCombatStart = logCombatStart;
            _configuration.Save();
        }

        ImGui.SameLine();
        if (ImGui.Checkbox("Pull end", ref logCombatEnd))
        {
            plc.LogCombatEnd = logCombatEnd;
            _configuration.Save();
        }

        ImGui.SameLine();
        if (ImGui.Checkbox("Pull recap", ref logRecap))
        {
            plc.LogRecap = logRecap;
            _configuration.Save();
        }

        // --------------------------------------------------
        ImGui.PushItemWidth(350f);

        // ImGui.Text("Available templates:");
        // ImGui.BulletText("{day}: pull day");
        var filePath = plc.FilePath;
        if (ImGui.InputText("File path", ref filePath, 1024))
        {
            plc.FilePath = filePath;
            _configuration.Save();
        }

        ImGui.PopItemWidth();
    }
}