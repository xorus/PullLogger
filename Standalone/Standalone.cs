using ImGuiScene;
using Standalone.Framework;
using Standalone.PullSource;

namespace Standalone;

internal sealed class Standalone : IPluginUiMock
{
    public static void Main(string[] args)
    {
        UiBootstrap.Initialize(new Standalone());
    }

    private SimpleImGuiScene? _scene;
    private readonly NotPlogon _plogon;

    private Standalone()
    {
        _plogon = new NotPlogon();
    }
    
    public void Initialize(SimpleImGuiScene scene)
    {
        // scene is a little different from what you have access to in dalamud
        // but it can accomplish the same things, and is really only used for initial setup here
        // eg, to load an image resource for use with ImGui
        scene.OnBuildUI += Draw;
        // saving this only so we can kill the test application by closing the window
        // (instead of just by hitting escape)
        _scene = scene;
    }


    public void Dispose()
    {
        // this.goatImage.Dispose();
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