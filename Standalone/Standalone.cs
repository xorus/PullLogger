using ImGuiScene;
using PullLogger;
using Standalone.Framework;

namespace Standalone;

internal sealed class Standalone : IPluginUiMock
{
    public static void Main(string[] args)
    {
        var container = new Container();
        container.Register(new AppConfig());
        container.Register<AssetReader>();
        UiBootstrap.Initialize(container, new Standalone(container));
    }

    private SimpleImGuiScene? _scene;
    private readonly NotPlogon _plogon;

    private Standalone(Container container)
    {
        _plogon = new NotPlogon(container);
    }

    public void Initialize(SimpleImGuiScene scene)
    {
        scene.OnBuildUI += Draw;
        _scene = scene;
    }


    public void Dispose()
    {
        _plogon.Dispose();
    }

    // the cookie cutter example said:
    //
    // > You COULD go all out here and make your UI generic and work on interfaces etc, and then
    // > mock dependencies and conceivably use exactly the same class in this testbed and the actual plugin
    // > That is, however, a bit excessive in general - it could easily be done for this sample, but I
    // > don't want to imply that is easy or the best way to go usually, so it's not done here either
    //
    // :-)
    private void Draw()
    {
        _plogon.Draw();
        if (_plogon.Exit) _scene!.ShouldQuit = true;
    }
}