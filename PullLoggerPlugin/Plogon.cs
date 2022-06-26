// PullLogger
// Copyright (C) 2022 Xorus
// GNU GENERAL PUBLIC LICENSE Version 3, see LICENCE
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Plugin;
using JetBrains.Annotations;
using PullLogger.Dalamud;
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
        Container.Register<IToaster>(new DalamudNotification(pluginInterface.UiBuilder));

        // you might normally want to embed resources and load them from the manifest stream
        // var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
        // var goatImage = PluginInterface.UiBuilder.LoadImage(imagePath);

        Container.RegisterDisposable(new EndViaDirectorUpdateHook());
        Container.Register<ITerritoryResolver>(new TerritoryResolver(Container));
        Container.Register(new StateData());
        Container.Register(new StateUpdater(Container));

        Ui = new Ui.Ui(Container);
        ConfigurationUi = new ConfigurationUi(Container);

        Container.RegisterDisposable(Ui);
        Container.Register(ConfigurationUi);
        Container.RegisterDisposable(new Commands(Container));

        Container.Register(new Csv());
        Container.RegisterDisposable(new Logger(Container));

        _pluginInterface.UiBuilder.Draw += DrawUi;
        _pluginInterface.UiBuilder.OpenConfigUi += DrawConfigUi;
    }

    public void Dispose()
    {
        _pluginInterface.UiBuilder.Draw -= DrawUi;
        _pluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUi;
        Container.DoDispose();
    }

    private void DrawUi()
    {
        Ui.Draw();
        ConfigurationUi.Draw();
    }

    private void DrawConfigUi()
    {
        ConfigurationUi.Visible = true;
    }
}