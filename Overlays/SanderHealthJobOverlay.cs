using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Sander.Overlays;

public sealed class SanderHealthJobOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private const float MaxRenderDistance = 15f; // Reduced to avoid extreme range warnings
    private int _currentFrame;
    private const int CacheUpdateInterval = 5;

    public SanderHealthJobOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = 100;
    }

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!SanderSearchState.HealthJobOverlayEnabled)
            return;

        var localEntity = _playerManager.LocalEntity;
        if (localEntity == null)
            return;

        _currentFrame++;

        var worldHandle = args.WorldHandle;

        if (!_entityManager.TryGetComponent<TransformComponent>(localEntity, out var playerTransform))
            return;

        var playerPos = playerTransform.WorldPosition;
        var mapId = playerTransform.MapID;
        if (mapId == MapId.Nullspace)
            return;

        var lookup = _entityManager.System<EntityLookupSystem>();

        // Use a bounded box around the player instead of the full viewport to avoid extreme range warnings
        var searchBox = new Box2(playerPos - new Vector2(MaxRenderDistance), playerPos + new Vector2(MaxRenderDistance));

        try
        {
            foreach (var entity in lookup.GetEntitiesIntersecting(mapId, searchBox))
            {
                if (entity == localEntity)
                    continue;

                // Only show for entities with MobStateComponent (players and NPCs)
                if (!_entityManager.HasComponent<MobStateComponent>(entity))
                    continue;

                if (!_entityManager.TryGetComponent<TransformComponent>(entity, out var transform))
                    continue;

                var entityPos = transform.WorldPosition;
                var dist = (entityPos - playerPos).Length();
                if (dist > MaxRenderDistance)
                    continue;

                // Draw a simple health bar above the entity
                DrawHealthBar(worldHandle, entityPos, entity);
            }
        }
        catch
        {
            // Ignore lookup errors
        }
    }

    private void DrawHealthBar(DrawingHandleWorld worldHandle, Vector2 worldPos, EntityUid entity)
    {
        const float barWidth = 1.5f;
        const float barHeight = 0.15f;
        const float offset = 1.2f;

        var barPos = worldPos + new Vector2(0, offset);

        // Background (dark)
        worldHandle.DrawRect(
            new Box2(barPos - new Vector2(barWidth / 2, barHeight / 2),
                     barPos + new Vector2(barWidth / 2, barHeight / 2)),
            new Color(0, 0, 0, 0.8f));

        // Get entity name if available
        string name = "";
        if (_entityManager.TryGetComponent<MetaDataComponent>(entity, out var meta))
        {
            name = meta.EntityName ?? "";
        }

        // Determine health color based on mob state
        Color healthColor;
        if (_entityManager.TryGetComponent<MobStateComponent>(entity, out var mobState))
        {
            // Use the CurrentState property
            var state = mobState.CurrentState;
            if (state == Content.Shared.Mobs.MobState.Dead)
            {
                healthColor = Color.Gray;
            }
            else if (state == Content.Shared.Mobs.MobState.Critical)
            {
                healthColor = Color.Red;
            }
            else
            {
                healthColor = Color.Green;
            }
        }
        else
        {
            healthColor = Color.Green;
        }

        // Draw health fill (full bar for alive, empty for dead)
        float fillRatio = healthColor == Color.Gray ? 0f : 1f;
        var fillWidth = (barWidth - 0.1f) * fillRatio;
        
        if (fillWidth > 0)
        {
            worldHandle.DrawRect(
                new Box2(
                    barPos + new Vector2(-barWidth / 2 + 0.05f, -barHeight / 2 + 0.05f),
                    barPos + new Vector2(-barWidth / 2 + fillWidth + 0.05f, barHeight / 2 - 0.05f)),
                healthColor);
        }
    }

    private IEyeManager _eyeManager => IoCManager.Resolve<IEyeManager>();
}