using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using System.Collections.Generic;

namespace Sander.Overlays;

public sealed class SanderItemSearchOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private readonly Font _font;
    private EntityLookupSystem? _entityLookup;

    // Performance: cache found entities
    private readonly List<(EntityUid Uid, Vector2 ScreenPos, string Name)> _cachedItems = new();
    private MapId _lastMapId = MapId.Nullspace;
    private int _frameCounter = 0;
    private const int CacheUpdateInterval = 15; // Update every 15 frames - much less lag
    private Vector2 _lastPlayerPos = Vector2.Zero;

    public SanderItemSearchOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = 220;
        _font = new VectorFont(_resourceCache.GetResource<FontResource>("/Fonts/Boxfont-round/Boxfont Round.ttf"), 10);
    }

    public override OverlaySpace Space => (OverlaySpace)2;

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!SanderSearchState.Enabled || string.IsNullOrWhiteSpace(SanderSearchState.Query))
            return;

        var local = _playerManager.LocalEntity;
        if (local == null)
            return;

        _entityLookup ??= _entityManager.System<EntityLookupSystem>();

        if (!_entityManager.TryGetComponent(local.Value, out TransformComponent? localXform))
            return;

        var mapId = localXform.MapID;
        var worldViewport = _eyeManager.GetWorldViewport();
        var playerWorldPos = localXform.WorldPosition;

        // Check if player moved significantly
        var movedDistSq = (playerWorldPos - _lastPlayerPos).LengthSquared();
        bool playerMoved = movedDistSq > 4f;

        // Only update cache when needed - less lag
        _frameCounter++;
        if (_frameCounter >= CacheUpdateInterval || _lastMapId != mapId || playerMoved)
        {
            _frameCounter = 0;
            _lastMapId = mapId;
            _lastPlayerPos = playerWorldPos;
            UpdateCache(mapId, worldViewport);
        }

        var localScreen = _eyeManager.WorldToScreen(playerWorldPos);
        var color = new Color(SanderSearchState.Color);

        // Draw all cached items
        foreach (var (uid, screenPos, name) in _cachedItems)
        {
            args.ScreenHandle.DrawLine(localScreen, screenPos, color);

            if (SanderSearchState.ShowNames)
                args.ScreenHandle.DrawString(_font, screenPos - new Vector2(0f, 10f), name, color);
        }
    }

    private void UpdateCache(MapId mapId, Box2 worldViewport)
    {
        _cachedItems.Clear();

        if (mapId == MapId.Nullspace)
            return;

        var queryLower = SanderSearchState.Query.ToLowerInvariant();

        try
        {
            var entities = _entityLookup.GetEntitiesIntersecting(mapId, worldViewport);
            foreach (var uid in entities)
            {
                if (!_entityManager.TryGetComponent(uid, out MetaDataComponent? meta) ||
                    !_entityManager.TryGetComponent(uid, out TransformComponent? xform))
                    continue;

                var name = meta.EntityName;
                if (!name.Contains(queryLower, StringComparison.OrdinalIgnoreCase))
                    continue;

                var screenPos = _eyeManager.WorldToScreen(xform.WorldPosition);
                _cachedItems.Add((uid, screenPos, name));
            }
        }
        catch
        {
            // Ignore lookup errors
        }
    }
}

