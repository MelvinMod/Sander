using System.Numerics;
using System.Text;
using Content.Shared.Clothing.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Tag;
using Hexa.NET.ImGui;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Sander.UI;

public sealed class SanderInfoCheckerWindow
{
    private bool _open = true;
    private EntityUid _target;
    private string _infoText = "";

    [Dependency] private readonly IEntityManager _ent = default!;

    public bool IsActive => _open;

    public SanderInfoCheckerWindow()
    {
        IoCManager.InjectDependencies(this);
    }

    public static void Show(EntityUid target)
    {
        var w = new SanderInfoCheckerWindow();
        w._target = target;
        w.UpdateInfo();
    }

    public void Render()
    {
        if (!_open)
            return;

        ImGui.SetNextWindowSize(new Vector2(350, 400), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Entity Info", ref _open, ImGuiWindowFlags.None))
        {
            ImGui.TextColored(new Vector4(1f, 0.84f, 0f, 1f), "Entity Information");
            ImGui.Separator();
            ImGui.TextWrapped(_infoText);
        }
    }

    private void UpdateInfo()
    {
        var sb = new StringBuilder();

        if (!_ent.TryGetComponent<MetaDataComponent>(_target, out var meta))
        {
            _infoText = "Entity not found.";
            return;
        }

        sb.AppendLine($"Name: {meta.EntityName}");
        sb.AppendLine($"Type: {meta.EntityPrototype?.ID ?? "N/A"}");
        sb.AppendLine($"Desc: {meta.EntityDescription ?? "No description"}");

        if (_ent.TryGetComponent<TransformComponent>(_target, out var xform))
        {
            sb.AppendLine($"Pos: {xform.WorldPosition:F1}");
            sb.AppendLine($"Map: {xform.MapID}");
        }

        if (_ent.TryGetComponent<MobStateComponent>(_target, out var ms))
            sb.AppendLine($"State: {ms.CurrentState}");
        else
            sb.AppendLine("Not a mob");

        int count = 0;
        foreach (var c in _ent.GetComponents(_target))
            count++;
        sb.AppendLine($"Components: {count}");

        _infoText = sb.ToString();
    }
}