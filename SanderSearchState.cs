using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.GameObjects;

namespace Sander;

public static class SanderSearchState
{
    public static bool Enabled = true;
    public static bool ShowNames = true;
    public static string Query = "disk";
    public static Vector4 Color = new(1f, 0.84f, 0.1f, 1f);

    public static bool ImplantEnabled = true;
    public static bool ImplantShowNames = false;
    public static Vector4 ImplantColor = new(1f, 0.41f, 0.71f, 1f);

    public static readonly Dictionary<EntityUid, HashSet<EntityUid>> ImplantVisible = new();

    public static bool CoordsEnabled = false;
    public static bool CoordsShowText = true;
    public static string CoordsText = "";
    public static bool CoordsValid = false;
    public static Vector2 CoordsTarget = Vector2.Zero;

    public static bool SyndicateEnabled = true;
    public static Vector4 SyndicateColor = new(1f, 0.2f, 0.2f, 1f);

    public static bool PirateEnabled = false;
    public static Vector4 PirateColor = new(0.4f, 0.8f, 1f, 1f);

    public static bool FullbrightEnabled = false;
    public static bool ShadowsEnabled = true;
    public static bool FovEnabled = true;
    public static float FovValue = 4.5f;
    public static bool FovExtenderEnabled = false;
    public static float FovExtenderValue = 2.0f;

    public static bool MoveMenuEnabled = false;

    public static bool SoundSubtitlesEnabled = false;

    public static bool HealthJobOverlayEnabled = true;

    public static bool FootstepsEnabled = false;

    public static bool GunAimbotEnabled = false;
    public static bool MeleeAimbotEnabled = false;
    public static float AimbotRadius = 4f;
    public static bool ShowDamageIndicator = true;

    public static bool FPSBoostEnabled = false;
    public static int FPSBoostLevel = 0;
    public static readonly string[] FPSBoostDescriptions = {
        "OFF",
        "Low (Shadows off, Simple particles)",
        "Medium (No shadows, Low quality)",
        "High (Maximum performance)"
    };

    // Ghost mode (client-side free camera)
    public static bool GhostModeEnabled = false;
    public static bool GhostReturnEnabled = true;

    // Mass spectrometer
    public static bool MassSpecEnabled = false;

    // Info checker
    public static bool InfoCheckerEnabled = true;

    // Anti-slip system
    public static bool AntiSlipEnabled = false;
}