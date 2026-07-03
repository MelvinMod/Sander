using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Sander.Overlays;

public sealed class SanderDamageIndicatorOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private readonly List<DamageIndicator> _indicators = new();
    private readonly Random _random = new();
    private readonly Font _font;

    public SanderDamageIndicatorOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = 350;
        _font = new VectorFont(_resourceCache.GetResource<FontResource>("/Fonts/Boxfont-round/Boxfont Round.ttf"), 14);
    }

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public void AddDamageIndicator(Vector2 worldPosition, float damage, bool isCrit = false)
    {
        // Convert world position to screen position
        var eyeManager = IoCManager.Resolve<IEyeManager>();
        var screenPos = eyeManager.WorldToScreen(worldPosition);

        _indicators.Add(new DamageIndicator
        {
            ScreenPosition = screenPos,
            Damage = damage,
            IsCrit = isCrit,
            CreatedAt = DateTime.UtcNow,
            OffsetX = (_random.NextSingle() - 0.5f) * 40f,
            OffsetY = _random.NextSingle() * 20f
        });
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!SanderSearchState.ShowDamageIndicator)
            return;

        var currentTime = DateTime.UtcNow;
        var toRemove = new List<DamageIndicator>();
        var viewportSize = new Vector2(_ui.RootControl.PixelWidth, _ui.RootControl.PixelHeight);

        foreach (var indicator in _indicators)
        {
            var age = (float)(currentTime - indicator.CreatedAt).TotalSeconds;

            if (age > 1.5f)
            {
                toRemove.Add(indicator);
                continue;
            }

            // Calculate position with upward movement
            var yOffset = age * 60f;
            var drawPos = indicator.ScreenPosition + new Vector2(indicator.OffsetX, indicator.OffsetY - yOffset);

            // Check if in viewport
            if (drawPos.X < 0 || drawPos.X > viewportSize.X || drawPos.Y < 0 || drawPos.Y > viewportSize.Y)
                continue;

            // Calculate alpha (fade out after 1 second)
            float alpha;
            if (age < 0.1f)
                alpha = age / 0.1f;
            else if (age > 1.0f)
                alpha = (float)(1.0 - (age - 1.0) / 0.5);
            else
                alpha = 1f;

            if (alpha < 0.05f)
                continue;

            // Color based on damage amount
            var color = GetDamageColor(indicator.Damage, indicator.IsCrit);

            // Draw damage text
            var text = indicator.IsCrit ? $"{indicator.Damage:F0}!" : $"{indicator.Damage:F0}";

            // Draw shadow
            args.ScreenHandle.DrawString(_font, drawPos + new Vector2(1, 1), text, Color.Black.WithAlpha(alpha * 0.5f));

            // Draw main text
            args.ScreenHandle.DrawString(_font, drawPos, text, color.WithAlpha(alpha));
        }

        // Remove old indicators
        foreach (var indicator in toRemove)
        {
            _indicators.Remove(indicator);
        }
    }

    private Color GetDamageColor(float damage, bool isCrit)
    {
        if (isCrit)
            return new Color(1f, 0.2f, 0.2f); // Bright red for crits

        if (damage >= 50)
            return new Color(1f, 0.3f, 0.3f); // High damage - red
        if (damage >= 25)
            return new Color(1f, 0.6f, 0.2f); // Medium damage - orange
        if (damage >= 10)
            return new Color(1f, 0.9f, 0.2f); // Low damage - yellow
        return new Color(0.9f, 0.9f, 0.9f); // Very low - white
    }

    private class DamageIndicator
    {
        public Vector2 ScreenPosition;
        public float Damage;
        public bool IsCrit;
        public DateTime CreatedAt;
        public float OffsetX;
        public float OffsetY;
    }
}