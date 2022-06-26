using System;
using PullLogger;
using PullLogger.Interface;
using PullLogger.State;
using PullLogger.Ui;
using Standalone.Framework;
using Standalone.Mocks;

namespace Standalone;

public sealed class NotPlogon : IDisposable
{
    private Configuration Configuration { get; }
    private ConfigurationUi ConfigurationUi { get; }
    private Container Container { get; }
    public string Name => "PullLogger";
    public bool Exit;

    public NotPlogon(Container container)
    {
        Container = container;
        var pluginInterface =  new MockPluginInterface(Container, Name);

        Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(pluginInterface.SavePluginConfig);

        Container.Register(Configuration);
        Container.Register(new StateData());
        Container.Register<IToaster>(new ConsoleNotification());
        Container.Register<ITerritoryResolver>(new TerritoryResolver());
        ConfigurationUi = Container.Register<ConfigurationUi>();
    }

    public void Draw()
    {
        ConfigurationUi.Visible = true;
        ConfigurationUi.Draw();

        Exit = !ConfigurationUi.Visible;
    }

    public void Dispose()
    {
        Container.DoDispose();
    }
}