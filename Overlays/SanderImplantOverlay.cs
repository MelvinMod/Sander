using System.Numerics;
using Content.Shared.Implants.Components;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using System.Collections.Generic;

namespace Sander.Overlays;

public sealed class SanderImplantOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private readonly Font _font;
    private EntityLookupSystem? _entityLookup;

    // Performance: cache entities with implants
    private readonly List<EntityUid> _entitiesWithImplants = new();
    private MapId _lastMapId = MapId.Nullspace;
    private int _frameCounter = 0;
    private const int CacheUpdateInterval = 20; // Update cache every 20 frames - much less lag
    private const float MaxDist = 18f;
    private Vector2 _lastPlayerPos = Vector2.Zero;

    public SanderImplantOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = 230;
        _font = new VectorFont(_resourceCache.GetResource<FontResource>("/Fonts/Boxfont-round/Boxfont Round.ttf"), 10);
    }

    public override OverlaySpace Space => (OverlaySpace)2;

    protected override void Draw(in OverlayDrawArgs args)
    {
        // Always update cache if implant features are enabled in any way
        if (!SanderSearchState.ImplantEnabled && !SanderSearchState.ImplantShowNames)
            return;

        _entityLookup ??= _entityManager.System<EntityLookupSystem>();
        var worldViewport = _eyeManager.GetWorldViewport();
        var camPos = _eyeManager.CurrentEye.Position.Position;

        // Check if player moved significantly
        var movedDistSq = (camPos - _lastPlayerPos).LengthSquared();
        bool playerMoved = movedDistSq > 4f;

        // Only update cache when needed - less lag
        _frameCounter++;
        if (_frameCounter >= CacheUpdateInterval || _lastMapId != args.MapId || playerMoved)
        {
            _frameCounter = 0;
            _lastMapId = args.MapId;
            _lastPlayerPos = camPos;
            UpdateEntityCache(args.MapId, worldViewport);
        }

        var maxDist2 = MaxDist * MaxDist;
        var color = new Color(SanderSearchState.ImplantColor).WithAlpha(180);

        // Draw implant markers for all cached entities
        foreach (var uid in _entitiesWithImplants)
        {
            if (!_entityManager.TryGetComponent(uid, out TransformComponent? xform))
                continue;

            if ((xform.WorldPosition - camPos).LengthSquared() > maxDist2)
                continue;

            if (!_entityManager.TryGetComponent(uid, out ContainerManagerComponent? containers))
                continue;

            if (!containers.Containers.TryGetValue(ImplanterComponent.ImplantSlotId, out var implantContainer))
                continue;

            if (implantContainer == null || implantContainer.ContainedEntities.Count == 0)
                continue;

            var screenPos = _eyeManager.WorldToScreen(xform.WorldPosition);

            // If implant overlay is disabled but implant info is enabled, show simple marker
            if (!SanderSearchState.ImplantEnabled && SanderSearchState.ImplantShowNames)
            {
                args.ScreenHandle.DrawString(_font, screenPos - new Vector2(0f, 10f), "[IMPL]", color);
                continue;
            }

            // Full implant display
            var lines = BuildImplantLines(uid, implantContainer.ContainedEntities);
            if (lines.Count == 0)
                continue;

            // Draw a vertical list above the entity.
            var drawPos = screenPos - new Vector2(0f, 18f + (lines.Count - 1) * 10f);
            for (var i = 0; i < lines.Count; i++)
            {
                args.ScreenHandle.DrawString(_font, drawPos + new Vector2(0f, i * 10f), lines[i], color);
            }
        }
    }

    private void UpdateEntityCache(MapId mapId, Box2 worldViewport)
    {
        _entitiesWithImplants.Clear();

        if (mapId == MapId.Nullspace)
            return;

        try
        {
            var entities = _entityLookup.GetEntitiesIntersecting(mapId, worldViewport);
            foreach (var uid in entities)
            {
                if (!_entityManager.TryGetComponent(uid, out ContainerManagerComponent? containers))
                    continue;

                if (!containers.Containers.TryGetValue(ImplanterComponent.ImplantSlotId, out var implantContainer))
                    continue;

                if (implantContainer != null && implantContainer.ContainedEntities.Count > 0)
                {
                    _entitiesWithImplants.Add(uid);
                }
            }
        }
        catch
        {
            // Ignore lookup errors
        }
    }

    private List<string> BuildImplantLines(EntityUid owner, IReadOnlyList<EntityUid> implants)
    {
        var result = new List<string>(4);
        if (implants.Count == 0)
            return result;

        HashSet<EntityUid>? visible = null;
        if (SanderSearchState.ImplantVisible.TryGetValue(owner, out var set))
            visible = set;

        var names = new List<string>(implants.Count);
        foreach (var implantUid in implants)
        {
            if (visible != null && !visible.Contains(implantUid))
                continue;

            if (_entityManager.TryGetComponent(implantUid, out MetaDataComponent? meta) &&
                !string.IsNullOrWhiteSpace(meta.EntityName))
            {
                names.Add(meta.EntityName);
            }
            else
            {
                names.Add("implant");
            }
        }

        if (!SanderSearchState.ImplantShowNames)
        {
            result.Add("[IMPL]");
            return result;
        }

        if (names.Count == 0)
        {
            result.Add("[IMPL]");
            return result;
        }

        const int maxLines = 4;
        var shown = 0;
        foreach (var name in names)
        {
            if (shown >= maxLines)
                break;

            result.Add($"[IMPL] {name}");
            shown++;
        }

        var remaining = names.Count - shown;
        if (remaining > 0)
            result.Add($"+{remaining} more");

        return result;
    }
}

