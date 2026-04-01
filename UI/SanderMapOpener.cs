using Content.Client.Pinpointer.UI;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Sander.UI;

public static class SanderMapOpener
{
    public static void Show()
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var player = IoCManager.Resolve<IPlayerManager>().LocalEntity;
        if (player == null || !player.Value.IsValid())
            return;

        // Prefer grid for map UID.
        if (!entMan.TryGetComponent(player.Value, out TransformComponent? xform))
            return;

        var grid = xform.GridUid;
        if (grid == null)
            return;

        var stationName = string.Empty;
        if (entMan.TryGetComponent(grid.Value, out MetaDataComponent? meta))
            stationName = meta.EntityName;

        // Always create a fresh window: some builds dispose or otherwise "dead-end" windows after closing.
        var window = new StationMapWindow();
        window.Title = "Map";

        // Track the player so the map shows where you're standing.
        window.Set(stationName, grid.Value, player.Value);
        window.OpenCentered();
    }
}

