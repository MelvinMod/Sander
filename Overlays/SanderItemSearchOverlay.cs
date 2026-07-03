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
    private readonly List<CachedItem> _cachedItems = new();
    private MapId _lastMapId = MapId.Nullspace;
    private int _frameCounter = 0;
    private const int CacheUpdateInterval = 15; // Update every 15 frames - much less lag
    private Vector2 _lastPlayerPos = Vector2.Zero;
    
    private const float MaxWorldDistance = 48f;
    private const int MaxCacheItems = 180;
    private const int MaxLineItems = 25;
    private const int MaxNameItems = 25;

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
            UpdateCache(mapId, worldViewport, playerWorldPos);
        }

        var color = new Color(SanderSearchState.Color);
        var maxDistSq = MaxWorldDistance * MaxWorldDistance;
        var drawLines = 0;
        var drawNames = 0;

        // Draw all cached items
        foreach (var item in _cachedItems)
        {
            if (item.DistanceSq > maxDistSq)
                continue;

            var screenPos = _eyeManager.WorldToScreen(item.WorldPos);

            if (drawLines < MaxLineItems)
            {
                var localScreen = _eyeManager.WorldToScreen(playerWorldPos);
                args.ScreenHandle.DrawLine(localScreen, screenPos, color);
                drawLines++;
            }

            if (SanderSearchState.ShowNames && drawNames < MaxNameItems)
            {
                args.ScreenHandle.DrawString(_font, screenPos - new Vector2(0f, 10f), item.Name, color);
                drawNames++;
            }

            if (drawLines >= MaxLineItems && (!SanderSearchState.ShowNames || drawNames >= MaxNameItems))
                break;
        }
    }

    private void UpdateCache(MapId mapId, Box2 worldViewport, Vector2 playerWorldPos)
    {
        _cachedItems.Clear();

        if (mapId == MapId.Nullspace)
            return;

        var query = SanderSearchState.Query;
        var maxDistSq = MaxWorldDistance * MaxWorldDistance;

        try
        {
            var entities = _entityLookup.GetEntitiesIntersecting(mapId, worldViewport);
            foreach (var uid in entities)
            {
                if (!_entityManager.TryGetComponent(uid, out MetaDataComponent? meta) ||
                    !_entityManager.TryGetComponent(uid, out TransformComponent? xform))
                    continue;

                var name = meta.EntityName;
                if (!name.Contains(query, StringComparison.OrdinalIgnoreCase))
                    continue;

                var worldPos = xform.WorldPosition;
                var distSq = (worldPos - playerWorldPos).LengthSquared();
                if (distSq > maxDistSq)
                    continue;

                _cachedItems.Add(new CachedItem(uid, worldPos, name, distSq));
            }

            _cachedItems.Sort(static (a, b) => a.DistanceSq.CompareTo(b.DistanceSq));

            if (_cachedItems.Count > MaxCacheItems)
                _cachedItems.RemoveRange(MaxCacheItems, _cachedItems.Count - MaxCacheItems);
        }
        catch
        {
            // Ignore lookup errors
        }
    }

    private readonly record struct CachedItem(EntityUid Uid, Vector2 WorldPos, string Name, float DistanceSq);
}

