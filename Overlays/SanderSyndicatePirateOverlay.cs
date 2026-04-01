using System.Numerics;
using Content.Shared.NukeOps;
using Content.Shared.Roles.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Map;
using System.Collections.Generic;

namespace Sander.Overlays;

public sealed class SanderSyndicatePirateOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IResourceCache _res = default!;

    private readonly Font _font;
    private EntityLookupSystem? _lookup;

    // Performance: cache for entities with syndicate/pirate markers
    private readonly List<(EntityUid Uid, string Text, Color Color)> _cachedEntities = new();
    private MapId _lastMapId = MapId.Nullspace;
    private int _frameCounter = 0;
    private const int CacheUpdateInterval = 20; // Update every 20 frames - much less lag
    private const float MaxDist = 22f;
    private Vector2 _lastPlayerPos = Vector2.Zero;

    public SanderSyndicatePirateOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = 228;
        _font = new VectorFont(_res.GetResource<FontResource>("/Fonts/Boxfont-round/Boxfont Round.ttf"), 10);
    }

    public override OverlaySpace Space => (OverlaySpace)2;

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!SanderSearchState.SyndicateEnabled && !SanderSearchState.PirateEnabled)
            return;

        var local = _player.LocalEntity;
        if (local == null)
            return;

        _lookup ??= _entMan.System<EntityLookupSystem>();
        var worldViewport = _eye.GetWorldViewport();
        var camPos = _eye.CurrentEye.Position.Position;

        // Check if player moved significantly
        var movedDistSq = (camPos - _lastPlayerPos).LengthSquared();
        bool playerMoved = movedDistSq > 4f;

        // Only update cache when needed - less lag
        _frameCounter++;
        if (_frameCounter >= CacheUpdateInterval || _lastMapId != args.MapId || playerMoved)
        {
            _frameCounter = 0;
            _lastMapId = args.MapId;
            _lastPlayerPos = camPos;
            UpdateCache(args.MapId, worldViewport);
        }

        var maxDist2 = MaxDist * MaxDist;

        // Draw cached entities
        foreach (var (uid, text, color) in _cachedEntities)
        {
            if (!_entMan.TryGetComponent(uid, out TransformComponent? xform))
                continue;

            if ((xform.WorldPosition - camPos).LengthSquared() > maxDist2)
                continue;

            var screen = _eye.WorldToScreen(xform.WorldPosition);
            args.ScreenHandle.DrawString(_font, screen + new Vector2(0f, -34f), text, color.WithAlpha(190));
        }
    }

    private void UpdateCache(MapId mapId, Box2 worldViewport)
    {
        _cachedEntities.Clear();

        if (mapId == MapId.Nullspace)
            return;

        try
        {
            var entities = _lookup.GetEntitiesIntersecting(mapId, worldViewport);
            foreach (var uid in entities)
            {
                var label = GetLabel(uid);
                if (label != null)
                {
                    _cachedEntities.Add((uid, label.Value.Text, label.Value.Color));
                }
            }
        }
        catch
        {
            // Ignore lookup errors
        }
    }

    private (string Text, Color Color)? GetLabel(EntityUid uid)
    {
        // Syndicate: best-effort based on components that can exist client-side.
        if (SanderSearchState.SyndicateEnabled)
        {
            if (_entMan.HasComponent<NukeOperativeComponent>(uid) ||
                _entMan.HasComponent<TraitorRoleComponent>(uid) ||
                _entMan.HasComponent<NukeopsRoleComponent>(uid))
            {
                return ("[SYND]", new Color(SanderSearchState.SyndicateColor));
            }
        }

        // Pirate: name heuristic
        if (SanderSearchState.PirateEnabled)
        {
            if (_entMan.TryGetComponent(uid, out MetaDataComponent? meta) &&
                meta.EntityName.Contains("pirate", StringComparison.OrdinalIgnoreCase))
            {
                return ("[PIRATE]", new Color(SanderSearchState.PirateColor));
            }
        }

        return null;
    }
}

