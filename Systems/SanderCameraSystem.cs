using System;
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
    private const int UpdateInterval = 2;
    private const float DefaultZoom = 1.0f;
    private float _currentZoom = DefaultZoom;
    private float _targetZoom = DefaultZoom;

    public override void Update(float frameTime)
    {
        _updateCounter++;
        if (_updateCounter < UpdateInterval)
            return;

        _updateCounter = 0;

        var localEntity = _playerManager.LocalEntity;
        if (localEntity == null)
            return;

        HandleZoom(frameTime);

        if (!_entityManager.TryGetComponent<EyeComponent>(localEntity, out var eyeComponent))
            return;

        ApplyVisualSettings(eyeComponent);
    }

    private void HandleZoom(float frameTime)
    {
        // FOV Extender takes priority (smaller, more comfortable FOV)
        if (SanderSearchState.FovExtenderEnabled && SanderSearchState.FovExtenderValue > 1.0f)
        {
            _targetZoom = SanderSearchState.FovExtenderValue;
        }
        // Extra FOV (bigger FOV)
        else if (SanderSearchState.FovEnabled && SanderSearchState.FovValue > 1.0f)
        {
            _targetZoom = SanderSearchState.FovValue;
        }
        else
        {
            _targetZoom = DefaultZoom;
        }

        if (Math.Abs(_currentZoom - _targetZoom) > 0.001f)
        {
            _currentZoom = _currentZoom + (_targetZoom - _currentZoom) * Math.Min(frameTime * 5f, 1f);
        }
    }

    private void ApplyVisualSettings(EyeComponent eyeComponent)
    {
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

        _lightManager.DrawShadows = SanderSearchState.ShadowsEnabled;

        // Use eyeComponent.Eye.Zoom like ArabicaCliento does
        if (eyeComponent.Eye != null)
        {
            eyeComponent.Eye.Zoom = new Vector2(_currentZoom, _currentZoom);
        }
    }

    public void ForceUpdate()
    {
        _updateCounter = UpdateInterval;
    }
}