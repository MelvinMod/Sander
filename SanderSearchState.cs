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

    // Per-entity implant toggles (set via right-click menu).
    // If an entity is present in this dictionary, ONLY implants in the set are shown (when ImplantShowNames = true).
    public static readonly Dictionary<EntityUid, HashSet<EntityUid>> ImplantVisible = new();

    // Coordinate arrow
    public static bool CoordsEnabled = false;
    public static bool CoordsShowText = true;
    public static string CoordsText = "";
    public static bool CoordsValid = false;
    public static Vector2 CoordsTarget = Vector2.Zero;

    // Syndicate / pirate detectors
    public static bool SyndicateEnabled = true;
    public static Vector4 SyndicateColor = new(1f, 0.2f, 0.2f, 1f);

    public static bool PirateEnabled = false;
    public static Vector4 PirateColor = new(0.4f, 0.8f, 1f, 1f);

    // Visual toggles
    public static bool FullbrightEnabled = false;
    public static bool ShadowsEnabled = true;
    public static bool FovEnabled = true;
    public static float FovValue = 1.2f;

    // Sound subtitles
    public static bool SoundSubtitlesEnabled = false;

    // Footsteps tracer
    public static bool FootstepsEnabled = false;

    // Aimbot settings
    public static bool GunAimbotEnabled = false;
    public static bool MeleeAimbotEnabled = false;

    // Friends list
    public static readonly HashSet<string> Friends = new();
}

