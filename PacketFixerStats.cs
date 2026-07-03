using System.Threading;

namespace Sander;

/// <summary>
/// Basic client-side stats tracker - no network packet tracking.
/// </summary>
public static class PacketFixerStats
{
    private static DateTime _startTime = DateTime.UtcNow;
    private static long _frameCount;

    public static TimeSpan Uptime => DateTime.UtcNow - _startTime;

    public static void RecordFrame()
    {
        Interlocked.Increment(ref _frameCount);
    }

    public static long FrameCount => Interlocked.Read(ref _frameCount);

    public static string GetStatsReport()
    {
        return $@"=== CLIENT STATS ===
Uptime: {Uptime:hh\:mm\:ss}
Frames: {_frameCount}";
    }

    public static void Reset()
    {
        _frameCount = 0;
        _startTime = DateTime.UtcNow;
    }
}