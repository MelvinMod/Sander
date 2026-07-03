using Content.Client.Pinpointer.UI;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Sander.UI;

public static class SanderHandHeldMassScannerOpener
{
    public static void Show()
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var player = IoCManager.Resolve<IPlayerManager>().LocalEntity;
        if (player == null || !player.Value.IsValid())
            return;

        // Open the HandHeldMassScanner (StationMapWindow)
        // This shows a non-interactive map of the station with shuttles, debris, and wrecks
        var window = new StationMapWindow();
        window.Title = "Handheld Mass Scanner";
        window.OpenCentered();
    }
}