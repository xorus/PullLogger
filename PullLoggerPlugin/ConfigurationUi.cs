using System;
using System.IO;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Logging;
using ImGuiNET;
using Newtonsoft.Json.Serialization;
using PullLogger.Events;

namespace PullLogger;

public class ConfigurationUi
{
    private readonly Configuration _configuration;
    private readonly EndViaDirectorUpdateHook _duh;
    private readonly TerritoryResolver _resolver;
    private readonly State _state;

    private int _selected;
    private bool _visible = false;
    private FileDialogManager Fdm { get; }

    public ConfigurationUi(Container container)
    {
        _configuration = container.Resolve<Configuration>();
        _state = container.Resolve<State>();
        _resolver = container.Resolve<TerritoryResolver>();
        _duh = container.Resolve<EndViaDirectorUpdateHook>();

        Fdm = new FileDialogManager();
    }

    public bool Visible
    {
        get => _visible;
        set => _visible = value;
    }

    // private bool _removeConfirm = false;

    private bool _removeConfirmationPopup;

    private void RemoveModalContent(PullLoggerConfig selected)
    {
        ImGui.Text("Are you sure you want to remove this instance configuration?\n" +
                   "Logs will not be affected.");
        ImGui.Text("Instance configuration:");
        ImGui.Indent();
        ImGui.Text("#" + selected.TerritoryType + " - " + _resolver.Name(selected.TerritoryType));
        ImGui.Text(selected.PullCount + " pulls");
        ImGui.Unindent();

        ImGui.Separator();

        var w = ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X;
        ImGui.SetItemDefaultFocus();
        if (ImGui.Button("No", new Vector2(w / 2, 0))) _removeConfirmationPopup = false;
        ImGui.SameLine();
        if (ImGui.Button("DELETE", new Vector2(w / 2, 0)))
        {
            _configuration.PullLoggers.RemoveAt(_selected);
            _configuration.Save();
            _selected = Math.Max(0, _selected - 1);
            ImGui.CloseCurrentPopup();
        }
    }

