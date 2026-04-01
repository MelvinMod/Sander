using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Map;
using Content.Shared.Mobs.Components;
using Content.Shared.NukeOps;
using Content.Shared.Roles.Components;
using System.Collections.Generic;

namespace Sander.UI;

public enum CompassMarkerType
{
    Self,
    Player,
    NPC,
    Item,
    Coordinate,
    Syndicate,
    Pirate
}

public sealed class SanderCompassControl : Control
{
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private readonly Font _font;

    // Cache for performance - only update every few frames
    private readonly List<(EntityUid Entity, Vector2 ScreenPos, Color Color, string Name, CompassMarkerType MarkerType)> _cachedEntities = new();
    private int _frameCounter = 0;
    private const int CacheInterval = 25; // Update every 25 frames - significantly reduces lag

    // Last known player position
    private Vector2 _lastPlayerPos = Vector2.Zero;
    private bool _needsFullUpdate = true;

    // Detection range for compass
    private const float CompassRange = 25f;

    // Track last map
    private MapId _lastMapId = MapId.Nullspace;

    // Colors
    private static readonly Color SelfColor = new(0.1f, 0.1f, 0.6f, 1f);      // Dark blue
    private static readonly Color PlayerColor = new(0.2f, 0.8f, 0.2f, 1f);    // Green
    private static readonly Color NpcColor = new(1f, 0.5f, 0f, 1f);           // Orange
    private static readonly Color ItemColor = new(1f, 0.84f, 0.1f, 1f);       // Yellow
    private static readonly Color CoordinateColor = new(0.2f, 1f, 0.2f, 1f);  // Green
    private static readonly Color SyndicateColor = new(1f, 0.2f, 0.2f, 1f);   // Red
    private static readonly Color PirateColor = new(0.2f, 0.8f, 1f, 1f);      // Cyan

    public SanderCompassControl()
    {
        IoCManager.InjectDependencies(this);
        _font = new VectorFont(_resourceCache.GetResource<FontResource>("/Fonts/Boxfont-round/Boxfont Round.ttf"), 10);
        MinSize = new Vector2(240, 240);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var size = PixelSize;
        var center = new Vector2(size.X / 2f, size.Y / 2f);
        var radius = MathF.Min(size.X, size.Y) / 2f - 8f;

        // Background + ring
        handle.DrawCircle(center, radius + 1f, new Color(0.05f, 0.06f, 0.08f, 0.85f));
        handle.DrawCircle(center, radius, new Color(0f, 0f, 0f, 0.90f));
        handle.DrawCircle(center, radius, new Color(0.25f, 0.30f, 0.38f, 0.95f), filled: false);

        // Crosshair
        var grid = new Color(0.20f, 0.24f, 0.30f, 0.90f);
        handle.DrawLine(center - new Vector2(radius, 0), center + new Vector2(radius, 0), grid);
        handle.DrawLine(center - new Vector2(0, radius), center + new Vector2(0, radius), grid);

        // Heading arrow
        var rotMul = new Vector2(1, -1);
        var rotOfs = new Angle(-MathF.PI / 2f);
        var heading = _eye.CurrentEye.Rotation;

        var dir = (heading + rotOfs).ToVec() * rotMul;
        var tip = center + dir * (radius - 10f);
        var tail = center - dir * (radius * 0.25f);

        handle.DrawLine(tail, tip, Color.Cyan);

        var left = new Vector2(-dir.Y, dir.X);
        var headSize = 10f;
        handle.DrawLine(tip, tip - dir * headSize + left * (headSize * 0.6f), Color.Cyan);
        handle.DrawLine(tip, tip - dir * headSize - left * (headSize * 0.6f), Color.Cyan);

        // Draw self (dark blue dot in center)
        handle.DrawCircle(center, 6f, SelfColor);

        // Draw coordinate target (green dot)
        if (SanderSearchState.CoordsEnabled && SanderSearchState.CoordsValid)
        {
            var coordScreen = _eye.WorldToScreen(SanderSearchState.CoordsTarget);
            var coordOffset = coordScreen - center;
            var coordDist = coordOffset.Length();

            Vector2 drawPos;
            if (coordDist > radius - 25f)
            {
                drawPos = center + coordOffset.Normalized() * (radius - 25f);
            }
            else
            {
                drawPos = coordScreen;
            }

            handle.DrawCircle(drawPos, 5f, CoordinateColor);
        }

        // Get local player
        var localPlayer = _playerManager.LocalEntity;
        if (localPlayer == null) return;

        if (!_entityManager.TryGetComponent<TransformComponent>(localPlayer, out var playerTransform)) return;
        var playerPos = playerTransform.WorldPosition;
        var mapId = playerTransform.MapID;
        if (mapId == MapId.Nullspace) return;

        // Only update when player moved 3+ units
        var movedDistSq = (playerPos - _lastPlayerPos).LengthSquared();
        bool playerMoved = movedDistSq > 9f;

        _frameCounter++;
        if (_frameCounter >= CacheInterval || _lastMapId != mapId || playerMoved || _needsFullUpdate)
        {
            _frameCounter = 0;
            _lastMapId = mapId;
            _lastPlayerPos = playerPos;
            _needsFullUpdate = false;
            UpdateCachedEntities(playerPos, mapId);
        }

        // Draw entities
        foreach (var (entity, cachedScreenPos, color, name, markerType) in _cachedEntities)
        {
            var offset = cachedScreenPos - center;
            var distance = offset.Length();
            Vector2 drawPos;
            if (distance > radius - 25f)
            {
                offset = offset.Normalized() * (radius - 25f);
                drawPos = center + offset;
            }
            else
            {
                drawPos = cachedScreenPos;
            }

            if (markerType == CompassMarkerType.Item)
            {
                // Yellow triangle for items
                DrawTriangle(handle, drawPos, color);
            }
            else
            {
                // Dot for players/NPCs
                handle.DrawCircle(drawPos, 5f, color);
            }

            if (distance < radius - 35f && !string.IsNullOrEmpty(name))
            {
                var textPos = drawPos + new Vector2(8f, -4f);
                handle.DrawString(_font, textPos, name, color);
            }
        }
    }

