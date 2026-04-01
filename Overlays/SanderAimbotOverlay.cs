using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;

namespace Sander.Overlays;

public sealed class SanderAimbotOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private EntityUid? _currentTarget;
    private Vector2 _mouseWorldPos;
    private bool _isReady = false;

    private const float AimbotRadius = 3f;

    public SanderAimbotOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = 300;
    }

    public override OverlaySpace Space => (OverlaySpace)2;

    protected override void FrameUpdate(FrameEventArgs args)
    {
        if (!SanderSearchState.GunAimbotEnabled && !SanderSearchState.MeleeAimbotEnabled)
        {
            _currentTarget = null;
            _isReady = false;
            return;
        }

        // Get mouse position in world
        var mouseScreenPos = _inputManager.MouseScreenPosition;
        var mouseMapCoords = _eyeManager.PixelToMap(mouseScreenPos);
        
        if (mouseMapCoords.MapId == MapId.Nullspace)
        {
            _currentTarget = null;
            _isReady = false;
            return;
        }

        _mouseWorldPos = mouseMapCoords.Position;

        var localPlayer = _playerManager.LocalEntity;
        if (localPlayer == null)
        {
            _currentTarget = null;
            _isReady = false;
            return;
        }

        _isReady = true;

        // Find target
        _currentTarget = FindTarget(localPlayer.Value, _mouseWorldPos);
    }

    private EntityUid? FindTarget(EntityUid player, Vector2 aimPos)
    {
        if (!_entityManager.TryGetComponent<TransformComponent>(player, out var playerTransform))
            return null;

        var mapId = playerTransform.MapID;
        if (mapId == MapId.Nullspace)
            return null;

        var lookup = _entityManager.System<Robust.Shared.GameObjects.EntityLookupSystem>();
        
        EntityUid? closestTarget = null;
        float closestDist = AimbotRadius * AimbotRadius;

        try
        {
            var viewport = _eyeManager.GetWorldViewport();
            foreach (var entity in lookup.GetEntitiesIntersecting(mapId, viewport))
            {
                if (entity == player)
                    continue;

                // Check if it's a valid target (mob, alive)
                if (!_entityManager.TryGetComponent<MobStateComponent>(entity, out var mobState))
                    continue;

                // Only target critters and humans (not dead)
                if (mobState.CurrentState == MobState.Dead)
                    continue;

                if (!_entityManager.TryGetComponent<TransformComponent>(entity, out var transform))
                    continue;

                var entityPos = transform.WorldPosition;
                var distToAim = (entityPos - aimPos).LengthSquared();

                if (distToAim < closestDist)
                {
                    closestDist = distToAim;
                    closestTarget = entity;
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return closestTarget;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_isReady || _currentTarget == null)
            return;

        if (!_entityManager.TryGetComponent<TransformComponent>(_currentTarget.Value, out var targetTransform))
            return;

        var targetPos = targetTransform.WorldPosition;
        var color = SanderSearchState.GunAimbotEnabled ? Color.Red : new Color(1f, 0.5f, 0f);

        // Draw target circle
        args.WorldHandle.DrawCircle(targetPos, 0.3f, color.WithAlpha(0.8f), false);

        // Draw line to target
        if (_entityManager.TryGetComponent<TransformComponent>(_playerManager.LocalEntity, out var playerTransform))
        {
            var playerPos = playerTransform.WorldPosition;
            args.WorldHandle.DrawLine(playerPos, targetPos, color.WithAlpha(0.5f));
        }

        // Draw aim circle around mouse
        args.WorldHandle.DrawCircle(_mouseWorldPos, AimbotRadius, color.WithAlpha(0.3f), false);
    }
}
