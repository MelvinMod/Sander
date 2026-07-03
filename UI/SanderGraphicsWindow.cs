using System;
using System.Numerics;
using Hexa.NET.ImGui;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;

namespace Sander.UI;

public sealed class SanderGraphicsWindow
{
    private bool _open = true;
    private float _motionBlur;
    private float _bloom;
    private float _sharpness;
    private float _saturation;
    private float _contrast;
    private float _brightness;
    private bool _disableParticles;
    private bool _disableDecals;
    private bool _lowQualityLights;
    private static IConfigurationManager _cfg = default!;

    public bool IsActive => _open;

    public SanderGraphicsWindow()
    {
        _cfg = IoCManager.Resolve<IConfigurationManager>();
        _motionBlur = SanderGraphicsSettings.GetMotionBlur();
        _bloom = SanderGraphicsSettings.GetBloomIntensity();
        _sharpness = SanderGraphicsSettings.GetSharpness();
        _saturation = SanderGraphicsSettings.GetColorSaturation();
        _contrast = SanderGraphicsSettings.GetColorContrast();
        _brightness = SanderGraphicsSettings.GetColorBrightness();
        _disableParticles = SanderGraphicsSettings.GetDisableParticles();
        _disableDecals = SanderGraphicsSettings.GetDisableDecals();
        _lowQualityLights = SanderGraphicsSettings.GetLowQualityLights();
    }

    public void Render()
    {
        if (!_open)
            return;

        ImGui.SetNextWindowSize(new Vector2(420, 500), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Graphics Settings", ref _open, ImGuiWindowFlags.None))
        {
            if (ImGui.BeginTabBar("Tabs"))
            {
                if (ImGui.BeginTabItem("Presets"))
                {
                    if (ImGui.Button("Potato Mode"))
                    {
                        SanderGraphicsSettings.ApplyPotatoMode();
                        UpdateFromSettings();
                    }
                    if (ImGui.Button("Quality Mode"))
                    {
                        SanderGraphicsSettings.ApplyQualityMode();
                        UpdateFromSettings();
                    }
                    if (ImGui.Button("Reset"))
                    {
                        SanderGraphicsSettings.ResetToDefault();
                        UpdateFromSettings();
                    }
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Performance"))
                {
                    ImGui.Checkbox("Disable Particles", ref _disableParticles);
                    if (_disableParticles) SanderGraphicsSettings.SetDisableParticles(true);
                    ImGui.Checkbox("Disable Decals", ref _disableDecals);
                    if (_disableDecals) SanderGraphicsSettings.SetDisableDecals(true);
                    ImGui.Checkbox("Low Quality Lights", ref _lowQualityLights);
                    if (_lowQualityLights) SanderGraphicsSettings.SetLowQualityLights(true);
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Visual"))
                {
                    ImGui.SliderFloat("Motion Blur", ref _motionBlur, 0f, 1f);
                    SanderGraphicsSettings.ApplyMotionBlur(_motionBlur);
                    ImGui.SliderFloat("Bloom Intensity", ref _bloom, 0f, 3f);
                    SanderGraphicsSettings.ApplyBloom(_bloom);
                    ImGui.SliderFloat("Sharpness", ref _sharpness, 0f, 1f);
                    SanderGraphicsSettings.ApplySharpness(_sharpness);
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Color"))
                {
                    ImGui.SliderFloat("Saturation", ref _saturation, 0f, 2f);
                    ImGui.SliderFloat("Contrast", ref _contrast, 0.5f, 2f);
                    ImGui.SliderFloat("Brightness", ref _brightness, 0.5f, 2f);
                    SanderGraphicsSettings.ApplyColorSettings(_saturation, _contrast, _brightness);
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
        }
    }

    private void UpdateFromSettings()
    {
        _motionBlur = SanderGraphicsSettings.GetMotionBlur();
        _bloom = SanderGraphicsSettings.GetBloomIntensity();
        _sharpness = SanderGraphicsSettings.GetSharpness();
        _saturation = SanderGraphicsSettings.GetColorSaturation();
        _contrast = SanderGraphicsSettings.GetColorContrast();
        _brightness = SanderGraphicsSettings.GetColorBrightness();
        _disableParticles = SanderGraphicsSettings.GetDisableParticles();
        _disableDecals = SanderGraphicsSettings.GetDisableDecals();
        _lowQualityLights = SanderGraphicsSettings.GetLowQualityLights();
    }
}