using System.Numerics;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Map;
using Content.Shared.Mobs.Components;

namespace Sander.Overlays;

public sealed class SanderFootstepsOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    // Footstep trail - stores recent positions with timestamps
    private readonly List<FootstepTrail> _footsteps = new();
    // Track last known positions to detect movement
    private readonly Dictionary<EntityUid, Vector2> _lastPositions = new();
    // Track last footstep position per entity to avoid too many footsteps
    private readonly Dictionary<EntityUid, Vector2> _lastFootstepPositions = new();
    private int _frameCounter = 0;
    private const int ScanInterval = 3; // Scan for mobs every 3 frames
    
    // Minimum distance between footsteps (to avoid lines/worms)
    private const float MinStepDistance = 0.8f;
    // Minimum distance to consider as movement (in world units)
    private const float MinMoveDistance = 0.5f;

    // Trail settings
    private const float TrailDuration = 2.5f; // How long footprints last
    private const float MaxTrailDistance = 15f; // Max distance to track (reduced to avoid extreme range warnings)
    private const int MaxFootsteps = 20; // Max footprints to draw

    // Footstep appearance
    private const float FootprintSize = 0.12f;
    private const float FootOffset = 0.15f;

    public SanderFootstepsOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = 200;
    }

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!SanderSearchState.FootstepsEnabled)
            return;

        _frameCounter++;

        var localPlayer = _playerManager.LocalEntity;
        if (localPlayer == null)
            return;

        // Clean up old footsteps
        var currentTime = DateTime.UtcNow;
        _footsteps.RemoveAll(f => (currentTime - f.Timestamp).TotalSeconds > TrailDuration);

        // Scan for mobs periodically
        if (_frameCounter >= ScanInterval)
        {
            _frameCounter = 0;
            ScanForMobs();
        }

        // Draw footsteps using world handle
        DrawFootsteps(args.WorldHandle);
    }

    private void ScanForMobs()
    {
        var localPlayer = _playerManager.LocalEntity;
        if (localPlayer == null)
            return;

        if (!_entityManager.TryGetComponent<TransformComponent>(localPlayer, out var playerTransform))
            return;

        var playerPos = playerTransform.WorldPosition;
        var mapId = playerTransform.MapID;
        if (mapId == MapId.Nullspace)
            return;

        var lookup = _entityManager.System<EntityLookupSystem>();
        var rangeSq = MaxTrailDistance * MaxTrailDistance;

        // Use a bounded box around the player instead of the full viewport to avoid extreme range warnings
        var searchBox = new Box2(playerPos - new Vector2(MaxTrailDistance), playerPos + new Vector2(MaxTrailDistance));

        try
        {
            foreach (var entity in lookup.GetEntitiesIntersecting(mapId, searchBox))
            {
                // Check if it's a mob (player or NPC)
                if (!_entityManager.HasComponent<MobStateComponent>(entity))
                    continue;

                if (!_entityManager.TryGetComponent<TransformComponent>(entity, out var transform))
                    continue;

                var worldPos = transform.WorldPosition;
                var distSq = (worldPos - playerPos).LengthSquared();
                if (distSq > rangeSq)
                    continue;

                // Get entity name
                string name = "Unknown";
                if (_entityManager.TryGetComponent<MetaDataComponent>(entity, out var meta))
                {
                    name = meta.EntityName;
                }

                // Check if entity has moved enough to create a new footstep
                bool shouldAddFootstep = false;
                if (_lastPositions.TryGetValue(entity, out var lastPos))
                {
                    var moveDist = (worldPos - lastPos).Length();
                    if (moveDist >= MinMoveDistance)
                    {
                        // Check if we've moved enough from the last footstep position
                        if (_lastFootstepPositions.TryGetValue(entity, out var lastFootstepPos))
                        {
                            var stepDist = (worldPos - lastFootstepPos).Length();
                            shouldAddFootstep = stepDist >= MinStepDistance;
                        }
                        else
                        {
                            shouldAddFootstep = true;
                        }
                    }
                }
                else
                {
                    // First time seeing this entity
                    shouldAddFootstep = true;
                }

                // Update last known position
                _lastPositions[entity] = worldPos;

                // Only add footstep if entity has moved enough
                if (shouldAddFootstep)
                {
                    // Update last footstep position
                    _lastFootstepPositions[entity] = worldPos;

                    _footsteps.Add(new FootstepTrail
                    {
                        Entity = entity,
                        WorldPosition = worldPos,
                        Name = name,
                        IsSelf = entity == localPlayer,
                        Timestamp = DateTime.UtcNow
                    });
                }

                // Limit total footsteps
                while (_footsteps.Count > MaxFootsteps)
                {
                    _footsteps.RemoveAt(0);
                }
            }
        }
        catch
        {
            // Ignore lookup errors
        }
    }

    private void DrawFootsteps(DrawingHandleWorld worldHandle)
    {
        var currentTime = DateTime.UtcNow;

        foreach (var footstep in _footsteps)
        {
            var age = (currentTime - footstep.Timestamp).TotalSeconds;
            var fadeRatio = 1f - (float)(age / TrailDuration);

            if (fadeRatio <= 0f)
                continue;

            var worldPos = footstep.WorldPosition;

            // Color based on entity type
            Color color;
            if (footstep.IsSelf)
            {
                color = new Color(0.2f, 0.4f, 1f, fadeRatio * 0.6f); // Blue for self
            }
            else
            {
                color = new Color(0.2f, 0.8f, 0.2f, fadeRatio * 0.5f); // Green for others
            }

            // Draw two small dots to represent left and right foot
            // Alternate which foot is forward based on position
            float sign = (worldPos.X * 10) % 2 > 1 ? 1f : -1f;
            
            // Left foot
            var leftPos = worldPos + new Vector2(-FootOffset, sign * FootOffset * 0.5f);
            worldHandle.DrawCircle(leftPos, FootprintSize * fadeRatio, color);
            
            // Right foot
            var rightPos = worldPos + new Vector2(FootOffset, -sign * FootOffset * 0.5f);
            worldHandle.DrawCircle(rightPos, FootprintSize * fadeRatio, color);
        }
    }

    private class FootstepTrail
    {
        public EntityUid Entity;
        public Vector2 WorldPosition;
        public string Name = "";
        public bool IsSelf;
        public DateTime Timestamp;
    }
}