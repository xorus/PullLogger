using System;
using PullLogger;
using PullLogger.Db;
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
    private PullDbUi PullDbUi { get; }
    private Container Container { get; }
    public string Name => "PullLogger";
    public bool Exit;

    public NotPlogon(Container container)
    {
        Container = container;
        var notPi = new MockPluginInterface(Container, Name);

        Configuration = notPi.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(notPi.SavePluginConfig);
        Container.Register(new StateData());
        Container.Register(new DbManager(Container, notPi.GetPluginConfigDir()));
        Container.Register(Configuration);
        Container.Register<IToaster>(new ConsoleNotification());
        Container.Register<ITerritoryResolver>(new TerritoryResolver());
        ConfigurationUi = Container.Register<ConfigurationUi>();
        PullDbUi = Container.Register<PullDbUi>();
    }

    public void Draw()
    {
        ConfigurationUi.Visible = true;
        ConfigurationUi.Draw();
        PullDbUi.Visible = true;
        PullDbUi.Draw();

        Exit = !ConfigurationUi.Visible || !PullDbUi.Visible;
    }

    public void Dispose()
    {
        Container.DoDispose();
    }
}