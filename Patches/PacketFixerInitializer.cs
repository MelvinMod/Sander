using System;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Sander.Patches;

/// <summary>
/// Client-side only helper - no network changes visible to server.
/// All features are purely client-side rendering/UI optimizations.
/// </summary>
public static class PacketFixerInitializer
{
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
            return;

        try
        {
            _initialized = true;
            Logger.Info("[Sander] Client helper initialized");
        }
        catch (Exception ex)
        {
            Logger.Warning($"[Sander] Init failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Get current memory usage for debugging.
    /// </summary>
    public static long GetMemoryUsage()
    {
        return GC.GetTotalMemory(false);
    }
}