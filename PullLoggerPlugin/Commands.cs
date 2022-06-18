using System;
using Dalamud.Game.Command;
using Dalamud.Logging;
using PullLogger.Log;

namespace PullLogger;

public sealed class Commands : IDisposable
{
    // private const string UiCommand = "/pplogui";
    private const string ConfigCommand = "/pplog";
    private const string Tab = "   ";

    public Commands(Container container)
    {
        Container = container;
        CommandManager = container.Resolve<CommandManager>();
        CommandManager.AddHandler(ConfigCommand, new CommandInfo(OnPPLogCommand)
        {
            HelpMessage = "opens the configuration\n" +
                          Tab + ConfigCommand + " retcon → removes the last pull event from the log\n"
        });
        // CommandManager.AddHandler(UiCommand, new CommandInfo(OnPPLogUiCommand)
        // {
        //     HelpMessage = "shows the main ui according to the argument:\n" +
        //                   Tab + UiCommand + " show\n" +
        //                   Tab + UiCommand + " hide\n" +
        //                   Tab + UiCommand + " toggle"
        // });
    }

    private CommandManager CommandManager { get; }
    private Container Container { get; }

    public void Dispose()
    {
        // CommandManager.RemoveHandler(UiCommand);
        CommandManager.RemoveHandler(ConfigCommand);
    }

    private void OnPPLogCommand(string command, string args)
    {
        // in response to the slash command, just display our main ui
        // Ui.Visible = true;
        if (args == "retcon")
        {
            try
            {
                Container.Resolve<Logger>().RetCon();
            }
            catch (RetconError e)
            {
                PluginLog.Error("Could not remove last pull: " + e.Reason);
            }

            return;
        }

        Container.Resolve<ConfigurationUi>().Visible = true;
    }
    // private void OnPPLogUiCommand(string command, string args)
    // {
    //     var ui = Container.Resolve<Ui>();
    //     ui.Visible = args switch
    //     {
    //         "enable" => true,
    //         "disable" => false,
    //         "toggle" => !ui.Visible,
    //         _ => ui.Visible
    //     };
    //
    //     PluginLog.Information(command, args);
    // }
}