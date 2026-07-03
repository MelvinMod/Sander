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

    private readonly List<(EntityUid Entity, Vector2 Offset, float Distance, Color Color, string Name, CompassMarkerType MarkerType)> _cachedEntities = new();
    private int _frameCounter = 0;
    private const int CacheInterval = 15;

    private Vector2 _lastPlayerPos = Vector2.Zero;
    private bool _needsFullUpdate = true;

    private const float CompassRange = 40f; // Increased from 25f for wider FOV

    private MapId _lastMapId = MapId.Nullspace;

    private static readonly Color SelfColor = new(0.1f, 0.1f, 0.6f, 1f);
    private static readonly Color PlayerColor = new(0.2f, 0.8f, 0.2f, 1f);
    private static readonly Color NpcColor = new(1f, 0.5f, 0f, 1f);
    private static readonly Color ItemColor = new(1f, 0.84f, 0.1f, 1f);
    private static readonly Color CoordinateColor = new(0.2f, 1f, 0.2f, 1f);
    private static readonly Color SyndicateColor = new(1f, 0.2f, 0.2f, 1f);
    private static readonly Color PirateColor = new(0.2f, 0.8f, 1f, 1f);

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

        // Better looking compass background with gradient effect
        handle.DrawCircle(center, radius + 2f, new Color(0.03f, 0.04f, 0.06f, 0.90f));
        handle.DrawCircle(center, radius, new Color(0.05f, 0.06f, 0.09f, 0.95f));
        handle.DrawCircle(center, radius - 1f, new Color(0.10f, 0.12f, 0.16f, 1f), filled: false);
        
        // Draw outer ring
        handle.DrawCircle(center, radius, new Color(0.25f, 0.30f, 0.38f, 0.95f), filled: false);

        // Draw grid lines
        var grid = new Color(0.20f, 0.24f, 0.30f, 0.70f);
        handle.DrawLine(center - new Vector2(radius - 15f, 0), center + new Vector2(radius - 15f, 0), grid);
        handle.DrawLine(center - new Vector2(0, radius - 15f), center + new Vector2(0, radius - 15f), grid);

        // Draw diagonal grid lines for better visual
        var diagOffset = (radius - 15f) * 0.707f;
        handle.DrawLine(center - new Vector2(diagOffset, diagOffset), center + new Vector2(diagOffset, diagOffset), grid.WithAlpha(0.4f));
        handle.DrawLine(center - new Vector2(diagOffset, -diagOffset), center + new Vector2(diagOffset, -diagOffset), grid.WithAlpha(0.4f));

        // Draw FOV indicator (wider arc showing visible area)
        var heading = _eye.CurrentEye.Rotation;

        // Draw direction indicators (N, S, E, W)
        var directions = new (string Label, float Angle)[]
        {
            ("N", 0f),
            ("E", MathF.PI / 2f),
            ("S", MathF.PI),
            ("W", 3f * MathF.PI / 2f)
        };
        
        foreach (var (label, baseAngle) in directions)
        {
            var angle = baseAngle - (float)heading.Theta;
            var pos = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (radius - 12f);
            var dirColor = label == "N" ? Color.FromHex("#FF4444") : new Color(0.7f, 0.8f, 0.9f, 0.8f);
            handle.DrawString(_font, pos - new Vector2(4f, 5f), label, dirColor);
        }

        // Draw heading indicator (larger and more visible)
        var tip = center + heading.ToVec() * (radius - 12f);
        var tail = center - heading.ToVec() * (radius * 0.3f);

        handle.DrawLine(tail, tip, Color.Cyan);

        var perp = new Vector2(-MathF.Sin((float)heading.Theta), MathF.Cos((float)heading.Theta));
        var headSize = 12f;
        handle.DrawLine(tip, tip - heading.ToVec() * headSize + perp * (headSize * 0.7f), Color.Cyan);
        handle.DrawLine(tip, tip - heading.ToVec() * headSize - perp * (headSize * 0.7f), Color.Cyan);

        // Draw center self marker (larger)
        handle.DrawCircle(center, 8f, SelfColor);
        handle.DrawCircle(center, 5f, Color.Cyan);

        var localPlayer = _playerManager.LocalEntity;
        if (localPlayer == null) return;

        if (!_entityManager.TryGetComponent<TransformComponent>(localPlayer, out var playerTransform)) return;
        var playerPos = playerTransform.WorldPosition;
        var mapId = playerTransform.MapID;
        if (mapId == MapId.Nullspace) return;

        if (SanderSearchState.CoordsEnabled && SanderSearchState.CoordsValid)
        {
            var coordOffsetWorld = SanderSearchState.CoordsTarget - playerPos;
            var coordDist = coordOffsetWorld.Length();
            if (coordDist > 0.001f)
            {
                var offsetAngle = MathF.Atan2(coordOffsetWorld.Y, coordOffsetWorld.X);
                var relativeAngle = offsetAngle - (float)heading.Theta;
                var dirVec = new Vector2(MathF.Cos(relativeAngle), MathF.Sin(relativeAngle));

                var t = coordDist / CompassRange;
                var markerRadius = MathF.Min(radius - 25f, t * (radius - 25f));

                var drawPos = center + dirVec.Normalized() * markerRadius;
                handle.DrawCircle(drawPos, 5f, CoordinateColor);
            }
        }

        var movedDistSq = (playerPos - _lastPlayerPos).LengthSquared();
        bool playerMoved = movedDistSq > 4f;

        _frameCounter++;
        if (_frameCounter >= CacheInterval || _lastMapId != mapId || playerMoved || _needsFullUpdate)
        {
            _frameCounter = 0;
            _lastMapId = mapId;
            _lastPlayerPos = playerPos;
            _needsFullUpdate = false;
            UpdateCachedEntities(playerPos, mapId);
        }

        foreach (var (entity, cachedOffsetWorld, cachedDistance, color, name, markerType) in _cachedEntities)
        {
            var offsetAngle = MathF.Atan2(cachedOffsetWorld.Y, cachedOffsetWorld.X);
            var relativeAngle = offsetAngle - (float)heading.Theta;
            var dirVec = new Vector2(MathF.Cos(relativeAngle), MathF.Sin(relativeAngle));

            var t = cachedDistance / CompassRange;
            var markerRadius = t * (radius - 30f);
            markerRadius = MathF.Min(radius - 30f, MathF.Max(0f, markerRadius));

            var drawPos = center + dirVec.Normalized() * markerRadius;

            // Draw line from center to entity (with fade effect)
            handle.DrawLine(center + dirVec.Normalized() * 15f, drawPos, color.WithAlpha(0.3f));

            if (markerType == CompassMarkerType.Item)
            {
                DrawTriangle(handle, drawPos, color);
            }
            else
            {
                // Draw larger, more visible markers
                var markerSize = cachedDistance < 15f ? 7f : 5f;
                handle.DrawCircle(drawPos, markerSize, color);
            }

            // Show name and distance for entities within reasonable range
            if (markerRadius < radius - 40f && !string.IsNullOrEmpty(name))
            {
                var textPos = drawPos + new Vector2(10f, -4f);
                handle.DrawString(_font, textPos, name, color);
                
                // Show distance
                var distStr = $"{cachedDistance:F0}m";
                var distPos = textPos + new Vector2(0f, 11f);
                handle.DrawString(_font, distPos, distStr, color.WithAlpha(0.7f));
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

                var offset = worldPos - playerPos;
                var distance = MathF.Sqrt(distSq);
                _cachedEntities.Add((entity, offset, distance, color, name, markerType));
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