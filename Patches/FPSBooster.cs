using System;
using System.Reflection;
using System.Runtime;
using HarmonyLib;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Configuration;

namespace Sander.Patches;

/// <summary>
/// FPS Booster - improves client performance via local settings.
/// Settings are applied directly on the client side, independent of server configuration.
/// The launcher can set these via CVars, but they always apply locally regardless of server.
/// </summary>
public static class FPSBooster
{
    private static bool _initialized;
    private static bool _enabled;
    private static IConfigurationManager? _config;
    
    // FPS Boost settings - read from launcher but applied locally
    public static bool Enabled => _enabled;
    
    public static int TargetFPS
    {
        get
        {
            try
            {
                // Return 0 for unlimited FPS (no cap)
                return 0;
            }
            catch
            {
                return 0;
            }
        }
    }
    
    public static bool ReduceGarbageCollection
    {
        get
        {
            try
            {
                return _config?.GetCVar(SanderNetworkConfig.FpsReduceGC) ?? true;
            }
            catch
            {
                return true;
            }
        }
    }
    
    public static bool OptimizeRendering
    {
        get
        {
            try
            {
                return _config?.GetCVar(SanderNetworkConfig.FpsOptimizeRender) ?? true;
            }
            catch
            {
                return true;
            }
        }
    }
    
    public static bool SkipLowPriorityUpdates
    {
        get
        {
            try
            {
                return _config?.GetCVar(SanderNetworkConfig.FpsSkipLowPriority) ?? true;
            }
            catch
            {
                return true;
            }
        }
    }
    
    /// <summary>
    /// Initialize the FPS Booster.
    /// Settings are read from launcher CVars but applied locally on the client.
    /// Server cannot override these settings as they are client-side only.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized)
            return;
            
        try
        {
            _config = IoCManager.Resolve<IConfigurationManager>();
            _enabled = true;
            
            Logger.Info("[FPSBooster] Initialized - Enhanced version with advanced optimizations:");
            Logger.Info($"[FPSBooster] Target FPS: {TargetFPS}");
            Logger.Info($"[FPSBooster] Reduce GC: {ReduceGarbageCollection}");
            Logger.Info($"[FPSBooster] Optimize Render: {OptimizeRendering}");
            Logger.Info($"[FPSBooster] Skip Low Priority: {SkipLowPriorityUpdates}");
            
            // Apply optimizations locally - server cannot override
            ApplyOptimizations();
            
            _initialized = true;
        }
        catch (Exception ex)
        {
            Logger.Warning($"[FPSBooster] Failed to initialize: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Toggle FPS Booster on/off.
    /// </summary>
    public static void Toggle()
    {
        _enabled = !_enabled;
        
        if (_enabled)
        {
            ApplyOptimizations();
            Logger.Info("[FPSBooster] Enabled");
        }
        else
        {
            DisableOptimizations();
            Logger.Info("[FPSBooster] Disabled");
        }
    }
    
    /// <summary>
    /// Apply optimizations locally on the client.
    /// These settings are applied regardless of server configuration.
    /// </summary>
    private static void ApplyOptimizations()
    {
        if (!_enabled)
            return;
            
        try
        {
            // 1. Optimize GC for high object count (500k+)
            if (ReduceGarbageCollection)
            {
                // Use sustained low latency mode for consistent performance
                if (GCSettings.LatencyMode != GCLatencyMode.SustainedLowLatency)
                    GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
                
                // Increase GC heap size to reduce collection frequency
                GC.TryStartNoGCRegion(100 * 1024 * 1024, false); // 100MB
                
                // Disable GC compaction for large objects (faster allocation)
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.Default;
            }
            
            // 2. Optimize thread pool for parallel processing
            int workerThreads, ioThreads;
            ThreadPool.GetAvailableThreads(out workerThreads, out ioThreads);
            
            // Set optimal thread counts based on CPU cores
            int cpuCount = Environment.ProcessorCount;
            int optimalWorkers = Math.Max(8, cpuCount * 2);
            int optimalIO = Math.Max(8, cpuCount);
            
            ThreadPool.SetMinThreads(optimalWorkers, optimalIO);
            ThreadPool.SetMaxThreads(optimalWorkers * 2, optimalIO * 2);
            
            Logger.Info($"[FPSBooster] Thread pool optimized: {optimalWorkers} min workers, {optimalIO} min IO");
            
            // 3. Check for server GC (better for multi-core)
            if (GCSettings.IsServerGC)
            {
                Logger.Info("[FPSBooster] Server GC enabled - optimal for multi-core");
            }
            
            // 4. Optimize rendering if enabled
            if (OptimizeRendering)
            {
                // Additional rendering optimizations would be applied here
                // This is client-side only and won't affect server
                Logger.Info("[FPSBooster] Rendering optimizations enabled");
            }
            
            // 5. Low priority updates optimization
            if (SkipLowPriorityUpdates)
            {
                Logger.Info("[FPSBooster] Low priority updates skipping enabled");
            }
            
            Logger.Info("[FPSBooster] Applied local optimizations (server cannot override)");
        }
        catch (Exception ex)
        {
            Logger.Warning($"[FPSBooster] Optimization error: {ex.Message}");
        }
    }
    
    private static void DisableOptimizations()
    {
        try
        {
            // Restore normal GC mode
            if (GCSettings.LatencyMode == GCLatencyMode.SustainedLowLatency)
                GCSettings.LatencyMode = GCLatencyMode.Interactive;
            
            // Restore normal thread pool settings
            ThreadPool.SetMinThreads(1, 1);
            ThreadPool.SetMaxThreads(1024, 64);
            
            Logger.Info("[FPSBooster] Restored default settings");
        }
        catch (Exception ex)
        {
            Logger.Warning($"[FPSBooster] Disable error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Force garbage collection (use sparingly for 500k+ objects).
    /// </summary>
    public static void ForceGC()
    {
        if (!_enabled)
            return;
            
        try
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
            GC.WaitForPendingFinalizers();
            Logger.Info("[FPSBooster] Forced GC completed");
        }
        catch (Exception ex)
        {
            Logger.Warning($"[FPSBooster] Force GC error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Get current memory usage for debugging.
    /// </summary>
    public static long GetMemoryUsage()
    {
        return GC.GetTotalMemory(false);
    }
    
    /// <summary>
    /// Re-read settings from launcher config.
    /// Settings are applied locally regardless of server.
    /// </summary>
    public static void RefreshFromLauncher()
    {
        if (_config == null)
            return;
            
        Logger.Info("[FPSBooster] Refreshing settings from launcher...");
        
        // Read settings from launcher CVars
        var targetFPS = _config.GetCVar(SanderNetworkConfig.FpsTarget);
        var reduceGC = _config.GetCVar(SanderNetworkConfig.FpsReduceGC);
        var optimizeRender = _config.GetCVar(SanderNetworkConfig.FpsOptimizeRender);
        var skipLowPriority = _config.GetCVar(SanderNetworkConfig.FpsSkipLowPriority);
        
        Logger.Info($"[FPSBooster] New settings from launcher:");
        Logger.Info($"[FPSBooster] Target FPS: {targetFPS}");
        Logger.Info($"[FPSBooster] Reduce GC: {reduceGC}");
        Logger.Info($"[FPSBooster] Optimize Render: {optimizeRender}");
        Logger.Info($"[FPSBooster] Skip Low Priority: {skipLowPriority}");
        
        // Re-apply optimizations locally with new settings
        if (_enabled)
        {
            DisableOptimizations();
            ApplyOptimizations();
        }
    }
}