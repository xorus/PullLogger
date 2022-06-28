// PullLogger
// Copyright (C) 2022 Xorus
// GNU GENERAL PUBLIC LICENSE Version 3, see LICENCE

using System;
using System.Diagnostics;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Logging;
using Dalamud.Plugin;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using PullLogger.Dalamud;
using PullLogger.Db;
using PullLogger.Events;
using PullLogger.Interface;
using PullLogger.Log;
using PullLogger.State;
using PullLogger.Ui;

namespace PullLogger;

[UsedImplicitly]
public sealed class Plogon : IDalamudPlugin
{
    private Configuration Configuration { get; }
    private Ui.Ui Ui { get; }
    private ConfigurationUi ConfigurationUi { get; }
    private Container Container { get; }
    public string Name => "PullLogger";
    private readonly DalamudPluginInterface _pluginInterface;

    public Plogon(
        DalamudPluginInterface pluginInterface,
        CommandManager commandManager,
        Framework framework,
        ClientState clientState,
        PartyList partyList,
        DataManager dataManager,
        Condition condition,
        ChatGui chat)
    {
        _pluginInterface = pluginInterface;
        Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(_pluginInterface.SavePluginConfig);

        var sw = new Stopwatch();
        Container = new Container();
        Container.Register(partyList);
        Container.Register(framework);
        Container.Register(clientState);
        Container.Register(condition);
        Container.Register(commandManager);
        Container.Register(Configuration);
        Container.Register(dataManager);
        Container.Register(pluginInterface.UiBuilder);
        Container.Register(chat);
        Container.Register(new StateData());

        Container.Register(new DbManager(Container, pluginInterface.GetPluginConfigDirectory()));
        Container.Register<IToaster>(new DalamudNotification(pluginInterface.UiBuilder));

        // you might normally want to embed resources and load them from the manifest stream
        // var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
        // var goatImage = PluginInterface.UiBuilder.LoadImage(imagePath);

        Container.RegisterDisposable(new EndViaDirectorUpdateHook());

        var teri = new TerritoryResolver(Container);
        teri.SaveAll();
        Container.Register<ITerritoryResolver>(teri);
        Container.Register<StateUpdater>();

        Ui = Container.RegisterDisposable<Ui.Ui>();
        DbUi = Container.Register<PullDbUi>();
        ConfigurationUi = Container.Register<ConfigurationUi>();
        Container.RegisterDisposable<Commands>();
        Container.Register<Csv>();
        Container.RegisterDisposable<Logger>();

        _pluginInterface.UiBuilder.Draw += DrawUi;
        _pluginInterface.UiBuilder.OpenConfigUi += DrawConfigUi;
    }

    private PullDbUi DbUi { get; set; }

    public void Dispose()
    {
        _pluginInterface.UiBuilder.Draw -= DrawUi;
        _pluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUi;
        Container.DoDispose();
    }

    private void DrawUi()
    {
        Ui.Draw();
        DbUi.Draw();
        ConfigurationUi.Draw();
    }

    private void DrawConfigUi()
    {
        ConfigurationUi.Visible = true;
    }
}