using System.Numerics;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
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

    private readonly Font _font;
    private readonly Font _iconFont;

    // Active sound subtitles with animation state
    private readonly List<ActiveSubtitle> _subtitles = new();
    private int _frameCounter = 0;
    private const int CacheUpdateInterval = 5;

    // Settings
    private const float MaxSoundDistance = 128f;
    private const float FadeInDuration = 0.2f;
    private const float FadeOutDuration = 0.5f;
    private const float DisplayDuration = 2.5f;

    public SanderSoundSubtitleOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = 250;
        _font = new VectorFont(_resourceCache.GetResource<FontResource>("/Fonts/Boxfont-round/Boxfont Round.ttf"), 12);
        _iconFont = new VectorFont(_resourceCache.GetResource<FontResource>("/Fonts/Boxfont-round/Boxfont Round.ttf"), 10);
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

        // Update subtitle states
        UpdateSubtitles();

        if (_subtitles.Count == 0)
            return;

        // Draw with smooth animations
        DrawAnimatedSubtitles(args.ScreenHandle);
    }

    private void UpdateSubtitles()
    {
        var currentTime = DateTime.UtcNow;

        // Update existing subtitles (fade out expired ones)
        foreach (var subtitle in _subtitles)
        {
            var age = (currentTime - subtitle.StartTime).TotalSeconds;
            
            if (age < FadeInDuration)
            {
                // Fading in
                subtitle.Alpha = (float)(age / FadeInDuration);
                subtitle.State = SubtitleState.FadeIn;
            }
            else if (age < DisplayDuration)
            {
                // Fully visible
                subtitle.Alpha = 1f;
                subtitle.State = SubtitleState.Visible;
            }
            else if (age < DisplayDuration + FadeOutDuration)
            {
                // Fading out
                subtitle.Alpha = 1f - (float)((age - DisplayDuration) / FadeOutDuration);
                subtitle.State = SubtitleState.FadeOut;
            }
            else
            {
                // Expired
                subtitle.Alpha = 0f;
                subtitle.State = SubtitleState.Expired;
            }
        }

        // Remove expired subtitles
        _subtitles.RemoveAll(s => s.State == SubtitleState.Expired);

        // Update positions and add new sounds
        if (_frameCounter >= CacheUpdateInterval)
        {
            _frameCounter = 0;
            UpdateSoundSources();
        }
    }

    private void UpdateSoundSources()
    {
        var localPlayer = _playerManager.LocalEntity;
        if (localPlayer == null)
            return;

        if (!_entityManager.TryGetComponent<TransformComponent>(localPlayer, out var playerTransform))
            return;

        var playerPos = playerTransform.WorldPosition;
        var mapId = playerTransform.MapID;
        if (mapId == MapId.Nullspace)
            return;

        var lookup = _entityManager.System<Robust.Shared.GameObjects.EntityLookupSystem>();
        var worldViewport = _eyeManager.GetWorldViewport();
        var maxDistSq = MaxSoundDistance * MaxSoundDistance;

        try
        {
            foreach (var entity in lookup.GetEntitiesIntersecting(mapId, worldViewport))
            {
                if (entity == localPlayer)
                    continue;

                if (!_entityManager.TryGetComponent<TransformComponent>(entity, out var transform))
                    continue;

                var worldPos = transform.WorldPosition;
                var distSq = (worldPos - playerPos).LengthSquared();
                if (distSq > maxDistSq)
                    continue;

                // Get entity name
                string entityName = "";
                if (_entityManager.TryGetComponent<MetaDataComponent>(entity, out var meta))
                {
                    entityName = meta.EntityName;
                }

                var soundType = GetSoundType(entity, entityName);
                if (string.IsNullOrEmpty(soundType))
                    continue;

                // Calculate direction relative to player
                var direction = worldPos - playerPos;
                var directionStr = GetDirectionString(direction);

                // Check if this entity already has a subtitle
                var existing = _subtitles.Find(s => s.Entity == entity && s.SoundType == soundType);
                
                if (existing != null)
                {
                    // Update existing - refresh time and position
                    existing.StartTime = DateTime.UtcNow;
                    existing.WorldPosition = worldPos;
                    existing.Direction = directionStr;
                    existing.Alpha = 1f;
                    existing.State = SubtitleState.Visible;
                }
                else
                {
                    // Add new subtitle
                    _subtitles.Add(new ActiveSubtitle
                    {
                        Entity = entity,
                        WorldPosition = worldPos,
                        SoundType = soundType,
                        Direction = directionStr,
                        Color = GetSoundColor(soundType),
                        StartTime = DateTime.UtcNow,
                        Alpha = 0f,
                        State = SubtitleState.FadeIn
                    });
                }
            }
        }
        catch
        {
            // Ignore lookup errors
        }
    }

    private string GetSoundType(EntityUid entity, string entityName)
    {
        var lowerName = entityName.ToLowerInvariant();

        if (lowerName.Contains("door"))
            return "Door opens";
            
        if (lowerName.Contains("window"))
            return "Window breaks";
            
        if (lowerName.Contains("gun") || lowerName.Contains("pistol") || lowerName.Contains("rifle") || lowerName.Contains("shotgun"))
            return "Gunshot";
            
        if (lowerName.Contains("melee") || lowerName.Contains("hit") || lowerName.Contains("punch") || lowerName.Contains("slash"))
            return "Impact";
            
        if (lowerName.Contains("explosion") || lowerName.Contains("bomb"))
            return "Explosion";
            
        if (lowerName.Contains("alarm") || lowerName.Contains("siren"))
            return "Alarm";
            
        if (lowerName.Contains("radio") || lowerName.Contains("intercom"))
            return "Radio";
            
        if (lowerName.Contains("computer"))
            return "Computer";
            
        if (lowerName.Contains("generator") || lowerName.Contains("smes") || lowerName.Contains("apc"))
            return "Electrical";
            
        if (lowerName.Contains("bot") || lowerName.Contains("cyborg"))
            return "Robot";
            
        if (lowerName.Contains("ghost"))
            return "Ghost";
            
        if (lowerName.Contains("vent"))
            return "Vent";
            
        if (lowerName.Contains("lock"))
            return "Lock";
            
        if (lowerName.Contains("footstep") || lowerName.Contains("walk") || lowerName.Contains("step"))
            return "Footsteps";

        if (_entityManager.HasComponent<Content.Shared.Mobs.Components.MobStateComponent>(entity))
        {
            if (lowerName.Contains("hurt") || lowerName.Contains("pain"))
                return "Pain";
            if (lowerName.Contains("die") || lowerName.Contains("death"))
                return "Death";
            if (lowerName.Contains("scream"))
                return "Scream";
            if (lowerName.Contains("breath"))
                return "Breathing";
            if (lowerName.Contains("heart"))
                return "Heartbeat";
            
            return "Mob sound";
        }

        return "";
    }

    private string GetDirectionString(Vector2 direction)
    {
        var angle = MathF.Atan2(direction.Y, direction.X);
        
        // Convert to 8-direction compass
        if (angle >= -MathF.PI / 8f && angle < MathF.PI / 8f)
            return "→";  // Right
        if (angle >= MathF.PI / 8f && angle < 3f * MathF.PI / 8f)
            return "↗";  // Up-Right
        if (angle >= 3f * MathF.PI / 8f && angle < 5f * MathF.PI / 8f)
            return "↑";  // Up
        if (angle >= 5f * MathF.PI / 8f && angle < 7f * MathF.PI / 8f)
            return "↖";  // Up-Left
        if (angle >= 7f * MathF.PI / 8f || angle < -7f * MathF.PI / 8f)
            return "←";  // Left
        if (angle >= -7f * MathF.PI / 8f && angle < -5f * MathF.PI / 8f)
            return "↙";  // Down-Left
        if (angle >= -5f * MathF.PI / 8f && angle < -3f * MathF.PI / 8f)
            return "↓";  // Down
        if (angle >= -3f * MathF.PI / 8f && angle < -MathF.PI / 8f)
            return "↘";  // Down-Right
            
        return "●"; // Center/Unknown
    }

    private Color GetSoundColor(string soundType)
    {
        return soundType switch
        {
            "Door opens" => new Color(0.95f, 0.7f, 0.4f),
            "Window breaks" => new Color(0.6f, 0.85f, 1f),
            "Gunshot" => new Color(1f, 0.4f, 0.2f),
            "Impact" => new Color(1f, 0.55f, 0.3f),
            "Explosion" => new Color(1f, 0.6f, 0.2f),
            "Alarm" => new Color(1f, 0.3f, 0.3f),
            "Radio" => new Color(0.5f, 0.9f, 0.5f),
            "Computer" => new Color(0.5f, 0.7f, 0.95f),
            "Electrical" => new Color(1f, 1f, 0.5f),
            "Robot" => new Color(0.6f, 0.75f, 0.9f),
            "Ghost" => new Color(0.85f, 0.85f, 1f),
            "Vent" => new Color(0.65f, 0.85f, 0.65f),
            "Lock" => new Color(0.8f, 0.7f, 0.5f),
            "Footsteps" => new Color(0.75f, 0.75f, 0.75f),
            "Pain" => new Color(1f, 0.45f, 0.45f),
            "Death" => new Color(0.65f, 0.25f, 0.25f),
            "Scream" => new Color(1f, 0.65f, 0.65f),
            "Breathing" => new Color(0.6f, 0.85f, 0.9f),
            "Heartbeat" => new Color(0.85f, 0.3f, 0.35f),
            "Mob sound" => new Color(0.9f, 0.9f, 0.9f),
            _ => new Color(0.95f, 0.95f, 0.95f)
        };
    }

    private void DrawAnimatedSubtitles(DrawingHandleScreen handle)
    {
        // Fixed position: bottom-right, comfort position (not at very bottom)
        const float boxWidth = 220f;
        const float rowHeight = 22f;
        const float padding = 8f;
        const float marginRight = 20f;
        const float marginBottom = 80f; // Comfort position from bottom
        
        // Calculate box height based on visible subtitles
        var visibleSubtitles = _subtitles.FindAll(s => s.Alpha > 0.01f);
        if (visibleSubtitles.Count == 0)
            return;
            
        var boxHeight = visibleSubtitles.Count * rowHeight + padding * 2;
        
        // Use fixed viewport size for consistent positioning
        var viewportSize = new Vector2(1920, 1080);
        
        var startX = viewportSize.X - boxWidth - marginRight;
        var startY = viewportSize.Y - boxHeight - marginBottom;

        // Draw background box
        var bgColor = new Color(0.02f, 0.02f, 0.02f, 0.8f);
        handle.DrawRect(new UIBox2(startX, startY, startX + boxWidth, startY + boxHeight), bgColor);

        // Draw each subtitle with animation
        var y = startY + padding;
        
        // Sort by alpha for proper layering (fade in on top)
        visibleSubtitles.Sort((a, b) => b.Alpha.CompareTo(a.Alpha));
        
        foreach (var subtitle in visibleSubtitles)
        {
            var alpha = subtitle.Alpha;
            var textColor = subtitle.Color.WithAlpha(alpha);
            var iconColor = subtitle.Color.WithAlpha(alpha * 0.9f);
            
            // Draw direction arrow icon
            handle.DrawString(_iconFont, new Vector2(startX + padding, y + 2), subtitle.Direction, iconColor);
            
            // Draw sound text
            handle.DrawString(_font, new Vector2(startX + padding + 20f, y + 2), subtitle.SoundType, textColor);
            
            y += rowHeight;
        }
    }

    private enum SubtitleState
    {
        FadeIn,
        Visible,
        FadeOut,
        Expired
    }

    private class ActiveSubtitle
    {
        public EntityUid Entity;
        public Vector2 WorldPosition;
        public string SoundType = "";
        public string Direction = "";
        public Color Color;
        public DateTime StartTime;
        public float Alpha;
        public SubtitleState State;
    }
}
