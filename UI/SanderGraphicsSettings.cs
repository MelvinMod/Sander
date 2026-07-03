using System;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Sander.UI;

/// <summary>
/// Custom graphics settings not available in the default game settings.
/// </summary>
public static class SanderGraphicsSettings
{
    private static IConfigurationManager _cfg = default!;
    private static IClyde _clyde = default!;

    // Custom CVars for graphics settings
    public static readonly CVarDef<float> MotionBlur = CVarDef.Create("sander.motion_blur", 0f, CVar.CLIENTONLY);
    public static readonly CVarDef<float> BloomIntensity = CVarDef.Create("sander.bloom_intensity", 1f, CVar.CLIENTONLY);
    public static readonly CVarDef<float> Sharpness = CVarDef.Create("sander.sharpness", 0f, CVar.CLIENTONLY);
    public static readonly CVarDef<float> ColorSaturation = CVarDef.Create("sander.color_saturation", 1f, CVar.CLIENTONLY);
    public static readonly CVarDef<float> ColorContrast = CVarDef.Create("sander.color_contrast", 1f, CVar.CLIENTONLY);
    public static readonly CVarDef<float> ColorBrightness = CVarDef.Create("sander.color_brightness", 1f, CVar.CLIENTONLY);
    public static readonly CVarDef<bool> DisableParticles = CVarDef.Create("sander.disable_particles", false, CVar.CLIENTONLY);
    public static readonly CVarDef<bool> DisableDecals = CVarDef.Create("sander.disable_decals", false, CVar.CLIENTONLY);
    public static readonly CVarDef<bool> LowQualityLights = CVarDef.Create("sander.low_quality_lights", false, CVar.CLIENTONLY);

    // New additional graphics settings
    public static readonly CVarDef<int> AntiAliasing = CVarDef.Create("sander.anti_aliasing", 0, CVar.CLIENTONLY); // 0=off, 1=FXAA, 2=MSAA
    public static readonly CVarDef<int> ViewDistance = CVarDef.Create("sander.view_distance", 15, CVar.CLIENTONLY);
    public static readonly CVarDef<float> UIScale = CVarDef.Create("sander.ui_scale", 1f, CVar.CLIENTONLY);
    public static readonly CVarDef<bool> VSync = CVarDef.Create("sander.vsync", true, CVar.CLIENTONLY);
    public static readonly CVarDef<int> TargetFPS = CVarDef.Create("sander.target_fps", 0, CVar.CLIENTONLY); // 0=unlimited

    public static void Initialize()
    {
        _cfg = IoCManager.Resolve<IConfigurationManager>();
        _clyde = IoCManager.Resolve<IClyde>();

        // Register custom CVars
        _cfg.RegisterCVar("sander.motion_blur", 0f, CVar.CLIENTONLY);
        _cfg.RegisterCVar("sander.bloom_intensity", 1f, CVar.CLIENTONLY);
        _cfg.RegisterCVar("sander.sharpness", 0f, CVar.CLIENTONLY);
        _cfg.RegisterCVar("sander.color_saturation", 1f, CVar.CLIENTONLY);
        _cfg.RegisterCVar("sander.color_contrast", 1f, CVar.CLIENTONLY);
        _cfg.RegisterCVar("sander.color_brightness", 1f, CVar.CLIENTONLY);
        _cfg.RegisterCVar("sander.disable_particles", false, CVar.CLIENTONLY);
        _cfg.RegisterCVar("sander.disable_decals", false, CVar.CLIENTONLY);
        _cfg.RegisterCVar("sander.low_quality_lights", false, CVar.CLIENTONLY);
        
        // Register new CVars
        _cfg.RegisterCVar("sander.anti_aliasing", 0, CVar.CLIENTONLY);
        _cfg.RegisterCVar("sander.view_distance", 15, CVar.CLIENTONLY);
        _cfg.RegisterCVar("sander.ui_scale", 1f, CVar.CLIENTONLY);
        _cfg.RegisterCVar("sander.vsync", true, CVar.CLIENTONLY);
        _cfg.RegisterCVar("sander.target_fps", 0, CVar.CLIENTONLY);
    }

    public static void ApplyMotionBlur(float value) => _cfg.SetCVar(MotionBlur, value);
    public static void ApplyBloom(float intensity) => _cfg.SetCVar(BloomIntensity, intensity);
    public static void ApplySharpness(float value) => _cfg.SetCVar(Sharpness, value);

    public static void ApplyColorSettings(float saturation, float contrast, float brightness)
    {
        _cfg.SetCVar(ColorSaturation, saturation);
        _cfg.SetCVar(ColorContrast, contrast);
        _cfg.SetCVar(ColorBrightness, brightness);
    }

