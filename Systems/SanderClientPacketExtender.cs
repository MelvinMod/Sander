using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Sander.Systems;

/// <summary>
/// Client-side only initialization - no network changes.
/// All features are purely client-side rendering/UI optimizations.
/// </summary>
public static class SanderClientPacketExtender
{
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
            return;

        try
        {
            Logger.Info("[Sander] Client initialization complete");
            _initialized = true;
        }
        catch (Exception ex)
        {
            Logger.Error($"[Sander] Init failed: {ex.Message}");
        }
    }

    public static bool IsEnabled => _initialized;
}