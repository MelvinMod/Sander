using System;
using Content.Shared.Movement.Components;
using Content.Shared.Slippery;
using Content.Shared.StepTrigger.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Sander.Systems;

/// <summary>
/// Anti-slip system - prevents players from slipping on soap, ice, and other slippery surfaces.
/// Based on CerberusWareV3 implementation - simulates walking when near slippery surfaces.
/// </summary>
public sealed class SanderAntiSlipSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    private bool _wasPressingWalk;
    private bool _lastWalkState;

    public override void FrameUpdate(float frameTime)
    {
        if (!SanderSearchState.AntiSlipEnabled)
            return;

        var localEntity = _playerManager.LocalEntity;
        if (localEntity == null)
            return;

        var shouldPressWalk = ShouldPressWalk(localEntity.Value);
        var wasPressingWalk = shouldPressWalk != _lastWalkState;
        _lastWalkState = shouldPressWalk;
        
        if (wasPressingWalk)
        {
            PressWalk(_lastWalkState ? BoundKeyState.Down : BoundKeyState.Up);
        }
    }

    private void PressWalk(BoundKeyState state)
    {
        var localEntity = _playerManager.LocalEntity;
        if (localEntity == null)
            return;

        if (!CanSlip(localEntity.Value))
            return;

        var moverCoordinates = _transformSystem.GetMoverCoordinates(localEntity.Value);
        var screenCoordinates = _eyeManager.CoordinatesToScreen(moverCoordinates);
        
        var keyFunctionId = _inputManager.NetworkBindMap.KeyFunctionID(EngineKeyFunctions.Walk);
        var inputMsg = new ClientFullInputCmdMessage(_timing.CurTick, _timing.TickFraction, keyFunctionId)
        {
            State = state,
            Coordinates = moverCoordinates,
            ScreenCoordinates = screenCoordinates,
            Uid = EntityUid.Invalid
        };
        
        _inputSystem.HandleInputCommand(_playerManager.LocalSession, EngineKeyFunctions.Walk, inputMsg, false);
    }

    private bool ShouldPressWalk(EntityUid player)
    {
        // Check if player has shoes
        var hasShoes = _containerSystem.TryGetContainer(player, "shoes", out var shoesContainer) && 
                       shoesContainer.ContainedEntities.Count > 0;

        // Check for slippery entities nearby
        foreach (var entity in _entityLookup.GetEntitiesInRange(player, 1f))
        {
            if (!TryComp<StepTriggerComponent>(entity, out var stepTrigger) || !stepTrigger.Active)
                continue;

            var (walkSpeed, sprintSpeed) = GetPlayerSpeed(player);
            
            // Check if player is moving fast enough to slip
            if (sprintSpeed < stepTrigger.RequiredTriggeredSpeed || walkSpeed > stepTrigger.RequiredTriggeredSpeed)
                continue;

            // Check for slippery component
            if (!HasComp<SlipperyComponent>(entity))
                continue;

            // Check if it's glass shards (which don't slip you with shoes)
            if (TryComp<MetaDataComponent>(entity, out var metaData) && 
                metaData.EntityPrototype != null && 
                metaData.EntityPrototype.ID.Contains("ShardGlass"))
            {
                if (hasShoes)
                    continue;
            }

            return true;
        }

        return false;
    }

    private (float WalkSpeed, float SprintSpeed) GetPlayerSpeed(EntityUid player)
    {
        if (TryComp<MovementSpeedModifierComponent>(player, out var speedModifier))
        {
            return (speedModifier.CurrentWalkSpeed, speedModifier.CurrentSprintSpeed);
        }
        return (2.5f, 4.5f); // Default speeds
    }

    public bool CanSlip(EntityUid target)
    {
        // Check if entity has NoSlip component
        if (HasComp<NoSlipComponent>(target))
            return false;

        // Check shoes
        if (!_containerSystem.TryGetContainer(target, "shoes", out var shoesContainer) || 
            shoesContainer.ContainedEntities.Count <= 0)
            return true;

        var shoes = shoesContainer.ContainedEntities[0];
        return !HasComp<NoSlipComponent>(shoes);
    }
}
