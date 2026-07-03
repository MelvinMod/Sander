using Robust.Shared.Configuration;

namespace Sander;

public static class SanderNetworkConfig
{
    // FPS Booster CVars - CLIENTONLY, not replicated to server
    // These are purely client-side settings that server doesn't know about
    public static readonly CVarDef<int> FpsTarget = CVarDef.Create(
        "sander.fps.target",
        0,  // 0 = unlimited (no FPS cap)
        CVar.CLIENTONLY);

    public static readonly CVarDef<bool> FpsReduceGC = CVarDef.Create(
        "sander.fps.reduce_gc",
        true,
        CVar.CLIENTONLY);

    public static readonly CVarDef<bool> FpsOptimizeRender = CVarDef.Create(
        "sander.fps.optimize_render",
        true,
        CVar.CLIENTONLY);

    public static readonly CVarDef<bool> FpsSkipLowPriority = CVarDef.Create(
        "sander.fps.skip_low_priority",
        true,
        CVar.CLIENTONLY);

    public static readonly CVarDef<bool> FpsEnabled = CVarDef.Create(
        "sander.fps.enabled",
        true,
        CVar.CLIENTONLY);
}