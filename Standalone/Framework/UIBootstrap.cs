using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using ImGuiScene;
using PullLogger;
using SDL2;

namespace Standalone.Framework;

internal static class UiBootstrap
{
    public static unsafe void Initialize(Container container, IPluginUiMock pluginUi)
    {
        var appConfig = container.Resolve<AppConfig>();
        var ass = container.Resolve<AssetReader>();
        var wci = new WindowCreateInfo
        {
            Title = "Pull Logger",
            Fullscreen = false,
            Width = 900,
            Height = 500,
            XPos = 50,
            YPos = 50,
        };

        if (appConfig.Fullscreen)
        {
            wci.Fullscreen = true;
            wci.TransparentColor = new float[] { 0, 0, 0 };
        }

        // you can edit this if you want more control over things
        // mainly if you want a regular window instead of transparent overlay
        // Typically you don't want to change any colors here if you keep the fullscreen overlay
        using var scene = new SimpleImGuiScene(RendererFactory.RendererBackend.DirectX11, wci);
        // the background color of your window - typically don't change this for fullscreen overlays
        scene.Renderer.ClearColor = new Vector4(0, 0, 0, 0);

        // this just makes the application quit if you hit escape
        scene.Window.OnSDLEvent += (ref SDL.SDL_Event sdlEvent) =>
        {
            if (sdlEvent.type == SDL.SDL_EventType.SDL_KEYDOWN &&
                sdlEvent.key.keysym.scancode == SDL.SDL_Scancode.SDL_SCANCODE_ESCAPE)
            {
                scene.ShouldQuit = true;
            }
        };

        // all of this is taken and adapted from dalamud 
        ImFontConfigPtr fontConfig = ImGuiNative.ImFontConfig_ImFontConfig();
        fontConfig.MergeMode = true;
        fontConfig.PixelSnapH = true;

        var fontSize = appConfig.FontSize;
        var fonts = ImGui.GetIO().Fonts;
        var fontPathJp = Path.Combine(ass.GetUIResDir().FullName, @"NotoSansCJKjp-Medium.otf");
        if (File.Exists(fontPathJp))
        {
            fonts.AddFontFromFileTTF(fontPathJp, fontSize, null, fonts.GetGlyphRangesJapanese());
        }

        var fontPathGame = Path.Combine(ass.GetUIResDir().FullName, @"gamesym.ttf");
        var allocated = new List<GCHandle>();
        if (File.Exists(fontPathGame))
        {
            var rangeHandle = GCHandle.Alloc(new ushort[] { 0xE020, 0xE0DB, 0 }, GCHandleType.Pinned);
            allocated.Add(rangeHandle);
            fonts.AddFontFromFileTTF(fontPathGame, fontSize, fontConfig, rangeHandle.AddrOfPinnedObject());
        }

        var fontPathIcon = Path.Combine(ass.GetUIResDir().FullName, @"FontAwesome5FreeSolid.otf");
        if (File.Exists(fontPathIcon))
        {
            var iconRangeHandle = GCHandle.Alloc(new ushort[] { 0xE000, 0xF8FF, 0, }, GCHandleType.Pinned);
            allocated.Add(iconRangeHandle);
            fontConfig.GlyphRanges = iconRangeHandle.AddrOfPinnedObject();
            fontConfig.PixelSnapH = true;
            fonts.AddFontFromFileTTF(fontPathIcon, fontSize, fontConfig);
        }

        var fontPathMono = Path.Combine(ass.GetUIResDir().FullName, "Inconsolata-Regular.ttf");
        if (File.Exists(fontPathMono))
        {
            fontConfig.GlyphRanges = IntPtr.Zero;
            fontConfig.PixelSnapH = true;
            fonts.AddFontFromFileTTF(fontPathMono, fontSize, fontConfig);
        }

        ImGui.GetIO().Fonts.Build();
        fontConfig.Destroy();

        foreach (var gcHandle in allocated) gcHandle.Free();

        // ImGui.GetStyle().GrabRounding = 3f;
        // ImGui.GetStyle().FrameRounding = 4f;
        // ImGui.GetStyle().WindowRounding = 4f;
        // ImGui.GetStyle().WindowBorderSize = 0f;
        // ImGui.GetStyle().WindowMenuButtonPosition = ImGuiDir.Right;
        // ImGui.GetStyle().ScrollbarSize = 16f;
        //
        // ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg] = new Vector4(0.06f, 0.06f, 0.06f, 0.87f);
        // ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg] = new Vector4(0.29f, 0.29f, 0.29f, 0.54f);
        // ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.54f, 0.54f, 0.54f, 0.40f);
        // ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.64f, 0.64f, 0.64f, 0.67f);
        // ImGui.GetStyle().Colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.29f, 0.29f, 0.29f, 1.00f);
        // ImGui.GetStyle().Colors[(int)ImGuiCol.CheckMark] = new Vector4(0.86f, 0.86f, 0.86f, 1.00f);
        // ImGui.GetStyle().Colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.54f, 0.54f, 0.54f, 1.00f);
        // ImGui.GetStyle().Colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.67f, 0.67f, 0.67f, 1.00f);
        // ImGui.GetStyle().Colors[(int)ImGuiCol.Button] = new Vector4(0.71f, 0.71f, 0.71f, 0.40f);
        // ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.47f, 0.47f, 0.47f, 1.00f);
        // ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.74f, 0.74f, 0.74f, 1.00f);
        // ImGui.GetStyle().Colors[(int)ImGuiCol.Header] = new Vector4(0.59f, 0.59f, 0.59f, 0.31f);
        // ImGui.GetStyle().Colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.50f, 0.50f, 0.50f, 0.80f);
        // ImGui.GetStyle().Colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.60f, 0.60f, 0.60f, 1.00f);
        // ImGui.GetStyle().Colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.79f, 0.79f, 0.79f, 0.25f);
        // ImGui.GetStyle().Colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.78f, 0.78f, 0.78f, 0.67f);
        // ImGui.GetStyle().Colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.88f, 0.88f, 0.88f, 0.95f);
        // ImGui.GetStyle().Colors[(int)ImGuiCol.Tab] = new Vector4(0.23f, 0.23f, 0.23f, 0.86f);
        // ImGui.GetStyle().Colors[(int)ImGuiCol.TabHovered] = new Vector4(0.71f, 0.71f, 0.71f, 0.80f);
        // ImGui.GetStyle().Colors[(int)ImGuiCol.TabActive] = new Vector4(0.36f, 0.36f, 0.36f, 1.00f);

        pluginUi.Initialize(scene);
        {
            scene.Run();
        }
        pluginUi.Dispose();
    }
}