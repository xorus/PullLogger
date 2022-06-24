using System;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Logging;
using PullLogger.Log;

namespace PullLogger;

public sealed class Commands : IDisposable
{
    // private const string UiCommand = "/pplogui";
    private const string ConfigCommand = "/pplog";
    private const string Tab = "   ";
    private readonly string _prefix;

    public Commands(Container container)
    {
        Container = container;
        _prefix = $"[{container.Resolve<Plugin>().Name}] ";
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
        var chat = Container.Resolve<ChatGui>();

        switch (args.ToLower())
        {
            case "retcon":
                try
                {
                    Container.Resolve<Logger>().RetCon();
                    chat.Print(_prefix + "We'll pretend like nothing happened.");
                }
                catch (RetconError e)
                {
                    var message = "Could not remove last pull: " + e.Reason;
                    PluginLog.Error(message);
                    chat.PrintError(_prefix + message);
                }

                return;
            case "unretcon":
                try
                {
                    Container.Resolve<Logger>().UnRetCon();
                    chat.Print(_prefix + "We'll stop pretending like nothing happened.");
                }
                catch (RetconError e)
                {
                    var message = "Could not un-remove last pull: " + e.Reason;
                    PluginLog.Error(message);
                    chat.PrintError(_prefix + message);
                }

                return;
            default:
                Container.Resolve<ConfigurationUi>().Visible = true;
                break;
        }
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