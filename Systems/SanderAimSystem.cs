using System.Numerics;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Sander.Components;

namespace Sander.Systems;

public record struct AimOutput(MapCoordinates Position, Vector2? Velocity);

public sealed class SanderAimSystem : EntitySystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public AimOutput? GetClosestToEntInRange(
        EntityUid ent,
        float range,
        HashSet<EntityUid>? exclude = null)
    {
        var mapCords = _transform.GetMapCoordinates(Transform(ent));
        var entitiesInRange = _lookup.GetEntitiesInRange(mapCords, range, LookupFlags.Uncontained);
        if (exclude != null)
            entitiesInRange.ExceptWith(exclude);
        return GetClosestTo(mapCords, entitiesInRange);
    }

    public AimOutput? GetClosestInRange(
        ScreenCoordinates screenCoordinates,
        float range,
        HashSet<EntityUid>? exclude = null)
    {
        return GetClosestInRange(_eyeManager.PixelToMap(screenCoordinates), range, exclude);
    }

    public AimOutput? GetClosestInRange(
        MapCoordinates coordinates,
        float range,
        HashSet<EntityUid>? exclude = null)
    {
        var entitiesInRange = _lookup.GetEntitiesInRange(coordinates, range, LookupFlags.Uncontained);

        if (exclude != null)
            entitiesInRange.ExceptWith(exclude);

        return GetClosestTo(coordinates, entitiesInRange);
    }

    private AimOutput? GetClosestTo(MapCoordinates coordinates, HashSet<EntityUid> entities)
    {
        MapCoordinates? closestCoordinates = null;
        EntityUid? closestEntity = null;
        var closestDistance = float.MaxValue;

        foreach (var ent in entities)
        {
            var transform = Transform(ent);
            if (!FilterEntity((ent, transform)))
                continue;

            var entityMapPos = _transform.GetMapCoordinates(transform);
            var vector = coordinates.Position - entityMapPos.Position;
            var distance = vector.Length();

            if (!(distance < closestDistance)) continue;

            closestCoordinates = entityMapPos;
            closestDistance = distance;
            closestEntity = ent;
        }

        Vector2? velocity = null;
        if (TryComp<PhysicsComponent>(closestEntity, out var phys))
            velocity = phys.LinearVelocity;

        if (closestCoordinates is null)
            return null;

        return new AimOutput { Position = closestCoordinates.Value, Velocity = velocity };
    }

    private bool FilterEntity(Entity<TransformComponent> ent)
    {
        if (ent.Comp.MapID != _eyeManager.CurrentMap) return false;
        if (!TryComp(ent, out MobStateComponent? state))
            return false;
        if (state.CurrentState != MobState.Alive) return false;
        if (HasComp<SanderFriendComponent>(ent.Owner)) return false;
        return true;
    }
}