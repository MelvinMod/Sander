using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Sander.Systems;

public sealed class SanderCameraSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly ILightManager _lightManager = default!;

    private int _updateCounter = 0;
    private const int UpdateInterval = 15;

    public override void Update(float frameTime)
    {
        _updateCounter++;
        if (_updateCounter < UpdateInterval)
            return;

        _updateCounter = 0;

        var localEntity = _playerManager.LocalEntity;
        if (localEntity == null)
            return;

        if (!_entityManager.TryGetComponent<EyeComponent>(localEntity, out var eyeComponent))
            return;

        ApplyVisualSettings(eyeComponent);
    }

    private void ApplyVisualSettings(EyeComponent eyeComponent)
    {
        // Fullbright - completely disable lighting
        if (SanderSearchState.FullbrightEnabled)
        {
            _lightManager.Enabled = false;
            _lightManager.DrawLighting = false;
            
            if (eyeComponent.Eye != null)
            {
                eyeComponent.Eye.DrawLight = false;
            }
        }
        else
        {
            _lightManager.Enabled = true;
            _lightManager.DrawLighting = true;
            
            if (eyeComponent.Eye != null)
            {
                eyeComponent.Eye.DrawLight = true;
            }
        }

        // Shadows - toggle shadow rendering
        _lightManager.DrawShadows = SanderSearchState.ShadowsEnabled;

        // FOV - toggle field of view using zoom
        if (eyeComponent.Eye != null)
        {
            if (SanderSearchState.FovEnabled)
            {
                var zoomFactor = 1f / SanderSearchState.FovValue;
                eyeComponent.Eye.Zoom = new Vector2(zoomFactor, zoomFactor);
            }
            else
            {
                eyeComponent.Eye.Zoom = new Vector2(1f / 1.2f, 1f / 1.2f);
            }
        }
    }

    public void ForceUpdate()
    {
        _updateCounter = UpdateInterval;
    }
}
