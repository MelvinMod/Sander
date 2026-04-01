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
    private int _frameCounter = 0;
    private const int ScanInterval = 10; // Scan for mobs every 10 frames

    // Trail settings
    private const float TrailDuration = 4.0f; // How long footprints last
    private const float MaxTrailDistance = 40f; // Max distance to track
    private const int MaxFootsteps = 50; // Max footprints to draw

    public SanderFootstepsOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = 200;
    }

    public override OverlaySpace Space => (OverlaySpace)2;

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

        // Draw footsteps
        DrawFootsteps(args.ScreenHandle);
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

        var lookup = _entityManager.System<Robust.Shared.GameObjects.EntityLookupSystem>();
        var worldViewport = _eyeManager.GetWorldViewport();
        var rangeSq = MaxTrailDistance * MaxTrailDistance;

        try
        {
            foreach (var entity in lookup.GetEntitiesIntersecting(mapId, worldViewport))
            {
                if (entity == localPlayer)
                    continue;

                // Check if it's a mob (player or NPC)
                if (!_entityManager.HasComponent<Content.Shared.Mobs.Components.MobStateComponent>(entity))
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

                // Check if this is a player or NPC (simplified - just check if has mob state)
                var isPlayer = _entityManager.HasComponent<MobStateComponent>(entity);

                // Add footstep at current position
                _footsteps.Add(new FootstepTrail
                {
                    Entity = entity,
                    WorldPosition = worldPos,
                    Name = name,
                    IsPlayer = isPlayer,
                    Timestamp = DateTime.UtcNow
                });

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

    private void DrawFootsteps(DrawingHandleScreen handle)
    {
        var currentTime = DateTime.UtcNow;
        var localPlayer = _playerManager.LocalEntity;
        Vector2? localPos = null;

        if (localPlayer != null && _entityManager.TryGetComponent<TransformComponent>(localPlayer, out var localTransform))
        {
            localPos = localTransform.WorldPosition;
        }

        foreach (var footstep in _footsteps)
        {
            var age = (currentTime - footstep.Timestamp).TotalSeconds;
            var fadeRatio = 1f - (float)(age / TrailDuration);

            if (fadeRatio <= 0f)
                continue;

            var screenPos = _eyeManager.WorldToScreen(footstep.WorldPosition);

            // Color based on entity type
            Color color;
            if (footstep.Entity == localPlayer)
            {
                color = new Color(0.2f, 0.4f, 1f, fadeRatio * 0.7f); // Blue for self
            }
            else if (footstep.IsPlayer)
            {
                color = new Color(0.2f, 0.8f, 0.2f, fadeRatio * 0.6f); // Green for players
            }
            else
            {
                color = new Color(1f, 0.5f, 0f, fadeRatio * 0.5f); // Orange for NPCs
            }

            // Draw footprint icon (small oval/foot shape)
            var size = 6f + (fadeRatio * 2f); // Slightly larger when fresh

            // Draw left foot
            var leftOffset = new Vector2(-3f, 0f);
            handle.DrawCircle(screenPos + leftOffset, size * 0.6f, color);

            // Draw right foot (slightly offset in time would be better, but simplified here)
            var rightOffset = new Vector2(3f, 0f);
            handle.DrawCircle(screenPos + rightOffset, size * 0.6f, color);

            // Draw name label for nearby footsteps
            if (localPos.HasValue)
            {
                var dist = (footstep.WorldPosition - localPos.Value).Length();
                if (dist < 15f)
                {
                    var labelPos = screenPos - new Vector2(0f, 12f);
                    // Simple color for text (white with fade)
                    var textColor = new Color(1f, 1f, 1f, fadeRatio * 0.8f);
                    // Note: We'd need a font to draw text, skipping for now
                }
            }
        }
    }

    private class FootstepTrail
    {
        public EntityUid Entity;
        public Vector2 WorldPosition;
        public string Name = "";
        public bool IsPlayer;
        public DateTime Timestamp;
    }
}
