using System;
using System.Numerics;
using Hexa.NET.ImGui;
using Robust.Client;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;

namespace Sander.Overlays
{
    public static class ImGuiManager
    {
        private static ImGuiContextPtr _context;
        private static bool _initialized;
        private static IUserInterfaceManager _uiManager = default!;

        public static bool IsInitialized => _initialized;

        public static void Initialize()
        {
            if (_initialized)
                return;

            _uiManager = IoCManager.Resolve<IUserInterfaceManager>();
            _context = ImGui.CreateContext();
            ImGui.SetCurrentContext(_context);

            var io = ImGui.GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

            var style = ImGui.GetStyle();
            style.WindowRounding = 4f;
            style.FrameRounding = 4f;
            style.GrabRounding = 3f;
            style.TabRounding = 4f;
            style.WindowPadding = new Vector2(8f, 8f);
            style.WindowTitleAlign = new Vector2(0.5f, 0.5f);
            style.ItemSpacing = new Vector2(8f, 4f);
            style.ItemInnerSpacing = new Vector2(6f, 4f);

            var c = style.Colors;
            c[(int)ImGuiCol.Text] = new Vector4(0.95f, 0.96f, 0.98f, 1f);
            c[(int)ImGuiCol.TextDisabled] = new Vector4(0.36f, 0.42f, 0.56f, 1f);
            c[(int)ImGuiCol.WindowBg] = new Vector4(0.06f, 0.06f, 0.06f, 0.94f);
            c[(int)ImGuiCol.ChildBg] = new Vector4(0f, 0f, 0f, 0f);
            c[(int)ImGuiCol.PopupBg] = new Vector4(0.08f, 0.08f, 0.08f, 0.94f);
            c[(int)ImGuiCol.Border] = new Vector4(0.19f, 0.19f, 0.19f, 0.79f);
            c[(int)ImGuiCol.BorderShadow] = new Vector4(0f, 0f, 0f, 0.24f);
            c[(int)ImGuiCol.FrameBg] = new Vector4(0.22f, 0.24f, 0.28f, 1f);
            c[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.26f, 0.32f, 0.4f, 1f);
            c[(int)ImGuiCol.FrameBgActive] = new Vector4(0.2f, 0.25f, 0.33f, 1f);
            c[(int)ImGuiCol.TitleBg] = new Vector4(0.04f, 0.04f, 0.04f, 1f);
            c[(int)ImGuiCol.TitleBgActive] = new Vector4(0.16f, 0.2f, 0.26f, 1f);
            c[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.1f, 0.1f, 0.12f, 1f);
            c[(int)ImGuiCol.MenuBarBg] = new Vector4(0.14f, 0.14f, 0.14f, 1f);
            c[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.02f, 0.02f, 0.02f, 1f);
            c[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.31f, 0.31f, 0.31f, 1f);
            c[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.41f, 0.41f, 0.41f, 1f);
            c[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.51f, 0.51f, 0.51f, 1f);
            c[(int)ImGuiCol.CheckMark] = new Vector4(0.33f, 0.67f, 0.86f, 1f);
            c[(int)ImGuiCol.SliderGrab] = new Vector4(0.33f, 0.67f, 0.86f, 1f);
            c[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.25f, 0.55f, 0.75f, 1f);
            c[(int)ImGuiCol.Button] = new Vector4(0.2f, 0.55f, 0.75f, 1f);
            c[(int)ImGuiCol.ButtonHovered] = new Vector4(0.25f, 0.65f, 0.85f, 1f);
            c[(int)ImGuiCol.ButtonActive] = new Vector4(0.18f, 0.5f, 0.68f, 1f);
            c[(int)ImGuiCol.Header] = new Vector4(0.2f, 0.45f, 0.6f, 1f);
            c[(int)ImGuiCol.HeaderHovered] = new Vector4(0.22f, 0.5f, 0.68f, 1f);
            c[(int)ImGuiCol.HeaderActive] = new Vector4(0.25f, 0.55f, 0.75f, 1f);
            c[(int)ImGuiCol.Separator] = new Vector4(0.19f, 0.19f, 0.19f, 0.5f);
            c[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.25f, 0.35f, 0.45f, 0.5f);
            c[(int)ImGuiCol.SeparatorActive] = new Vector4(0.2f, 0.5f, 0.7f, 1f);
            c[(int)ImGuiCol.Tab] = new Vector4(0.15f, 0.2f, 0.26f, 1f);
            c[(int)ImGuiCol.TabHovered] = new Vector4(0.25f, 0.35f, 0.45f, 1f);
            c[(int)ImGuiCol.PlotLines] = new Vector4(0.61f, 0.61f, 0.61f, 1f);
            c[(int)ImGuiCol.PlotLinesHovered] = new Vector4(1f, 0.43f, 0.35f, 1f);
            c[(int)ImGuiCol.PlotHistogram] = new Vector4(0.9f, 0.7f, 0f, 1f);
            c[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(1f, 0.6f, 0f, 1f);
            c[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.2f, 0.5f, 0.7f, 0.35f);

            _initialized = true;
        }

        public static void Shutdown()
        {
            if (_context != ImGuiContextPtr.Null)
            {
                ImGui.SetCurrentContext(_context);
                ImGui.DestroyContext(_context);
                _context = ImGuiContextPtr.Null;
            }
            _initialized = false;
        }

        public static void NewFrame(float deltaTime)
        {
            if (!_initialized)
                return;
            ImGui.SetCurrentContext(_context);
            var io = ImGui.GetIO();
            io.DeltaTime = deltaTime;
        }

        public static void Render()
        {
            if (!_initialized)
                return;
            ImGui.SetCurrentContext(_context);
            ImGui.Render();
        }
    }
}
