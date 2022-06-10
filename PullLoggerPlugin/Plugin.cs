using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using PullLogger.Events;
using PullLogger.Log;

namespace PullLogger;

public sealed class Plugin : IDalamudPlugin
{
    public Plugin(
        DalamudPluginInterface pluginInterface,
        CommandManager commandManager,
        Framework framework,
        ClientState clientState,
        PartyList partyList,
        DataManager dataManager,
        Condition condition)
    {
        PluginInterface = pluginInterface;
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);

        Container = new Container();
        Container.Register(partyList);
        Container.Register(framework);
        Container.Register(clientState);
        Container.Register(condition);
        Container.Register(commandManager);
        Container.Register(Configuration);
        Container.Register(dataManager);

        // you might normally want to embed resources and load them from the manifest stream
        // var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
        // var goatImage = PluginInterface.UiBuilder.LoadImage(imagePath);

        Container.RegisterDisposable(new EndViaDirectorUpdateHook());
        Container.Register(new TerritoryResolver(Container));
        Container.RegisterDisposable(new State(Container));

        Ui = new Ui(Container);
        ConfigurationUi = new ConfigurationUi(Container);

        Container.RegisterDisposable(Ui);
        Container.Register(ConfigurationUi);
        Container.RegisterDisposable(new Commands(Container));

        Container.Register(new Csv());
        Container.RegisterDisposable(new Logger(Container));

        PluginInterface.UiBuilder.Draw += DrawUi;
        PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUi;
    }

    private DalamudPluginInterface PluginInterface { get; }
    private Configuration Configuration { get; }
    private Ui Ui { get; }
    private ConfigurationUi ConfigurationUi { get; }
    private Container Container { get; }
    public string Name => "PullLogger";

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= DrawUi;
        PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUi;
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