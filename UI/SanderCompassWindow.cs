using System.Numerics;
using Hexa.NET.ImGui;

namespace Sander.UI;

public sealed class SanderCompassWindow
{
    private bool _open = true;
    private bool _showLabels = true;
    private bool _showDistance = true;
    private bool _showIcons = true;
    private float _labelScale = 1f;
    private float _labelOpacity = 1f;
    private float _iconScale = 1f;
    private float _iconOpacity = 1f;
    private string _targetFilter = "";
    private string _selectedDir = "N";
    private float _compassRadius = 100f;
    private float _needleLength = 80f;
    private Vector4 _accent = new(0.33f, 0.67f, 0.86f, 1f);

    public bool IsActive => _open;

    public void Render()
    {
        if (!_open)
            return;

        ImGui.SetNextWindowSize(new Vector2(420, 380), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Compass", ref _open, ImGuiWindowFlags.NoCollapse))
        {
            if (ImGui.BeginTabBar("Tabs"))
            {
                if (ImGui.BeginTabItem("View"))
                {
                    ImGui.TextColored(_accent, "Compass Display");
                    ImGui.Separator();
                    ImGui.Checkbox("Show Labels", ref _showLabels);
                    ImGui.Checkbox("Show Distance", ref _showDistance);
                    ImGui.Checkbox("Show Icons", ref _showIcons);
                    ImGui.Spacing();
                    ImGui.Text("Target Filter:");
                    ImGui.InputText("##filter", ref _targetFilter, 64);
                    ImGui.Spacing();
                    ImGui.Text($"Selected: {_selectedDir}");
                    if (ImGui.Button("N"))
                        _selectedDir = "N";
                    ImGui.SameLine();
                    if (ImGui.Button("S"))
                        _selectedDir = "S";
                    ImGui.SameLine();
                    if (ImGui.Button("E"))
                        _selectedDir = "E";
                    ImGui.SameLine();
                    if (ImGui.Button("W"))
                        _selectedDir = "W";
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Settings"))
                {
                    ImGui.TextColored(_accent, "Compass Settings");
                    ImGui.Separator();
                    ImGui.SliderFloat("Label Scale", ref _labelScale, 0.5f, 2f);
                    ImGui.SliderFloat("Label Opacity", ref _labelOpacity, 0.1f, 1f);
                    ImGui.SliderFloat("Icon Scale", ref _iconScale, 0.5f, 2f);
                    ImGui.SliderFloat("Icon Opacity", ref _iconOpacity, 0.1f, 1f);
                    ImGui.SliderFloat("Compass Radius", ref _compassRadius, 50f, 200f);
                    ImGui.SliderFloat("Needle Length", ref _needleLength, 40f, 120f);
                    ImGui.ColorEdit4("Accent Color", ref _accent);
                    if (ImGui.Button("Reset"))
                    {
                        _labelScale = 1f; _labelOpacity = 1f;
                        _iconScale = 1f; _iconOpacity = 1f;
                        _compassRadius = 100f; _needleLength = 80f;
                        _accent = new Vector4(0.33f, 0.67f, 0.86f, 1f);
                    }
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Directions"))
                {
                    ImGui.TextColored(_accent, "Cardinal Directions");
                    ImGui.Separator();
                    foreach (var d in new[] { "N (North)", "S (South)", "E (East)", "W (West)", "NE", "NW", "SE", "SW" })
                    {
                        var key = d.Split(' ')[0];
                        if (ImGui.Selectable(d, _selectedDir == key))
                            _selectedDir = key;
                    }
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
        }
    }
}