    private void DrawTriangle(DrawingHandleScreen handle, Vector2 pos, Color color)
    {
        var size = 8f;
        var top = pos + new Vector2(0, -size);
        var bottomLeft = pos + new Vector2(-size * 0.8f, size * 0.6f);
        var bottomRight = pos + new Vector2(size * 0.8f, size * 0.6f);

        handle.DrawLine(top, bottomLeft, color);
        handle.DrawLine(bottomLeft, bottomRight, color);
        handle.DrawLine(bottomRight, top, color);
    }

    private void UpdateCachedEntities(Vector2 playerPos, MapId mapId)
    {
        _cachedEntities.Clear();

        var rangeSq = CompassRange * CompassRange;
        var localEntity = _playerManager.LocalEntity;

        var lookup = _entityManager.System<EntityLookupSystem>();
        var worldViewport = _eye.GetWorldViewport();

        try
        {
            foreach (var entity in lookup.GetEntitiesIntersecting(mapId, worldViewport))
            {
                if (entity == localEntity)
                    continue;

                if (!_entityManager.TryGetComponent<TransformComponent>(entity, out var transform))
                    continue;

                var worldPos = transform.WorldPosition;
                var distSq = (worldPos - playerPos).LengthSquared();
                if (distSq > rangeSq) continue;

                var hasMobState = _entityManager.HasComponent<MobStateComponent>(entity);

                Color color;
                string name = "";
                CompassMarkerType markerType;

                if (hasMobState)
                {
                    if (_entityManager.TryGetComponent<MetaDataComponent>(entity, out var meta))
                    {
                        name = meta.EntityName;
                    }

                    if (SanderSearchState.SyndicateEnabled && IsSyndicate(entity))
                    {
                        color = SyndicateColor;
                        markerType = CompassMarkerType.Syndicate;
                    }
                    else if (SanderSearchState.PirateEnabled && IsPirate(entity))
                    {
                        color = PirateColor;
                        markerType = CompassMarkerType.Pirate;
                    }
                    else
                    {
                        color = PlayerColor;
                        markerType = CompassMarkerType.Player;
                    }
                }
                else if (SanderSearchState.Enabled && !string.IsNullOrWhiteSpace(SanderSearchState.Query))
                {
                    if (!_entityManager.TryGetComponent<MetaDataComponent>(entity, out var itemMeta))
                        continue;

                    var entityName = itemMeta.EntityName;
                    if (entityName.Contains(SanderSearchState.Query, StringComparison.OrdinalIgnoreCase))
                    {
                        color = ItemColor;
                        markerType = CompassMarkerType.Item;
                        name = entityName;
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    continue;
                }

                var screenPos = _eye.WorldToScreen(worldPos);
                _cachedEntities.Add((entity, screenPos, color, name, markerType));
            }
        }
        catch
        {
            // Ignore lookup errors
        }
    }

    private bool IsSyndicate(EntityUid entity)
    {
        if (_entityManager.HasComponent<NukeOperativeComponent>(entity) ||
            _entityManager.HasComponent<TraitorRoleComponent>(entity) ||
            _entityManager.HasComponent<NukeopsRoleComponent>(entity))
        {
            return true;
        }
        return false;
    }

    private bool IsPirate(EntityUid entity)
    {
        if (_entityManager.TryGetComponent<MetaDataComponent>(entity, out var meta))
        {
            var name = meta.EntityName;
            if (name.Contains("pirate", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Pirate", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
