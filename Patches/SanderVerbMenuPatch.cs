using Content.Client.ContextMenu.UI;
using Content.Client.Verbs.UI;
using Content.Shared.Implants.Components;
using Content.Shared.Mobs.Components;
using HarmonyLib;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Sander.UI;

namespace Sander.Patches;

[HarmonyPatch(typeof(VerbMenuUIController), "FillVerbPopup")]
public static class SanderVerbMenuPatch
{
    public static void Postfix(VerbMenuUIController __instance, ContextMenuPopup popup)
    {
        try
        {
            var entMan = IoCManager.Resolve<IEntityManager>();

            var netTarget = __instance.CurrentTarget;
            if (!netTarget.IsValid())
                return;

            var target = entMan.GetEntity(netTarget);
            if (!target.IsValid())
                return;

            var context = GetContextController(__instance);
            if (context == null)
                return;

            // Add Info Checker button for living entities
            if (SanderSearchState.InfoCheckerEnabled && entMan.HasComponent<MobStateComponent>(target))
            {
                var infoElement = new ContextMenuElement("Info Checker");
                infoElement.OnPressed += _ => SanderInfoCheckerWindow.Show(target);
                context.AddElement(popup, infoElement);
            }

            // Existing implant info
            if (TryGetImplants(entMan, target, out var implants) && implants.Count > 0)
            {
                var rootElement = new ContextMenuElement("IMPLANT INFO");
                rootElement.SubMenu = new ContextMenuPopup(context, rootElement);

                FillImplantSubMenu(entMan, context, rootElement.SubMenu, target, implants);
                context.AddElement(popup, rootElement);
            }
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
        if (!SanderSearchState.ImplantVisible.TryGetValue(owner, out var visible))
        {
            visible = new HashSet<EntityUid>(implants);
            SanderSearchState.ImplantVisible[owner] = visible;
        }

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

                element.Text = MakeToggleLabel(visible.Contains(implant), name);
                SanderSearchState.ImplantShowNames = true;
            };

            context.AddElement(subMenu, element);
        }

        var showAll = new ContextMenuElement("Show all");
        showAll.OnPressed += _ =>
        {
            visible.Clear();
            foreach (var i in implants)
                visible.Add(i);
            SanderSearchState.ImplantShowNames = true;
        };
        context.AddElement(subMenu, showAll);

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
        var field = AccessTools.Field(typeof(VerbMenuUIController), "_context");
        return field?.GetValue(instance) as ContextMenuUIController;
    }
}