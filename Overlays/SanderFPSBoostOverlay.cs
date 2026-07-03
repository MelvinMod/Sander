using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Sander.Overlays;

/// <summary>
/// FPS Booster overlay that displays FPS and provides a toggle button.
/// </summary>
public sealed class SanderFPSBoostOverlay : Overlay
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Font _font;
    private readonly Font _smallFont;
    
    private int _fps;
    private int _frameCount;
    private double _timeAccumulator;
    private bool _fpsBoostEnabled = true;
    private DateTime _lastUpdate = DateTime.UtcNow;

    public bool FPSBoostEnabled => _fpsBoostEnabled;

    public SanderFPSBoostOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = 500;
        _font = new VectorFont(_resourceCache.GetResource<FontResource>("/Fonts/Boxfont-round/Boxfont Round.ttf"), 14);
        _smallFont = new VectorFont(_resourceCache.GetResource<FontResource>("/Fonts/Boxfont-round/Boxfont Round.ttf"), 11);
    }

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    protected override void Draw(in OverlayDrawArgs args)
    {
        // Calculate FPS
        UpdateFPS();

        // Draw on screen
        DrawFPSOverlay(args.ScreenHandle, new Vector2(1920, 1080));
    }

    private void UpdateFPS()
    {
        _frameCount++;
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastUpdate).TotalSeconds;

        if (elapsed >= 1.0)
        {
            _fps = (int)(_frameCount / elapsed);
            _frameCount = 0;
            _lastUpdate = now;
        }
    }

    private void DrawFPSOverlay(DrawingHandleScreen handle, Vector2 viewportSize)
    {
        // Position: top-right corner
        var boxWidth = 140f;
        var boxHeight = 70f;
        var startX = viewportSize.X - boxWidth - 15f;
        var startY = 15f;

        // Draw semi-transparent background
        var bgColor = new Color(0.02f, 0.02f, 0.05f, 0.85f);
        handle.DrawRect(new UIBox2(startX, startY, startX + boxWidth, startY + boxHeight), bgColor);

        // Draw border
        var borderColor = _fpsBoostEnabled 
            ? new Color(0.2f, 0.9f, 0.4f, 0.6f)  // Green when enabled
            : new Color(0.9f, 0.4f, 0.2f, 0.6f); // Red when disabled
        handle.DrawRect(new UIBox2(startX, startY, startX + boxWidth, startY + boxHeight), borderColor, false);

        // Draw FPS text
        var fpsColor = GetFPSColor(_fps);
        handle.DrawString(_font, new Vector2(startX + 10f, startY + 8f), $"FPS: {_fps}", fpsColor);

        // Draw status
        var statusColor = _fpsBoostEnabled ? new Color(0.4f, 0.95f, 0.5f) : new Color(0.95f, 0.5f, 0.4f);
        var statusText = _fpsBoostEnabled ? "FPS+ ON" : "FPS+ OFF";
        handle.DrawString(_smallFont, new Vector2(startX + 10f, startY + 28f), statusText, statusColor);

        // Draw target info
        var infoColor = new Color(0.7f, 0.8f, 0.9f);
        handle.DrawString(_smallFont, new Vector2(startX + 10f, startY + 44f), "Press F10 to toggle", infoColor);

        // Draw status indicator dot
        var dotColor = _fpsBoostEnabled ? new Color(0.2f, 0.9f, 0.4f) : new Color(0.9f, 0.4f, 0.2f);
        handle.DrawCircle(new Vector2(startX + boxWidth - 15f, startY + 15f), 6f, dotColor);
    }

    private Color GetFPSColor(int fps)
    {
        if (fps >= 60)
            return new Color(0.3f, 0.9f, 0.4f); // Green
        if (fps >= 30)
            return new Color(0.9f, 0.8f, 0.3f); // Yellow
        return new Color(0.9f, 0.3f, 0.3f); // Red
    }

    public void ToggleFPSBoost()
    {
        _fpsBoostEnabled = !_fpsBoostEnabled;
        
        // Also toggle the FPSBooster
        try
        {
            Patches.FPSBooster.Toggle();
        }
        catch
        {
            // Ignore if not available
        }
    }
}
