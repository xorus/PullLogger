using System;
using ImGuiScene;

namespace Standalone.Framework;

internal interface IPluginUiMock :  IDisposable
{
    void Initialize(SimpleImGuiScene scene);
}