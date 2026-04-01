using System.Globalization;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Sander.Overlays;

public sealed class SanderCoordinateOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IResourceCache _res = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private readonly Font _font;

    public SanderCoordinateOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = 225;
        _font = new VectorFont(_res.GetResource<FontResource>("/Fonts/Boxfont-round/Boxfont Round.ttf"), 10);
    }

    public override OverlaySpace Space => (OverlaySpace)2;

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!SanderSearchState.CoordsEnabled || !SanderSearchState.CoordsValid)
            return;

        var local = _player.LocalEntity;
        if (local == null)
            return;

        if (!_entMan.TryGetComponent(local.Value, out TransformComponent? xform))
            return;

        var fromWorld = xform.WorldPosition;
        var toWorld = SanderSearchState.CoordsTarget;
        var fromScreen = _eye.WorldToScreen(fromWorld);
        var toScreen = _eye.WorldToScreen(toWorld);

        // If target is off-screen, clamp arrow endpoint to screen bounds.
        var viewport = new Vector2(_ui.RootControl.PixelWidth, _ui.RootControl.PixelHeight);
        var bounds = UIBox2.FromDimensions(Vector2.Zero, viewport);
        var clamped = ClampToBounds(fromScreen, toScreen, bounds, 14f);

        var color = Color.Lime;
        args.ScreenHandle.DrawLine(fromScreen, clamped, color);
        DrawArrowHead(args.ScreenHandle, fromScreen, clamped, color);

        if (SanderSearchState.CoordsShowText)
        {
            var dist = (toWorld - fromWorld).Length();
            var label = $"COORDS: {FormatVec(toWorld)} ({dist:0.0}m)";
            args.ScreenHandle.DrawString(_font, clamped + new Vector2(8f, -8f), label, color);
        }
    }

    private static string FormatVec(Vector2 v)
        => string.Create(CultureInfo.InvariantCulture, $"{v.X:0.00}, {v.Y:0.00}");

    private static Vector2 ClampToBounds(Vector2 origin, Vector2 target, UIBox2 bounds, float margin)
    {
        // If already inside bounds, return target.
        var inner = new UIBox2(bounds.Left + margin, bounds.Bottom + margin, bounds.Right - margin, bounds.Top - margin);
        if (inner.Contains(target))
            return target;

        var dir = target - origin;
        if (dir.LengthSquared() < 0.0001f)
            return origin;

        // Ray-box intersection against inner bounds
        var tMin = float.NegativeInfinity;
        var tMax = float.PositiveInfinity;

        void Slab(float originC, float dirC, float min, float max)
        {
            if (MathF.Abs(dirC) < 0.00001f)
            {
                if (originC < min || originC > max)
                {
                    tMin = 1f;
                    tMax = 0f;
                }
                return;
            }

            var inv = 1f / dirC;
            var t0 = (min - originC) * inv;
            var t1 = (max - originC) * inv;
            if (t0 > t1) (t0, t1) = (t1, t0);
            tMin = MathF.Max(tMin, t0);
            tMax = MathF.Min(tMax, t1);
        }

        Slab(origin.X, dir.X, inner.Left, inner.Right);
        Slab(origin.Y, dir.Y, inner.Bottom, inner.Top);

        if (tMax < tMin || tMax < 0f)
            return new Vector2(
                MathHelper.Clamp(target.X, inner.Left, inner.Right),
                MathHelper.Clamp(target.Y, inner.Bottom, inner.Top));

        var t = tMin > 0f ? tMin : tMax;
        return origin + dir * t;
    }

    private static void DrawArrowHead(DrawingHandleScreen handle, Vector2 from, Vector2 to, Color color)
    {
        var dir = to - from;
        if (dir.LengthSquared() < 0.0001f)
            return;

        dir = Vector2.Normalize(dir);
        var left = new Vector2(-dir.Y, dir.X);
        var size = 10f;
        var tip = to;
        handle.DrawLine(tip, tip - dir * size + left * (size * 0.6f), color);
        handle.DrawLine(tip, tip - dir * size - left * (size * 0.6f), color);
    }
}

