using System.Numerics;
using Hexa.NET.ImGui;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Sander.UI;

public sealed class SanderScannerWindow
{
    private bool _open = true;
    private string _infoText = "";
    private string _status = "Ready";

    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IEyeManager _eye = default!;

    public bool IsActive => _open;

    public SanderScannerWindow()
    {
        IoCManager.InjectDependencies(this);
        RefreshScan();
    }

    public void Render()
    {
        if (!_open)
            return;

        ImGui.SetNextWindowSize(new Vector2(500, 420), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Scanner", ref _open, ImGuiWindowFlags.None))
        {
            ImGui.TextColored(new Vector4(1f, 1f, 1f, 1f), "Space Map Scanner");
            ImGui.Separator();
            ImGui.TextColored(new Vector4(0.4f, 0.95f, 0.3f, 1f), $"Status: {_status}");
            ImGui.Separator();
            ImGui.TextWrapped(_infoText);
            if (ImGui.Button("Refresh"))
                RefreshScan();
            ImGui.SameLine();
            if (ImGui.Button("Close"))
                _open = false;
        }
    }

    private void RefreshScan()
    {
        var sb = new System.Text.StringBuilder();
        try
        {
            sb.AppendLine("Space Scanner Results:");
            sb.AppendLine();
            int shuttles = 0, stations = 0, total = 0;
            foreach (var e in _ent.GetEntities())
            {
                if (!_ent.TryGetComponent<MetaDataComponent>(e, out var m)) continue;
                var name = m.EntityName.ToLowerInvariant();
                var proto = m.EntityPrototype?.ID ?? "";
                if (name.Contains("shuttle") || proto.Contains("shuttle")) { shuttles++; sb.AppendLine($"Shuttle: {m.EntityName}"); }
                else if (name.Contains("station") || name.Contains("grid") || proto.Contains("station") || proto.Contains("grid")) { stations++; sb.AppendLine($"Structure: {m.EntityName}"); }
                total++;
            }
            sb.AppendLine();
            sb.AppendLine($"Total entities: {total}");
            sb.AppendLine($"Shuttles: {shuttles}");
            sb.AppendLine($"Structures: {stations}");
        }
        catch
        {
            sb.AppendLine("Scan failed");
        }
        _infoText = sb.ToString();
        _status = "Scan complete";
    }

    public static void Show()
    {
        var ui = IoCManager.Resolve<Robust.Client.UserInterface.IUserInterfaceManager>();
        var w = new SanderScannerWindow();
        w.RefreshScan();
    }
}