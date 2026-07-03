using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Sander.Overlays;

/// <summary>
/// Hidden overlay - no visible debug info to avoid detection.
/// </summary>
public sealed class SanderPacketDebugOverlay : Overlay
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private readonly Font _font;
    private readonly Font _smallFont;

    public SanderPacketDebugOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = 400;
        _font = new VectorFont(_resourceCache.GetResource<FontResource>("/Fonts/Boxfont-round/Boxfont Round.ttf"), 11);
        _smallFont = new VectorFont(_resourceCache.GetResource<FontResource>("/Fonts/Boxfont-round/Boxfont Round.ttf"), 9);
    }

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    protected override void Draw(in OverlayDrawArgs args)
    {
        // No visible debug info - keeps mod hidden
    }
}