    public static void SetDisableParticles(bool disabled) => _cfg.SetCVar(DisableParticles, disabled);
    public static void SetDisableDecals(bool disabled) => _cfg.SetCVar(DisableDecals, disabled);

    public static void SetLowQualityLights(bool lowQuality) => _cfg.SetCVar(LowQualityLights, lowQuality);

    // New setters
    public static void SetAntiAliasing(int level) => _cfg.SetCVar(AntiAliasing, level);
    public static void SetViewDistance(int distance) => _cfg.SetCVar(ViewDistance, Math.Clamp(distance, 5, 30));
    public static void SetUIScale(float scale) => _cfg.SetCVar(UIScale, Math.Clamp(scale, 0.5f, 2f));
    public static void SetVSync(bool enabled) => _cfg.SetCVar(VSync, enabled);
    public static void SetTargetFPS(int fps) => _cfg.SetCVar(TargetFPS, fps);

    // Preset configurations
    public static void ApplyPotatoMode()
    {
        _cfg.SetCVar(DisableParticles, true);
        _cfg.SetCVar(DisableDecals, true);
        _cfg.SetCVar(LowQualityLights, true);
        _cfg.SetCVar(MotionBlur, 0f);
        _cfg.SetCVar(BloomIntensity, 0f);
        _cfg.SetCVar(Sharpness, 0.5f);
        _cfg.SetCVar(AntiAliasing, 0);
        _cfg.SetCVar(ViewDistance, 8);
        _cfg.SetCVar(TargetFPS, 60);
    }

    public static void ApplyQualityMode()
    {
        _cfg.SetCVar(DisableParticles, false);
        _cfg.SetCVar(DisableDecals, false);
        _cfg.SetCVar(LowQualityLights, false);
        _cfg.SetCVar(MotionBlur, 0.5f);
        _cfg.SetCVar(BloomIntensity, 1.5f);
        _cfg.SetCVar(Sharpness, 0f);
        _cfg.SetCVar(AntiAliasing, 2);
        _cfg.SetCVar(ViewDistance, 20);
        _cfg.SetCVar(TargetFPS, 0); // Unlimited
    }

    public static void ApplyUltraMode()
    {
        _cfg.SetCVar(DisableParticles, false);
        _cfg.SetCVar(DisableDecals, false);
        _cfg.SetCVar(LowQualityLights, false);
        _cfg.SetCVar(MotionBlur, 0.8f);
        _cfg.SetCVar(BloomIntensity, 2f);
        _cfg.SetCVar(Sharpness, 0f);
        _cfg.SetCVar(AntiAliasing, 2);
        _cfg.SetCVar(ViewDistance, 30);
        _cfg.SetCVar(TargetFPS, 0); // Unlimited
    }

    public static void ResetToDefault()
    {
        _cfg.SetCVar(DisableParticles, false);
        _cfg.SetCVar(DisableDecals, false);
        _cfg.SetCVar(LowQualityLights, false);
        _cfg.SetCVar(MotionBlur, 0f);
        _cfg.SetCVar(BloomIntensity, 1f);
        _cfg.SetCVar(Sharpness, 0f);
        _cfg.SetCVar(ColorSaturation, 1f);
        _cfg.SetCVar(ColorContrast, 1f);
        _cfg.SetCVar(ColorBrightness, 1f);
    }

    // Getters
    public static float GetMotionBlur() => _cfg.GetCVar(MotionBlur);
    public static float GetBloomIntensity() => _cfg.GetCVar(BloomIntensity);
    public static float GetSharpness() => _cfg.GetCVar(Sharpness);
    public static float GetColorSaturation() => _cfg.GetCVar(ColorSaturation);
    public static float GetColorContrast() => _cfg.GetCVar(ColorContrast);
    public static float GetColorBrightness() => _cfg.GetCVar(ColorBrightness);
    public static bool GetDisableParticles() => _cfg.GetCVar(DisableParticles);
    public static bool GetDisableDecals() => _cfg.GetCVar(DisableDecals);
    public static bool GetLowQualityLights() => _cfg.GetCVar(LowQualityLights);
    
    // New getters
    public static int GetAntiAliasing() => _cfg.GetCVar(AntiAliasing);
    public static int GetViewDistance() => _cfg.GetCVar(ViewDistance);
    public static float GetUIScale() => _cfg.GetCVar(UIScale);
    public static bool GetVSync() => _cfg.GetCVar(VSync);
    public static int GetTargetFPS() => _cfg.GetCVar(TargetFPS);
}