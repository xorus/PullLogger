using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Logging;
using ImGuiNET;
using PullLogger.Events;
using PullLogger.Interface;

namespace PullLogger.Ui;

public class ConfigurationUi
{
    private readonly Configuration _configuration;
    private readonly ITerritoryResolver _resolver;
    private readonly State.StateData _stateData;

    private int _selected;
    private bool _visible = false;
    private readonly ConfirmPopup _removeConfirmPopup;
    private bool _editPullCount;
    private bool _editTerritoryType;
    private PullLoggerConfig? _selectedToConfirm = null;

    public bool Visible
    {
        get => _visible;
        set => _visible = value;
    }

    private FileDialogManager Fdm { get; }

    public ConfigurationUi(Container container)
    {
        _configuration = container.Resolve<Configuration>();
        _stateData = container.Resolve<State.StateData>();
        _resolver = container.Resolve<ITerritoryResolver>();
        _removeConfirmPopup = new ConfirmPopup(
            "Remove confirmation", RemoveModalContent,
            "DELETE", RemoveModalConfirm
        );

        Fdm = new FileDialogManager();
    }


    // private bool _removeConfirm = false;

    private const string NamePattern = @"^(?'name'[A-Z][a-z'\-]{1,14} [A-Z][a-z'\-]{1,14})@(?'world'[A-Z][a-z\'-]+)$";
    private bool _invalidName = false;

    public void Draw()
    {
        var len = _configuration.PullLoggers.Count;
        var selectionValid = len > 0 && _selected >= 0 && _selected < len;
        PullLoggerConfig? selected = null;
        if (len > 0 && _selected >= 0 && _selected < len)
            selected = _configuration.PullLoggers[_selected];

        if (!Visible) return;

        if (_configuration.PlayerNameWorld == null && _stateData.Character != null)
        {
            _configuration.PlayerNameWorld = _stateData.Character;
            _configuration.Save();
        }

        if (ImGui.Begin("PullLogger configuration", ref _visible, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextWrapped(
                "This plugin will log all your future pulls for your own statistical analysis purposes.\n" +
                "It can log the start, end and duration of your pulls, as well as the territory you pulled in.\n");
            ImGui.Separator();

            if (_stateData.Character is null)
            {
                ImGui.Text("Not logged in or standalone mode detected.");

                var name = _configuration.PlayerNameWorld ?? "";
                ImGui.InputText("Character Name", ref name, 100);

                if (ImGui.IsItemEdited())
                {
                    var match = Regex.Match(name, NamePattern);

                    if (match.Success)
                    {
                        _configuration.PlayerNameWorld = match.Groups["name"] + "@" + match.Groups["world"];
                        _configuration.Save();
                        _invalidName = false;
                    }
                    else _invalidName = true;
                }

                if (_invalidName) ImGui.Text("must respect format Player Name@World");

                ImGui.Separator();
            }

            const int firstRowWidth = 100;
            // const int iconBtnWidth = 35;
            if (ImGui.BeginTable("PullLoggers", 2,
                    ImGuiTableFlags.Resizable
                    | ImGuiTableFlags.BordersInnerV
                    | ImGuiTableFlags.SizingFixedFit))
            {
                if (selected != null)
                {
                    _selectedToConfirm = selected;
                    _removeConfirmPopup.Draw();
                }

                ImGui.TableSetupColumn("Instances", ImGuiTableColumnFlags.WidthFixed, firstRowWidth);
                ImGui.TableSetupColumn("Instance configuration", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableHeadersRow();

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
                        TerritoryType = _stateData.CurrentTerritoryType
                    });
                    _selected = _configuration.PullLoggers.Count - 1;
                    _configuration.Save();
                }

                ImGui.SameLine();

                if (!selectionValid) ImGui.PushStyleVar(ImGuiStyleVar.Alpha, .3f);
                if (ImGui.Button(FontAwesomeIcon.Minus.ToIconString(), new Vector2(w / 2, 0)) &&
                    selectionValid)
                {
                    _removeConfirmPopup.Open();
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

    private static void Label(string str)
    {
        ImGui.TextDisabled(str);
    }

    private void DrawPullLoggerConfig(PullLoggerConfig plc)
    {
        Label("Last logged pull:");
        ImGui.SameLine();
        ImGui.Text(plc.LastPull != null ? plc.LastPull.Value.ToString("F", CultureInfo.CurrentCulture) : "(never)");

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

        ImGui.AlignTextToFramePadding();
        Label("Invalidate pulls below:");
        var autoInvalidate = plc.AutoInvalidate;
        ImGui.SameLine();
        ImGui.PushItemWidth(100f);
        if (ImGui.InputFloat("seconds " + (autoInvalidate <= 0 ? "(disabled)" : "") + "##AutoInvalidate",
                ref autoInvalidate, 1f, 5f, "%.1f"))
        {
            plc.AutoInvalidate = Math.Max(0, autoInvalidate);
            _configuration.Save();
        }

        ImGui.PopItemWidth();

        ImGui.NewLine();

        if (ImGui.Button("Change TerritoryType (instance/content id)")) _editTerritoryType = !_editTerritoryType;

        if (_editTerritoryType)
        {
            var territoryType = (int)plc.TerritoryType;
            ImGui.TextWrapped(
                $"You are currently in TerritoryType ID {_stateData.CurrentTerritoryType} :\n{_stateData.CurrentTerritoryName}");
            if (ImGui.InputInt("Territory Type ID", ref territoryType))
            {
                if (territoryType < ushort.MinValue) territoryType = ushort.MinValue;
                if (territoryType > ushort.MaxValue) territoryType = ushort.MaxValue;
                plc.TerritoryType = (ushort)territoryType;
                _configuration.Save();
            }
        }
    }

    private void RemoveModalContent()
    {
        ImGui.Text("Are you sure you want to remove this instance configuration?\n" +
                   "Logs will not be affected.");
        ImGui.Text("Instance configuration:");
        ImGui.Indent();
        ImGui.Text("#" + _selectedToConfirm.TerritoryType + " - " + _resolver.Name(_selectedToConfirm.TerritoryType));
        ImGui.Text(_selectedToConfirm.PullCount + " pulls");
        ImGui.Unindent();
    }

    private void RemoveModalConfirm()
    {
        _configuration.PullLoggers.RemoveAt(_selected);
        _configuration.Save();
        _selected = Math.Max(0, _selected - 1);
    }
}