using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Logging;
using ImGuiNET;
using Microsoft.EntityFrameworkCore;
using PullLogger.Db;
using PullLogger.Db.Data;
using PullLogger.State;

namespace PullLogger.Ui;

public sealed class PullDbUi : IDisposable
{
    private readonly Container _container;
    private bool _visible = true;
    private readonly DbSet<Pull> _pulls;
    private List<Pull>? _sorted = null;
    private readonly Database _db;
    private readonly Random _random;
    private readonly StateData _state;
    private List<string>? _characters;

    private bool _updateRequired = false;
    private string? _characterFilter = null;
    private readonly FileDialogManager _fdm;
    private bool _allColumns = false;

    public bool Visible
    {
        get => _visible;
        set => _visible = value;
    }

    public PullDbUi(Container container)
    {
        _container = container;
        _db = container.Resolve<Database>();
        _pulls = container.Resolve<Database>().Pulls;
        _state = container.Resolve<StateData>();
        _random = new Random();
        _fdm = new FileDialogManager();

        _db.SavedChanges += DbOnSavedChanges;
    }

    // trigger a re-sort on next ui draw
    private void DbOnSavedChanges(object? sender, SavedChangesEventArgs e) =>
        _updateRequired = true;

    public void Dispose() => _db.SavedChanges -= DbOnSavedChanges;

    private void Update()
    {
        PluginLog.Information("updating");

        var query = _pulls.AsQueryable();
        if (_showOnlyPulls)
        {
            query = query.Where(pull => pull.EventName == PullEvent.Pull);
            // query = query.Where(
            // pull => pull.EventName != PullEvent.Start && pull.EventName != PullEvent.End
            // );
        }

        _sorted = query.OrderByDescending(x => x.Time).ToList();
        _characters = _pulls.GroupBy(x => x.Character).Select(x => x.Key).ToList();
        if (_characters.Count == 1) _characterFilter = _characters[0];
        _updateRequired = false;
    }

    private bool _showOnlyPulls = true;

