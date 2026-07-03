using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Map;

namespace Sander.Systems;

public sealed class SanderGhostSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;

    private EntityUid? _originalPlayer;
    private Vector2 _originalPosition;
    private bool _isGhostMode = false;

    private Vector2 _cameraPosition;

    public void ToggleGhostMode()
    {
        if (_isGhostMode)
        {
            ReturnFromBody();
        }
        else
        {
            EnterGhostMode();
        }
    }

    private void EnterGhostMode()
    {
        var localPlayer = _playerManager.LocalEntity;
        if (localPlayer == null)
            return;

        _originalPlayer = localPlayer;
        if (_entityManager.TryGetComponent<TransformComponent>(localPlayer, out var transform))
        {
            _originalPosition = transform.WorldPosition;
        }

        _cameraPosition = _originalPosition;
        _isGhostMode = true;
        SanderSearchState.GhostModeEnabled = true;
    }

    public void ReturnFromBody()
    {
        if (!_isGhostMode || _originalPlayer == null)
            return;

        _isGhostMode = false;
        SanderSearchState.GhostModeEnabled = false;
        _originalPlayer = null;
    }

    public bool IsGhostModeActive() => _isGhostMode;
}