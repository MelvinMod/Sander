using System.Numerics;
using Hexa.NET.ImGui;
using Robust.Client.Graphics;
using Sander.UI;

namespace Sander.Overlays
{
    public sealed class SanderImGuiOverlay : Overlay
    {
        private SanderCompassWindow _compass = default!;
        private SanderGraphicsWindow _graphics = default!;
        private SanderInfoCheckerWindow _infoChecker = default!;
        private SanderMassSpecWindow _massSpec = default!;
        private SanderScannerWindow _scanner = default!;
        private SanderServerConsoleWindow _console = default!;
        private bool _mainOpen = true;
        private bool _compassOpen;
        private bool _graphicsOpen;
        private bool _infoCheckerOpen;
        private bool _massSpecOpen;
        private bool _scannerOpen;
        private bool _consoleOpen;

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (!ImGuiManager.IsInitialized)
                return;

            ImGuiManager.NewFrame(0.016f);

            _mainOpen = true;
            ImGui.SetNextWindowSize(new Vector2(380, 280), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(new Vector2(100, 100), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Sander Menu", ref _mainOpen, ImGuiWindowFlags.NoCollapse))
            {
                ImGui.TextColored(new Vector4(0.33f, 0.67f, 0.86f, 1f), "Sander Mod");
                ImGui.Separator();
                ImGui.Spacing();

                if (ImGui.Button("Compass"))
                    _compassOpen = !_compassOpen;
                if (ImGui.Button("Graphics"))
                    _graphicsOpen = !_graphicsOpen;
                if (ImGui.Button("Info Checker"))
                    _infoCheckerOpen = !_infoCheckerOpen;
                if (ImGui.Button("Mass Spec"))
                    _massSpecOpen = !_massSpecOpen;
                if (ImGui.Button("Scanner"))
                    _scannerOpen = !_scannerOpen;
                if (ImGui.Button("Console"))
                    _consoleOpen = !_consoleOpen;

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "Sander Mod v1.0");

                if (ImGui.BeginTabBar("Tabs"))
                {
                    if (_compassOpen && ImGui.BeginTabItem("Compass"))
                    {
                        _compass.Render();
                        ImGui.EndTabItem();
                    }
                    if (_graphicsOpen && ImGui.BeginTabItem("Graphics"))
                    {
                        _graphics.Render();
                        ImGui.EndTabItem();
                    }
                    if (_infoCheckerOpen && ImGui.BeginTabItem("Info"))
                    {
                        _infoChecker.Render();
                        ImGui.EndTabItem();
                    }
                    if (_massSpecOpen && ImGui.BeginTabItem("Mass Spec"))
                    {
                        _massSpec.Render();
                        ImGui.EndTabItem();
                    }
                    if (_scannerOpen && ImGui.BeginTabItem("Scanner"))
                    {
                        _scanner.Render();
                        ImGui.EndTabItem();
                    }
                    if (_consoleOpen && ImGui.BeginTabItem("Console"))
                    {
                        _console.Render();
                        ImGui.EndTabItem();
                    }
                    ImGui.EndTabBar();
                }
            }
            ImGui.End();

            if (!_compass.IsActive)
                _compass = new SanderCompassWindow();
            if (!_graphics.IsActive)
                _graphics = new SanderGraphicsWindow();
            if (!_infoChecker.IsActive)
                _infoChecker = new SanderInfoCheckerWindow();
            if (!_massSpec.IsActive)
                _massSpec = new SanderMassSpecWindow();
            if (!_scanner.IsActive)
                _scanner = new SanderScannerWindow();
            if (!_console.IsActive)
                _console = new SanderServerConsoleWindow();

            ImGuiManager.Render();
        }
    }
}
