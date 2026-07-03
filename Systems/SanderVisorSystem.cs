using Content.Client.Overlays;
using Content.Shared.Overlays;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Sander.Systems;

/// <summary>
/// Makes the game think the player is wearing a visor with health bar and job info.
/// This adds the ShowHealthBarsComponent to the player entity.
/// </summary>
public sealed class SanderVisorSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public void EnableVisorMode()
    {
        var localPlayer = _playerManager.LocalEntity;
        if (localPlayer == null)
            return;

        var player = localPlayer.Value;

        // Add ShowHealthBarsComponent to enable health bar overlay
        if (!_entityManager.HasComponent<ShowHealthBarsComponent>(player))
        {
            _entityManager.AddComponent<ShowHealthBarsComponent>(player);
        }
    }

    public void DisableVisorMode()
    {
        var localPlayer = _playerManager.LocalEntity;
        if (localPlayer == null)
            return;

        var player = localPlayer.Value;

        // Remove ShowHealthBarsComponent to disable health bar overlay
        if (_entityManager.HasComponent<ShowHealthBarsComponent>(player))
        {
            _entityManager.RemoveComponent<ShowHealthBarsComponent>(player);
        }
    }

    public bool IsVisorModeEnabled()
    {
        var localPlayer = _playerManager.LocalEntity;
        if (localPlayer == null)
            return false;

        return _entityManager.HasComponent<ShowHealthBarsComponent>(localPlayer.Value);
    }
}