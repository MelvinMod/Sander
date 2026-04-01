using System.Collections.Generic;
using Content.Client.ContextMenu.UI;
using Content.Client.Verbs.UI;
using Content.Shared.Implants.Components;
using Content.Shared.Verbs;
using HarmonyLib;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Sander.Patches;

// Adds a right-click verb menu entry: "IMPLANT INFO"
[HarmonyPatch(typeof(VerbMenuUIController), "FillVerbPopup")]
public static class SanderVerbMenuPatch
{
    public static void Postfix(VerbMenuUIController __instance, ContextMenuPopup popup)
    {
        try
        {
            var entMan = IoCManager.Resolve<IEntityManager>();

            // Resolve current target to an EntityUid.
            var netTarget = __instance.CurrentTarget;
            if (!netTarget.IsValid())
                return;

            var target = entMan.GetEntity(netTarget);
            if (!target.IsValid())
                return;

            // Only show if the target currently has implants.
            if (!TryGetImplants(entMan, target, out var implants) || implants.Count == 0)
                return;

            var context = GetContextController(__instance);
            if (context == null)
                return;

            var rootElement = new ContextMenuElement("IMPLANT INFO");
            rootElement.SubMenu = new ContextMenuPopup(context, rootElement);

            FillImplantSubMenu(entMan, context, rootElement.SubMenu, target, implants);

            // Add near the top.
            context.AddElement(popup, rootElement);
        }
        catch
        {
            // If SS14 internals change, don't crash the client.
        }
    }

    private static void FillImplantSubMenu(
        IEntityManager entMan,
        ContextMenuUIController context,
        ContextMenuPopup subMenu,
        EntityUid owner,
        IReadOnlyList<EntityUid> implants)
    {
        // Ensure we have a set; default is "all visible" when first opened.
        if (!SanderSearchState.ImplantVisible.TryGetValue(owner, out var visible))
        {
            visible = new HashSet<EntityUid>(implants);
            SanderSearchState.ImplantVisible[owner] = visible;
        }

        // Helper element to toggle a single implant.
        foreach (var implant in implants)
        {
            var name = "implant";
            if (entMan.TryGetComponent(implant, out MetaDataComponent? meta) && !string.IsNullOrWhiteSpace(meta.EntityName))
                name = meta.EntityName;

            var element = new ContextMenuElement(MakeToggleLabel(visible.Contains(implant), name));
            element.OnPressed += _ =>
            {
                if (!visible.Add(implant))
                    visible.Remove(implant);

                // Update label in-place.
                element.Text = MakeToggleLabel(visible.Contains(implant), name);

                // If user is using this menu, they want details.
                SanderSearchState.ImplantShowNames = true;
            };

            context.AddElement(subMenu, element);
        }

        // Quick action: show all
        var showAll = new ContextMenuElement("Show all");
        showAll.OnPressed += _ =>
        {
            visible.Clear();
            foreach (var i in implants)
                visible.Add(i);
            SanderSearchState.ImplantShowNames = true;
        };
        context.AddElement(subMenu, showAll);

        // Quick action: hide all (will fall back to generic [IMPL])
        var hideAll = new ContextMenuElement("Hide all");
        hideAll.OnPressed += _ =>
        {
            visible.Clear();
            SanderSearchState.ImplantShowNames = true;
        };
        context.AddElement(subMenu, hideAll);
    }

    private static string MakeToggleLabel(bool on, string name)
    {
        return on ? $"[x] {name}" : $"[ ] {name}";
    }

    private static bool TryGetImplants(IEntityManager entMan, EntityUid owner, out IReadOnlyList<EntityUid> implants)
    {
        implants = Array.Empty<EntityUid>();

        if (!entMan.TryGetComponent(owner, out ContainerManagerComponent? containers))
            return false;

        if (!containers.Containers.TryGetValue(ImplanterComponent.ImplantSlotId, out var implantContainer))
            return false;

        if (implantContainer == null)
            return false;

        implants = implantContainer.ContainedEntities;
        return true;
    }

    private static ContextMenuUIController? GetContextController(VerbMenuUIController instance)
    {
        // private readonly ContextMenuUIController _context
        var field = AccessTools.Field(typeof(VerbMenuUIController), "_context");
        return field?.GetValue(instance) as ContextMenuUIController;
    }
}

