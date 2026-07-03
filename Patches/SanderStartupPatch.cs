using Content.Client.Entry;
using HarmonyLib;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Shared.Maths;
using Robust.Shared.IoC;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.GameObjects;
using Sander.Overlays;
using Sander.UI;
using Sander.Systems;
using Sander.Patches;

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
    private static SanderPacketDebugOverlay? _packetDebugOverlay;
    private static SanderSocialButtons? _socialButtons;

    public static void Postfix()
    {
        try
        {
            if (_initialized)
                return;

            // Initialize Packet Fixer
            PacketFixerInitializer.Initialize();

            // Initialize FPS Booster - automatically enabled
            try
            {
                FPSBooster.Initialize();
            }
            catch
            {
                // Ignore if fails
            }

            // Initialize Graphics Settings
            try
            {
                Sander.UI.SanderGraphicsSettings.Initialize();
            }
            catch
            {
                // Ignore if fails
            }

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

            // Packet Fixer - Always active with debug overlay
            _packetDebugOverlay = new SanderPacketDebugOverlay();
            overlays.AddOverlay(_packetDebugOverlay);

            // Initialize packet fixer system
            try
            {
                SanderClientPacketExtender.Initialize();
            }
            catch
            {
                // Ignore errors during initialization
            }

            // Top screen search bar UI
            _searchBar = new SanderSearchBar();

            // Try to get systems for the search bar (optional)
            try
            {
                var entManager = IoCManager.Resolve<IEntityManager>();
                var cameraSystem = entManager.System<SanderCameraSystem>();
                var ghostSystem = entManager.System<SanderGhostSystem>();
                _searchBar.SetSystems(cameraSystem, ghostSystem);
            }
            catch
            {
                // Systems might not be available yet, that's okay
            }

            // StateRoot is for state-specific screens (lobby, menus). RootControl persists in gameplay.
            // Avoid duplicate attach if something already added it.
            if (_searchBar.Parent == null)
                ui.RootControl.AddChild(_searchBar);

            // Social media buttons (Matrix + Telegram) - bottom-right corner
            _socialButtons = new SanderSocialButtons();
            if (_socialButtons.Parent == null)
                ui.RootControl.AddChild(_socialButtons);

            _initialized = true;
        }
        catch (Exception ex)
        {
            Logger.Error($"[Sander] Startup failed: {ex.Message}\n{ex.StackTrace}");
        }
    }

}