    public void Draw()
    {
        const string deletePopup = "Remove confirmation";

        var len = _configuration.PullLoggers.Count;
        var selectionValid = len > 0 && _selected >= 0 && _selected < len;
        PullLoggerConfig? selected = null;
        if (len > 0 && _selected >= 0 && _selected < len)
            selected = _configuration.PullLoggers[_selected];

        // if (!Visible) return;

        if (ImGui.Begin("PullLogger configuration", ref _visible, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextWrapped(
                "This plugin will log all your future pulls for your own statistical analysis purposes.\n" +
                "It can log the start, end and duration of your pulls, as well as the territory you pulled in.\n");
            ImGui.Separator();

            const int firstRowWidth = 100;
            const int iconBtnWidth = 35;
            if (ImGui.BeginTable("PullLoggers", 2,
                    ImGuiTableFlags.Resizable
                    | ImGuiTableFlags.BordersInnerV
                    | ImGuiTableFlags.PadOuterX
                    | ImGuiTableFlags.SizingFixedFit))
            {
                if (selected != null)
                {
                    var center = ImGui.GetMainViewport().GetCenter();
                    ImGui.SetNextWindowPos(center, ImGuiCond.Always, new Vector2(0.5f, 0.5f));
                    if (ImGui.BeginPopupModal(deletePopup, ref _removeConfirmationPopup,
                            ImGuiWindowFlags.AlwaysAutoResize))
                    {
                        RemoveModalContent(selected);
                        ImGui.EndPopup();
                    }
                }

                ImGui.TableSetupColumn("AAA", ImGuiTableColumnFlags.WidthFixed, firstRowWidth);
                ImGui.TableSetupColumn("BBB", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextColumn();
                ImGui.TableHeader("Instances");
                ImGui.TableNextColumn();
                ImGui.TableHeader("Instance configuration");

                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                for (var i = 0; i < _configuration.PullLoggers.Count; i++)
                {
                    var name = _resolver.Name(_configuration.PullLoggers[i].TerritoryType);
                    if (ImGui.Selectable($"{name}###PullLoggerSelectable{i}", _selected == i)) _selected = i;
                }

                ImGui.TableNextColumn();
                if (selected != null) DrawPullLoggerConfig(selected);

                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);

                var w = ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X;
                if (ImGui.Button(FontAwesomeIcon.Plus.ToIconString(), new Vector2(w / 2, 0)))
                {
                    _configuration.PullLoggers.Add(new PullLoggerConfig
                    {
                        TerritoryType = _state.CurrentTerritoryType
                    });
                    _selected = _configuration.PullLoggers.Count - 1;
                    _configuration.Save();
                }

                ImGui.SameLine();

                if (!selectionValid) ImGui.PushStyleVar(ImGuiStyleVar.Alpha, .3f);
                if (ImGui.Button(FontAwesomeIcon.Minus.ToIconString(), new Vector2(w / 2, 0)) &&
                    selectionValid)
                {
                    _removeConfirmationPopup = true;
                    ImGui.OpenPopup(deletePopup);
                }

                if (!selectionValid) ImGui.PopStyleVar();

                ImGui.PopFont();
                ImGui.PopStyleVar();
                ImGui.EndTable();
            }

            ImGui.Separator();
            ImGui.TextWrapped("Pull count increases at the start of every pull.");
            ImGui.TextWrapped("A pull is defined by the transition from non-combat state to combat state for " +
                              "your party.");

            if (ImGui.CollapsingHeader("File format description"))
            {
                ImGui.TextWrapped("The log is a CSV file with the following columns:");
                ImGui.Indent();
                ImGui.Text("Event,Time,Pull,Duration,Clear,TerritoryType,ContentName");
                ImGui.Unindent();
                ImGui.NewLine();
                ImGui.TextWrapped("Columns:");
                ImGui.Indent();
                ImGui.TextWrapped("Event: start, end, or pull");
                ImGui.TextWrapped("Time: YYYY-MM-DD HH:MM:SS");
                ImGui.TextWrapped("Pull: pull number");
                ImGui.TextWrapped("Duration: duration of the pull in seconds (floating point)");
                ImGui.TextWrapped("Clear: 0 for a wipe, 1 for a clear");
                ImGui.TextWrapped("TerritoryType: the territory ID where the event happened");
                ImGui.TextWrapped("ContentName: content name associated with that TerritoryType");
                ImGui.Unindent();
                ImGui.NewLine();
                ImGui.TextWrapped("Example contents:");
                ImGui.Indent();
                ImGui.Text("start,2022-06-14 19:41:28,,,1,1046,the Navel");
                ImGui.Text("end,2022-06-14 19:41:46,,,1,1046,the Navel");
                ImGui.Text("pull,2022-06-14 19:41:46,355,18.0034,1,1046,the Navel");
                ImGui.Unindent();
            }

            ImGui.Separator();
            if (ImGui.Button("Close")) Visible = false;
        }

        Fdm.Draw();
        ImGui.End();
    }

    private bool _editPullCount;
    private bool _editTerritoryType;

    private void Label(string str)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, .7f);
        ImGui.Text(str);
        ImGui.PopStyleVar();
    }

    private void DrawPullLoggerConfig(PullLoggerConfig plc)
    {
        var pullCount = plc.PullCount;
        ImGui.AlignTextToFramePadding();
        Label("Current pull count:");
        ImGui.SameLine();
        ImGui.Text(pullCount.ToString());
        ImGui.SameLine();
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Pen)) _editPullCount = !_editPullCount;

        if (_editPullCount)
        {
            ImGui.Indent();
            ImGui.TextWrapped("Warning: changing the pull number will not affect your already saved logs in any way.");
            ImGui.PushItemWidth(100f);
            if (ImGui.InputInt("Current pull count", ref pullCount))
            {
                plc.PullCount = pullCount;
                _configuration.Save();
            }

            ImGui.PopItemWidth();
            ImGui.Unindent();
        }
        
        var enabled = plc.Enabled;
        if (ImGui.Checkbox("Enable logging for this instance", ref enabled))
        {
            plc.Enabled = enabled;
            _configuration.Save();
        }

        var visible = plc.Visible;
        if (ImGui.Checkbox("Show pull number window", ref visible))
        {
            plc.Visible = visible;
            _configuration.Save();
        }

        // --------------------------------------------------
        ImGui.PushItemWidth(350f);

        // ImGui.Text("Available templates:");
        // ImGui.BulletText("{day}: pull day");
        Label("Log file path:");
        var filePath = plc.FilePath;
        if (ImGui.InputText("###File path", ref filePath, 1024))
        {
            plc.FilePath = filePath;
            _configuration.Save();
        }

        ImGui.SameLine();
        if (ImGuiComponents.IconButton(FontAwesomeIcon.FolderOpen))
        {
            var startPath = plc.FilePath.Length > 0 ? Path.GetDirectoryName(plc.FilePath) : null;
            Fdm.SaveFileDialog("Log file path", "*", $"{_resolver.Name(plc.TerritoryType).Slugify()}", ".pplog",
                (b, s) =>
                {
                    if (b) plc.FilePath = s;
                }, startPath, false);
        }

        ImGui.PopItemWidth();

        // --------------------------------------------------

        var logCombatStart = plc.LogCombatStart;
        var logCombatEnd = plc.LogCombatEnd;
        var logRecap = plc.LogRecap;

        ImGui.AlignTextToFramePadding();
        Label("Events to log:");
        ImGui.SameLine();
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

        ImGui.NewLine();

        if (ImGui.Button("Change TerritoryType (instance/content id)")) _editTerritoryType = !_editTerritoryType;

        if (_editTerritoryType)
        {
            ImGui.Indent();
            var territoryType = (int)plc.TerritoryType;
            ImGui.TextWrapped(
                $"You are currently in TerritoryType ID {_state.CurrentTerritoryType} :\n{_state.CurrentTerritoryName}");
            if (ImGui.InputInt("Territory Type ID", ref territoryType))
            {
                if (territoryType < ushort.MinValue) territoryType = ushort.MinValue;
                if (territoryType > ushort.MaxValue) territoryType = ushort.MaxValue;
                plc.TerritoryType = (ushort)territoryType;
                _configuration.Save();
            }

            ImGui.Unindent();
        }
    }
}