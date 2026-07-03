using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Audio.Components;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Map;

namespace Sander.Overlays;

public sealed class SanderSoundSubtitleOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private readonly Font _font;

    private readonly List<ActiveSubtitle> _activeSubtitles = new();
    private int _frameCounter = 0;
    private const int CacheUpdateInterval = 3;

    private const float MaxSoundDistance = 50f;
    private const float SubtitleLifetime = 2.0f;
    private const float FadeDuration = 0.3f;

    // Track which entities have already triggered a subtitle recently
    private readonly Dictionary<EntityUid, DateTime> _recentSounds = new();
    private const float CooldownTime = 2.0f;

    public SanderSoundSubtitleOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = 250;
        _font = new VectorFont(_resourceCache.GetResource<FontResource>("/Fonts/Boxfont-round/Boxfont Round.ttf"), 13);
    }

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!SanderSearchState.SoundSubtitlesEnabled)
            return;

        _frameCounter++;

        var localPlayer = _playerManager.LocalEntity;
        if (localPlayer == null)
            return;

        if (_frameCounter >= CacheUpdateInterval)
        {
            _frameCounter = 0;
            UpdateSoundSources(localPlayer.Value);
        }

        UpdateAndDrawSubtitles(args);
    }

    private void UpdateSoundSources(EntityUid localPlayer)
    {
        if (!_entityManager.TryGetComponent<TransformComponent>(localPlayer, out var playerTransform))
            return;

        var playerPos = playerTransform.WorldPosition;
        var mapId = playerTransform.MapID;
        if (mapId == MapId.Nullspace)
            return;

        var currentTime = DateTime.UtcNow;

        // Clean up old entries
        var toRemove = _recentSounds.Where(kvp => (currentTime - kvp.Value).TotalSeconds > CooldownTime).Select(kvp => kvp.Key).ToList();
        foreach (var key in toRemove)
            _recentSounds.Remove(key);

        // Remove expired subtitles
        _activeSubtitles.RemoveAll(s => (currentTime - s.CreatedAt).TotalSeconds > SubtitleLifetime + FadeDuration);

        var lookup = _entityManager.System<Robust.Shared.GameObjects.EntityLookupSystem>();
        var worldViewport = _eyeManager.GetWorldViewport();
        var maxDistSq = MaxSoundDistance * MaxSoundDistance;

        try
        {
            foreach (var entity in lookup.GetEntitiesIntersecting(mapId, worldViewport))
            {
                if (entity == localPlayer)
                    continue;

                // Only show subtitles for entities that are actually playing sounds
                if (!_entityManager.HasComponent<AudioComponent>(entity))
                    continue;

                if (!_entityManager.TryGetComponent<TransformComponent>(entity, out var transform))
                    continue;

                var worldPos = transform.WorldPosition;
                var distSq = (worldPos - playerPos).LengthSquared();
                if (distSq > maxDistSq)
                    continue;

                // Check cooldown
                if (_recentSounds.TryGetValue(entity, out var lastTime) && 
                    (currentTime - lastTime).TotalSeconds < CooldownTime)
                    continue;

                string soundType = GetSoundType(entity);
                if (string.IsNullOrEmpty(soundType))
                    continue;

                // Add to recent sounds
                _recentSounds[entity] = currentTime;

                var direction = worldPos - playerPos;
                var directionStr = GetDirectionString(direction);
                var distance = MathF.Sqrt(distSq);

                _activeSubtitles.Add(new ActiveSubtitle
                {
                    Entity = entity,
                    SoundType = soundType,
                    Direction = directionStr,
                    Color = GetSoundColor(soundType),
                    CreatedAt = currentTime,
                    WorldPosition = worldPos,
                    Distance = distance
                });
            }
        }
        catch
        {
            // Ignore lookup errors
        }
    }

    private void UpdateAndDrawSubtitles(in OverlayDrawArgs args)
    {
        var handle = args.ScreenHandle;
        var currentTime = DateTime.UtcNow;
        var viewportSize = new Vector2(_ui.RootControl.PixelWidth, _ui.RootControl.PixelHeight);

        const float subtitleHeight = 22f;
        const float padding = 10f;
        const float marginRight = 20f;
        const float marginBottom = 100f;

        // Filter and sort subtitles
        var visibleSubtitles = _activeSubtitles
            .Where(s =>
            {
                var age = (currentTime - s.CreatedAt).TotalSeconds;
                return age <= SubtitleLifetime + FadeDuration;
            })
            .OrderByDescending(s => s.CreatedAt)
            .Take(5)
            .ToList();

        if (visibleSubtitles.Count == 0)
            return;

        // Draw from bottom up
        var currentY = viewportSize.Y - marginBottom;

        foreach (var subtitle in visibleSubtitles)
        {
            var age = (currentTime - subtitle.CreatedAt).TotalSeconds;
            float alpha;

            if (age < FadeDuration)
            {
                alpha = (float)(age / FadeDuration);
            }
            else if (age > SubtitleLifetime)
            {
                alpha = (float)(1.0 - (age - SubtitleLifetime) / FadeDuration);
            }
            else
            {
                alpha = 1f;
            }

            if (alpha < 0.05f)
                continue;

            currentY -= subtitleHeight + 2;

            // Make box wider to fit longer names
            var boxWidth = 350f;
            var startX = viewportSize.X - boxWidth - marginRight;

            // Ensure box doesn't go off screen
            if (startX < 0)
            {
                startX = 0;
                boxWidth = viewportSize.X - marginRight;
            }

            // Background
            var bgColor = new Color(0.05f, 0.05f, 0.05f, 0.85f * alpha);
            handle.DrawRect(new UIBox2(startX, currentY, startX + boxWidth, currentY + subtitleHeight), bgColor);

            // Direction arrow
            handle.DrawString(_font, new Vector2(startX + padding, currentY + 3), subtitle.Direction, subtitle.Color.WithAlpha(alpha * 0.9f));

            // Sound text - truncate if too long (estimate ~8px per character)
            var soundText = subtitle.SoundType;
            var textStartX = startX + padding + 24f;
            var maxTextWidth = boxWidth - padding - 60f; // Leave room for distance
            var maxChars = (int)(maxTextWidth / 8f); // Approximate chars that fit
            if (soundText.Length > maxChars)
            {
                soundText = soundText.Substring(0, Math.Max(3, maxChars - 3)) + "...";
            }
            handle.DrawString(_font, new Vector2(textStartX, currentY + 3), soundText, subtitle.Color.WithAlpha(alpha));

            // Distance
            var distText = $"{(int)subtitle.Distance}m";
            handle.DrawString(_font, new Vector2(startX + boxWidth - 45f, currentY + 3), distText, Color.Gray.WithAlpha(alpha * 0.7f));
        }
    }

    private string GetSoundType(EntityUid entity)
    {
        // Check if entity has AudioComponent (actual sound playing)
        if (!_entityManager.HasComponent<AudioComponent>(entity))
            return "";

        if (_entityManager.TryGetComponent<MetaDataComponent>(entity, out var meta))
        {
            var name = meta.EntityName;

            // Return the actual entity name (cleaned up)
            if (!string.IsNullOrEmpty(name) && name != "Sound")
            {
                // Remove common prefixes/suffixes
                name = name.Replace("Sound ", "").Replace(" Audio", "").Trim();
                if (!string.IsNullOrEmpty(name))
                    return name;
            }

            // Fallback to prototype name
            if (meta.EntityPrototype != null)
            {
                var protoName = meta.EntityPrototype.Name ?? meta.EntityPrototype.ID;
                if (!string.IsNullOrEmpty(protoName))
                    return protoName;
            }

            return "Sound";
        }
        return "";
    }

    private string GetDirectionString(Vector2 direction)
    {
        var angle = MathF.Atan2(direction.Y, direction.X);
        var degrees = angle * 180f / MathF.PI;

        if (degrees < 0) degrees += 360;

        if (degrees >= 337.5f || degrees < 22.5f) return "→";
        if (degrees >= 22.5f && degrees < 67.5f) return "↗";
        if (degrees >= 67.5f && degrees < 112.5f) return "↑";
        if (degrees >= 112.5f && degrees < 157.5f) return "↖";
        if (degrees >= 157.5f && degrees < 202.5f) return "←";
        if (degrees >= 202.5f && degrees < 247.5f) return "↙";
        if (degrees >= 247.5f && degrees < 292.5f) return "↓";
        if (degrees >= 292.5f && degrees < 337.5f) return "↘";

        return "●";
    }

    private Color GetSoundColor(string soundType)
    {
        return soundType switch
        {
            "Door" => new Color(0.9f, 0.7f, 0.4f),
            "Glass" => new Color(0.6f, 0.85f, 1f),
            "Gunshot" => new Color(1f, 0.3f, 0.2f),
            "Explosion" => new Color(1f, 0.5f, 0.1f),
            "Alarm" => new Color(1f, 0.2f, 0.2f),
            "Radio" => new Color(0.4f, 0.9f, 0.4f),
            "Footsteps" => new Color(0.7f, 0.7f, 0.7f),
            "Voice" => new Color(0.5f, 0.8f, 1f),
            "Music" => new Color(0.8f, 0.5f, 1f),
            "Machine" => new Color(0.9f, 0.9f, 0.5f),
            _ => new Color(0.9f, 0.9f, 0.9f)
        };
    }

    private class ActiveSubtitle
    {
        public EntityUid Entity;
        public string SoundType = "";
        public string Direction = "";
        public Color Color;
        public DateTime CreatedAt;
        public Vector2 WorldPosition;
        public float Distance;
    }
}