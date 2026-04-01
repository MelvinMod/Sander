using Content.Client.Entry;
using HarmonyLib;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Shared.Maths;
using Robust.Shared.IoC;
using Sander.Overlays;
using Sander.UI;

namespace Sander.Patches;

[HarmonyPatch(typeof(EntryPoint), nameof(EntryPoint.PostInit))]
public static class SanderStartupPatch
{
    private static bool _initialized;
    private static SanderItemSearchOverlay? _searchOverlay;
    private static SanderImplantOverlay? _implantOverlay;
    private static SanderCoordinateOverlay? _coordsOverlay;
    private static SanderSyndicatePirateOverlay? _syndPirateOverlay;
    private static SanderSearchBar? _searchBar;

    public static void Postfix()
    {
        if (_initialized)
            return;

        var overlays = IoCManager.Resolve<IOverlayManager>();
        var ui = IoCManager.Resolve<IUserInterfaceManager>();

        _searchOverlay = new SanderItemSearchOverlay();
        overlays.AddOverlay(_searchOverlay);

        _implantOverlay = new SanderImplantOverlay();
        overlays.AddOverlay(_implantOverlay);

        _coordsOverlay = new SanderCoordinateOverlay();
        overlays.AddOverlay(_coordsOverlay);

        _syndPirateOverlay = new SanderSyndicatePirateOverlay();
        overlays.AddOverlay(_syndPirateOverlay);

        // Top screen search bar UI
        _searchBar = new SanderSearchBar
        {
            HorizontalAlignment = Control.HAlignment.Center,
            VerticalAlignment = Control.VAlignment.Top,
            Margin = new Thickness(0, 8, 0, 0)
        };

        // StateRoot is for state-specific screens (lobby, menus). RootControl persists in gameplay.
        // Avoid duplicate attach if something already added it.
        if (_searchBar.Parent == null)
            ui.RootControl.AddChild(_searchBar);

        _initialized = true;
    }
}