    private void TableStatus()
    {
        if (_sorted == null || _characters == null) return;
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Cog))
        {
            _container.Resolve<ConfigurationUi>().Visible = true;
        }

        if (_characters.Count > 1)
        {
            if (ImGui.BeginCombo("###character selection", _characterFilter ?? "All"))
            {
                if (ImGui.Selectable("All", _characterFilter == null)) _characterFilter = null;
                foreach (var character in _characters)
                {
                    var selected = _characterFilter == character;
                    if (ImGui.Selectable(character, selected)) _characterFilter = character;
                    if (selected) ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("clear") && _sorted != null)
        {
            _pulls.RemoveRange(_sorted);
            _db.SaveChangesAsync();
        }

        // ImGui.SameLine();
        // if (ImGui.Button("add"))
        // {
        //     var eventTypes = Enum.GetValues(typeof(PullEvent));
        //
        //     var max = _random.Next(5, 20);
        //     for (var i = 0; i < max; i++)
        //     {
        //         _pulls.Add(new Pull
        //         {
        //             EventName = (PullEvent)(eventTypes.GetValue(_random.Next(eventTypes.Length)) ?? 0),
        //             Time = DateTime.Now,
        //             IsClear = _random.Next(0, 1) != 0,
        //             Duration = new TimeSpan(0, _random.Next(0, 17), _random.Next(0, 60)),
        //             ContentName = "some content instance name",
        //             Character = _state.Character ?? "<unknown>"
        //         });
        //     }
        //
        //     _db.SaveChangesAsync();
        // }

        ImGui.SameLine();
        if (ImGui.Button("import"))
        {
            var conf = _container.Resolve<Configuration>();
            var dir = conf.LastImportDir;
            if (!Directory.Exists(dir)) dir = null;

            _fdm.OpenFileDialog("Select a log file", "Pull Logger CSV{.csv}", (b, s) =>
            {
                if (!b) return;
                var dirUpdated = false;
                foreach (var s1 in s)
                {
                    if (!dirUpdated)
                    {
                        conf.LastImportDir = new FileInfo(s1).DirectoryName;
                        conf.Save();
                        dirUpdated = true;
                    }

                    _container.Resolve<Exchange>().ImportFile(s1);
                }
            }, 0, dir);
        }

        ImGui.SameLine();
        if (ImGui.Button("export"))
        {
        }

        ImGui.SameLine();
        if (ImGui.Checkbox("only pulls", ref _showOnlyPulls)) _updateRequired = true;
        ImGui.SameLine();
        if (ImGui.Checkbox("all columns", ref _allColumns)) _updateRequired = true;

        ImGui.SameLine();
        CoolBeans.PushTextRight(_sorted?.Count + " items");
    }

    public void Draw()
    {
        _visible = true;
        if (ImGui.Begin("PullLogger database", ref _visible, ImGuiWindowFlags.AlwaysAutoResize))
        {
            _fdm.Draw();
            if (_updateRequired || _sorted == null) Update();

            if (ImGui.BeginTable("Pulls", 9,
                    ImGuiTableFlags.Resizable
                    | ImGuiTableFlags.Reorderable
                    | ImGuiTableFlags.ScrollY
                    | ImGuiTableFlags.SizingStretchProp
                    | ImGuiTableFlags.Hideable
                    | ImGuiTableFlags.Borders, new Vector2(800, 400)))
            {
                ImGui.TableSetupColumn("Character");
                ImGui.TableSetColumnEnabled(0, _characterFilter == null);
                ImGui.TableSetupColumn("Event");
                ImGui.TableSetColumnEnabled(1, true);
                ImGui.TableSetupColumn("Date");
                ImGui.TableSetColumnEnabled(2, true);
                ImGui.TableSetupColumn("Duration");
                ImGui.TableSetColumnEnabled(3, true);
                ImGui.TableSetupColumn("Zone");
                ImGui.TableSetColumnEnabled(4, true);
                ImGui.TableSetupColumn("Clear");
                ImGui.TableSetColumnEnabled(5, true);
                ImGui.TableSetupColumn("Pull #");
                ImGui.TableSetColumnEnabled(6, true);
                ImGui.TableSetupColumn("Real #");
                ImGui.TableSetColumnEnabled(7, _allColumns);
                ImGui.TableSetupColumn("Source");
                ImGui.TableSetColumnEnabled(8, _allColumns);
                ImGui.TableHeadersRow();
                

                foreach (var pull in _sorted!)
                {
                    if (_characterFilter != null && pull.Character != _characterFilter) continue;
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text(pull.Character);
                    ImGui.TableNextColumn();
                    ImGui.Text(pull.EventName.ToString());
                    ImGui.TableNextColumn();
                    ImGui.Text(pull.Time?.ToString("g") ?? "");
                    ImGui.TableNextColumn();
                    ImGui.Text(pull.Duration?.ToString(@"mm\:ss") ?? "");
                    // ImGui.Text(pull.Duration?.ToString(@"mm\:ss\.ff") ?? "");
                    ImGui.TableNextColumn();
                    ImGui.Text((pull.TerritoryType?.ToString()) + " " + (pull.ContentName ?? ""));
                    ImGui.TableNextColumn();
                    if (pull.IsClear == null) ImGui.Text("");
                    else ImGui.Text(pull.IsClear.Value ? "✓" : "×");
                    ImGui.TableNextColumn();
                    ImGui.Text(pull.AutoPullNumber.ToString());
                    ImGui.TableNextColumn();
                    ImGui.Text(pull.RealPullNumber.ToString());
                    ImGui.TableNextColumn();
                    ImGui.Text(pull.Source.ToString());
                }

                ImGui.EndTable();
            }

            TableStatus();
        }

        ImGui.End();
    }
}