using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Sander.Systems;

namespace Sander.UI;

public sealed class SanderSearchBar : Control
{
    private readonly LineEdit _query;
    private readonly Label _status;
    private readonly CheckBox _names;
    private readonly CheckBox _implants;
    private readonly CheckBox _implantNames;
    private readonly LineEdit _coords;
    private readonly CheckBox _coordsToggle;
    private readonly CheckBox _coordsText;
    private readonly CheckBox _syndicate;
    private readonly CheckBox _pirate;
    private readonly CheckBox _fullbright;
    private readonly CheckBox _shadows;
    private readonly CheckBox _fov;
    private readonly CheckBox _soundSubtitles;
    private readonly CheckBox _footsteps;
    private readonly CheckBox _gunAimbot;
    private readonly CheckBox _meleeAimbot;
    private readonly Button _mapButton;
    private readonly Button _compassButton;

    private SanderCameraSystem? _cameraSystem;

    public SanderSearchBar()
    {
        MouseFilter = MouseFilterMode.Pass;

        var panel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#101318CC"),
                BorderColor = Color.FromHex("#2A3240FF"),
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 8,
                ContentMarginRightOverride = 8,
                ContentMarginTopOverride = 6,
                ContentMarginBottomOverride = 6
            }
        };

        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 8
        };

        var row2 = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 8
        };

        _query = new LineEdit
        {
            MinWidth = 260,
            PlaceHolder = "Search (e.g. disk, gun, oxygen)…",
            Text = SanderSearchState.Query
        };
        _query.OnTextChanged += _ =>
        {
            SanderSearchState.Query = _query.Text.Trim();
            SanderSearchState.Enabled = !string.IsNullOrWhiteSpace(SanderSearchState.Query);
            UpdateStatus();
        };

        _names = new CheckBox { Text = "Names", Pressed = SanderSearchState.ShowNames };
        _names.OnToggled += args =>
        {
            SanderSearchState.ShowNames = args.Pressed;
            UpdateStatus();
        };

        _implants = new CheckBox { Text = "Implants", Pressed = SanderSearchState.ImplantEnabled };
        _implants.OnToggled += args =>
        {
            SanderSearchState.ImplantEnabled = args.Pressed;
            UpdateStatus();
        };

        _implantNames = new CheckBox { Text = "Implant details", Pressed = SanderSearchState.ImplantShowNames };
        _implantNames.OnToggled += args =>
        {
            SanderSearchState.ImplantShowNames = args.Pressed;
            UpdateStatus();
        };

        _mapButton = new Button { Text = "MAP" };
        _mapButton.OnPressed += _ => SanderMapOpener.Show();

        _compassButton = new Button { Text = "COMPASS" };
        _compassButton.OnPressed += _ => SanderCompassOpener.Show();

        _status = new Label
        {
            Text = "",
            FontColorOverride = Color.FromHex("#C7D0E0FF"),
            MinWidth = 180
        };

        _coords = new LineEdit
        {
            MinWidth = 160,
            PlaceHolder = "Coords: x y",
            Text = SanderSearchState.CoordsText
        };
        _coords.OnTextChanged += _ =>
        {
            SanderSearchState.CoordsText = _coords.Text;
            ParseCoords(_coords.Text);
            UpdateStatus();
        };

        _coordsToggle = new CheckBox { Text = "Coords", Pressed = SanderSearchState.CoordsEnabled };
        _coordsToggle.OnToggled += args =>
        {
            SanderSearchState.CoordsEnabled = args.Pressed;
            UpdateStatus();
        };

        _coordsText = new CheckBox { Text = "Show", Pressed = SanderSearchState.CoordsShowText };
        _coordsText.OnToggled += args =>
        {
            SanderSearchState.CoordsShowText = args.Pressed;
            UpdateStatus();
        };

        _syndicate = new CheckBox { Text = "Synd", Pressed = SanderSearchState.SyndicateEnabled };
        _syndicate.OnToggled += args =>
        {
            SanderSearchState.SyndicateEnabled = args.Pressed;
            UpdateStatus();
        };

        _pirate = new CheckBox { Text = "Pirate", Pressed = SanderSearchState.PirateEnabled };
        _pirate.OnToggled += args =>
        {
            SanderSearchState.PirateEnabled = args.Pressed;
            UpdateStatus();
        };

        // Visual toggles
        _fullbright = new CheckBox { Text = "Fullbright", Pressed = SanderSearchState.FullbrightEnabled };
        _fullbright.OnToggled += args =>
        {
            SanderSearchState.FullbrightEnabled = args.Pressed;
            UpdateCamera();
        };

        _shadows = new CheckBox { Text = "Shadows", Pressed = SanderSearchState.ShadowsEnabled };
        _shadows.OnToggled += args =>
        {
            SanderSearchState.ShadowsEnabled = args.Pressed;
            UpdateCamera();
        };

        _fov = new CheckBox { Text = "FOV", Pressed = SanderSearchState.FovEnabled };
        _fov.OnToggled += args =>
        {
            SanderSearchState.FovEnabled = args.Pressed;
            UpdateCamera();
        };

        _soundSubtitles = new CheckBox { Text = "Sounds", Pressed = SanderSearchState.SoundSubtitlesEnabled };
        _soundSubtitles.OnToggled += args =>
        {
            SanderSearchState.SoundSubtitlesEnabled = args.Pressed;
            UpdateStatus();
        };

        _footsteps = new CheckBox { Text = "Footsteps", Pressed = SanderSearchState.FootstepsEnabled };
        _footsteps.OnToggled += args =>
        {
            SanderSearchState.FootstepsEnabled = args.Pressed;
            UpdateStatus();
        };

        _gunAimbot = new CheckBox { Text = "GunBot", Pressed = SanderSearchState.GunAimbotEnabled };
        _gunAimbot.OnToggled += args =>
        {
            SanderSearchState.GunAimbotEnabled = args.Pressed;
            UpdateStatus();
        };

        _meleeAimbot = new CheckBox { Text = "MeleeBot", Pressed = SanderSearchState.MeleeAimbotEnabled };
        _meleeAimbot.OnToggled += args =>
        {
            SanderSearchState.MeleeAimbotEnabled = args.Pressed;
            UpdateStatus();
        };

        row.AddChild(_query);
        row.AddChild(_names);
        row.AddChild(_implants);
        row.AddChild(_implantNames);
        row.AddChild(_mapButton);
        row.AddChild(_compassButton);
        row.AddChild(_status);

        row2.AddChild(_coordsToggle);
        row2.AddChild(_coords);
        row2.AddChild(_coordsText);
        row2.AddChild(_syndicate);
        row2.AddChild(_pirate);
        row2.AddChild(_fullbright);
        row2.AddChild(_shadows);
        row2.AddChild(_fov);
        row2.AddChild(_soundSubtitles);
        row2.AddChild(_footsteps);
        row2.AddChild(_gunAimbot);
        row2.AddChild(_meleeAimbot);

        var stack = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 6
        };
        stack.AddChild(row);
        stack.AddChild(row2);

        panel.AddChild(stack);
        AddChild(panel);

        UpdateStatus();
    }

    private void UpdateStatus()
    {
        var q = SanderSearchState.Query;
        if (string.IsNullOrWhiteSpace(q))
        {
            _status.Text = "search: off";
        }
        else
        {
            _status.Text = $"search: \"{q}\"";
        }
    }

    private void UpdateCamera()
    {
        // Force update camera settings
        _cameraSystem?.ForceUpdate();
    }

    private static void ParseCoords(string text)
    {
        SanderSearchState.CoordsValid = false;
        var t = text.Trim();
        if (string.IsNullOrWhiteSpace(t))
            return;

        // Accept "x y" or "x, y"
        t = t.Replace(",", " ");
        var parts = t.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
            return;

        if (!float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x))
            return;
        if (!float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var y))
            return;

        SanderSearchState.CoordsTarget = new System.Numerics.Vector2(x, y);
        SanderSearchState.CoordsValid = true;
    }
}

