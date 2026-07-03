using Content.Client.Chemistry.UI;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Sander.UI;

public static class SanderChemMasterOpener
{
    public static void Show()
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var player = IoCManager.Resolve<IPlayerManager>().LocalEntity;
        if (player == null || !player.Value.IsValid())
            return;

        // Open the Reagent Analyzer (ChemMasterWindow)
        // This is the real chemistry analyzer for scanning reagents
        var window = new ChemMasterWindow();
        window.Title = "Reagent Analyzer";
        window.OpenCentered();
    }
}