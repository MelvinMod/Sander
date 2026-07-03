using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Content.Client.StatusIcon;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusIcon.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Clyde;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using EntityManagerExt = Robust.Shared.GameObjects.EntityManagerExt;

namespace Sander.Overlays;

public class HealthBarOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private SharedTransformSystem _transformSystem = default!;
    private MobStateSystem _mobStateSystem = default!;
    private MobThresholdSystem _mobThresholdSystem = default!;
    private StatusIconSystem _statusIconSystem = default!;

    private static bool _useFastPath = true;
    private static bool _fastPathTested = false;
    
    private static Type? _cachedDamageableType;
    private static MethodInfo? _cachedTryGetMethod;
    private static FieldInfo? _cachedThresholdsField;
    private static bool _reflectionCacheInitialized = false;
    
    private int _frameSkipCounter = 0;
    private int _currentFrame = 0;
    private const int FRAME_SKIP = 1;

    public HealthBarOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = 100;
    }

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!SanderSearchState.HealthJobOverlayEnabled)
            return;

        _currentFrame++;
        _frameSkipCounter++;
        if (_frameSkipCounter < FRAME_SKIP)
            return;
        _frameSkipCounter = 0;

        if (_transformSystem == null)
            _transformSystem = _entityManager.System<SharedTransformSystem>();
        if (_mobStateSystem == null)
            _mobStateSystem = _entityManager.System<MobStateSystem>();
        if (_mobThresholdSystem == null)
            _mobThresholdSystem = _entityManager.System<MobThresholdSystem>();
        if (_statusIconSystem == null)
            _statusIconSystem = _entityManager.System<StatusIconSystem>();

        DrawingHandleWorld worldHandle = args.WorldHandle;
        var eye = args.Viewport.Eye;
        Angle angle = (eye != null) ? eye.Rotation : Angle.Zero;

        EntityQuery<TransformComponent> entityQuery = _entityManager.GetEntityQuery<TransformComponent>();
        
        Vector2 scale = new Vector2(1f, 1f);
        Matrix3x2 scaleMatrix = Matrix3Helpers.CreateScale(ref scale);
        Matrix3x2 rotationMatrix = Matrix3Helpers.CreateRotation(-angle);

        if (_useFastPath)
        {
            try
            {
                DrawFastPath(args, worldHandle, entityQuery, scaleMatrix, rotationMatrix);
                if (!_fastPathTested)
                {
                    _fastPathTested = true;
                }
                return;
            }
            catch (Exception ex)
            {
                _useFastPath = false;
                InitializeReflectionCache();
            }
        }

        DrawReflectionPath(args, worldHandle, entityQuery, scaleMatrix, rotationMatrix);
    }
    
    private static void InitializeReflectionCache()
    {
        if (_reflectionCacheInitialized) return;
        _reflectionCacheInitialized = true;
        
        try
        {
            _cachedDamageableType = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Content.Shared")
                ?.GetType("Content.Shared.Damage.Components.DamageableComponent");
            
            if (_cachedDamageableType != null)
            {
                _cachedTryGetMethod = typeof(IEntityManager).GetMethod("TryGetComponent", 
                    new[] { typeof(EntityUid), _cachedDamageableType.MakeByRefType() });
            }
        }
        catch { }
    }

    private void DrawFastPath(in OverlayDrawArgs args, DrawingHandleWorld worldHandle,
        EntityQuery<TransformComponent> entityQuery, Matrix3x2 scaleMatrix, Matrix3x2 rotationMatrix)
    {
        var query = _entityManager.AllEntityQueryEnumerator<DamageableComponent, MobStateComponent, SpriteComponent>();

        while (query.MoveNext(out EntityUid entityUid, out var damageableComponent, out var mobStateComponent, out var spriteComponent))
        {
            if (mobStateComponent.CurrentState == MobState.Dead) continue;
            if (!entityQuery.TryGetComponent(entityUid, out var transformComponent)) continue;
            if (transformComponent.MapID != args.MapId) continue;

            MobThresholdsComponent? thresholds = null;
            _entityManager.TryGetComponent(entityUid, out thresholds);

            var progressInfo = CalcProgressFast(entityUid, mobStateComponent, damageableComponent, thresholds);

            if (progressInfo.HasValue)
            {
                var (ratio, inCrit) = progressInfo.Value;
                DrawHealthBar(worldHandle, entityUid, spriteComponent, transformComponent, scaleMatrix, rotationMatrix, ratio, inCrit);
            }
        }

        Matrix3x2 identity = Matrix3x2.Identity;
        worldHandle.SetTransform(ref identity);
    }

    private void DrawReflectionPath(in OverlayDrawArgs args, DrawingHandleWorld worldHandle,
        EntityQuery<TransformComponent> entityQuery, Matrix3x2 scaleMatrix, Matrix3x2 rotationMatrix)
    {
        if (!_fastPathTested)
        {
            _fastPathTested = true;
        }

        var query = _entityManager.AllEntityQueryEnumerator<MobStateComponent, SpriteComponent>();

        while (query.MoveNext(out EntityUid entityUid, out var mobStateComponent, out var spriteComponent))
        {
            if (mobStateComponent.CurrentState == MobState.Dead) continue;
            if (!entityQuery.TryGetComponent(entityUid, out var transformComponent)) continue;
            if (transformComponent.MapID != args.MapId) continue;

            object? damageableComponent = null;
            if (_cachedDamageableType != null && _cachedTryGetMethod != null)
            {
                try
                {
                    var parameters = new object[] { entityUid, null };
                    var result = (bool)_cachedTryGetMethod.Invoke(_entityManager, parameters);
                    if (result)
                    {
                        damageableComponent = parameters[1];
                    }
                }
                catch { }
            }

            if (damageableComponent == null)
                continue;

            MobThresholdsComponent? thresholds = null;
            _entityManager.TryGetComponent(entityUid, out thresholds);

            var progressInfo = CalcProgressReflection(entityUid, mobStateComponent, damageableComponent, thresholds);

            if (progressInfo.HasValue)
            {
                var (ratio, inCrit) = progressInfo.Value;
                DrawHealthBar(worldHandle, entityUid, spriteComponent, transformComponent, scaleMatrix, rotationMatrix, ratio, inCrit);
            }
        }

        Matrix3x2 identity = Matrix3x2.Identity;
        worldHandle.SetTransform(ref identity);
    }

    private void DrawHealthBar(DrawingHandleWorld worldHandle, EntityUid entityUid, SpriteComponent spriteComponent,
        TransformComponent transformComponent, Matrix3x2 scaleMatrix, Matrix3x2 rotationMatrix, float ratio, bool inCrit)
    {
        StatusIconComponent? statusIconComponent = EntityManagerExt.GetComponentOrNull<StatusIconComponent>(_entityManager, entityUid);
        Box2 box = (statusIconComponent?.Bounds) ?? spriteComponent.Bounds;

        Vector2 worldPos = _transformSystem.GetWorldPosition(transformComponent);
        Matrix3x2 translationMatrix = Matrix3Helpers.CreateTranslation(worldPos);
        Matrix3x2 transformMatrix = Matrix3x2.Multiply(scaleMatrix, translationMatrix);
        transformMatrix = Matrix3x2.Multiply(rotationMatrix, transformMatrix);

        worldHandle.SetTransform(ref transformMatrix);

        float offsetX = 0f;
        float offsetY = 0f;
        float configWidth = 0f;
        float configHeight = 0f;
        
        float height = box.Height * 32f / 2f + offsetY;
        float width = configWidth > 0 ? configWidth : Math.Max(box.Width * 32f, 32f);
        Vector2 baseOffset = new Vector2(-width / 32f / 2f + offsetX / 32f, height / 32f);

        Color progressColor = GetProgressColor(ratio, inCrit);

        float barWidth = width - 8f;
        float filledWidth = barWidth * ratio + 8f;
        
        float barHeight = configHeight > 0 ? configHeight : 3f;
        
        Box2 bgBox = new Box2(new Vector2(8f, 0f) / 32f, new Vector2(barWidth + 8f, barHeight) / 32f);
        bgBox = bgBox.Translated(baseOffset);
        worldHandle.DrawRect(bgBox, Color.Black.WithAlpha(192), true);
        
        if (filledWidth > 8f)
        {
            Box2 fgBox = new Box2(new Vector2(8f, 0f) / 32f, new Vector2(filledWidth, barHeight) / 32f);
            fgBox = fgBox.Translated(baseOffset);
            worldHandle.DrawRect(fgBox, progressColor, true);
        }
        
        Box2 shadowBox = new Box2(new Vector2(8f, barHeight - 1f) / 32f, new Vector2(filledWidth, barHeight) / 32f);
        shadowBox = shadowBox.Translated(baseOffset);
        worldHandle.DrawRect(shadowBox, Color.Black.WithAlpha(128), true);
    }

    private (float ratio, bool inCrit)? CalcProgressFast(EntityUid uid, MobStateComponent comp, DamageableComponent damageableComp, MobThresholdsComponent? thresholds)
    {
        if (damageableComp == null)
            return null;

        float currentDamage = damageableComp.TotalDamage.Float();

        float critThreshold = 100f;
        float deadThreshold = 100f;

        if (thresholds != null)
        {
            try 
            {
                foreach (var kvp in thresholds.Thresholds)
                {
                    var state = kvp.Value;
                    var val = FixedPoint2.FromObject(kvp.Key).ToFloat();
                    
                    if (state == MobState.Critical) critThreshold = val;
                    else if (state == MobState.Dead) deadThreshold = val;
                }
            }
            catch { }
        }

        if (_mobStateSystem.IsAlive(uid, comp))
        {
            float ratio = 1f - (currentDamage / Math.Max(critThreshold, 0.1f));
            return (Math.Clamp(ratio, 0f, 1f), false);
        }
        else if (_mobStateSystem.IsCritical(uid, comp))
        {
            float critRange = deadThreshold - critThreshold;
            if (critRange <= 0.1f) return (0f, true);

            float ratio = 1f - ((currentDamage - critThreshold) / critRange);
            return (Math.Clamp(ratio, 0f, 1f), true);
        }
        
        return null;
    }

    private (float ratio, bool inCrit)? CalcProgressReflection(EntityUid uid, MobStateComponent comp, object damageableComp, MobThresholdsComponent? thresholds)
    {
        if (damageableComp == null)
            return null;

        float currentDamage = 0f;
        try 
        {
            PropertyInfo? prop = damageableComp.GetType().GetProperty("TotalDamage");
            if (prop != null)
            {
                var val = prop.GetValue(damageableComp);
                currentDamage = FixedPoint2.FromObject(val).ToFloat();
            }
            else
            {
                return null;
            }
        }
        catch { return null; }

        float critThreshold = 100f;
        float deadThreshold = 100f;

        if (thresholds != null)
        {
            try 
            {
                foreach (var kvp in thresholds.Thresholds)
                {
                    var state = kvp.Value;
                    var val = FixedPoint2.FromObject(kvp.Key).ToFloat();
                    
                    if (state == MobState.Critical) critThreshold = val;
                    else if (state == MobState.Dead) deadThreshold = val;
                }
            }
            catch { }
        }

        if (_mobStateSystem.IsAlive(uid, comp))
        {
            float ratio = 1f - (currentDamage / Math.Max(critThreshold, 0.1f));
            return (Math.Clamp(ratio, 0f, 1f), false);
        }
        else if (_mobStateSystem.IsCritical(uid, comp))
        {
            float critRange = deadThreshold - critThreshold;
            if (critRange <= 0.1f) return (0f, true);

            float ratio = 1f - ((currentDamage - critThreshold) / critRange);
            return (Math.Clamp(ratio, 0f, 1f), true);
        }
        
        return null;
    }

    public Color GetProgressColor(float progress, bool crit)
    {
        if (crit)
        {
            return Color.Red; 
        }
        
        if (progress > 0.5f)
        {
            return Color.InterpolateBetween(Color.Yellow, Color.LimeGreen, (progress - 0.5f) * 2f);
        }
        else
        {
            return Color.InterpolateBetween(Color.Red, Color.Yellow, progress * 2f);
        }
    }
}