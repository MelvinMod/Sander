using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Hexa.NET.ImGui;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Sander.UI;

public sealed class SanderMassSpecWindow
{
    private bool _open = true;
    private string _search = "";
    private string _status = "Ready";
    private readonly List<ReagentData> _data = new();

    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public bool IsActive => _open;

    public SanderMassSpecWindow()
    {
        IoCManager.InjectDependencies(this);
    }

    public void Render()
    {
        if (!_open)
            return;

        ImGui.SetNextWindowSize(new Vector2(450, 350), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Mass Spectrometer", ref _open, ImGuiWindowFlags.None))
        {
            if (ImGui.BeginTabBar("Tabs"))
            {
                if (ImGui.BeginTabItem("Results"))
                {
                    ImGui.Text("Search:");
                    ImGui.InputText("##search", ref _search, 64);
                    if (ImGui.Button("Refresh"))
                    {
                        _data.Clear();
                        Scan();
                        _status = "Analysis complete";
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Clear"))
                    {
                        _data.Clear();
                        _status = "Cleared";
                    }
                    ImGui.Spacing();
                    ImGui.TextColored(new Vector4(0.4f, 0.95f, 0.3f, 1f), $"Status: {_status}");
                    ImGui.Separator();
                    RenderResults();
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("About"))
                {
                    ImGui.TextWrapped("Client-side reagent scanner. Shows detected chemicals in nearby chemistry items.");
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
        }
    }

    private void Scan()
    {
        try
        {
            var local = _player.LocalEntity;
            if (local == null) return;
            if (!_ent.TryGetComponent<TransformComponent>(local, out var xf)) return;
            var ppos = xf.WorldPosition;

            foreach (var e in _ent.GetEntities())
            {
                if (!_ent.TryGetComponent<TransformComponent>(e, out var t)) continue;
                var dist = (t.WorldPosition - ppos).Length();
                if (dist > 15f) continue;
                if (!_ent.TryGetComponent<MetaDataComponent>(e, out var m)) continue;
                var name = m.EntityName.ToLowerInvariant();
                if (IsChemistry(name))
                {
                    foreach (var r in GetReagents(name))
                        _data.Add(new ReagentData { ReagentId = r, Source = m.EntityName, Distance = dist });
                }
            }
        }
        catch { }
    }

    private bool IsChemistry(string n)
    {
        return n.Contains("beaker") || n.Contains("flask") || n.Contains("bottle") || n.Contains("tube") ||
               n.Contains("solution") || n.Contains("chem") || n.Contains("reagent") || n.Contains("pill") ||
               n.Contains("syringe") || n.Contains("inhaler") || n.Contains("patch") || n.Contains("canister") ||
               n.Contains("tank") || n.Contains("dispenser");
    }

    private List<string> GetReagents(string name)
    {
        var r = new List<string>();
        if (name.Contains("blood")) { r.Add("Blood"); r.Add("Saline-Glucose"); }
        else if (name.Contains("space")) { r.Add("Space Cleaner"); r.Add("Ammonia"); }
        else if (name.Contains("alcohol") || name.Contains("beer") || name.Contains("vodka")) r.Add("Ethanol");
        else if (name.Contains("acid")) { r.Add("Fluorosurfactant"); r.Add("Acid"); }
        else if (name.Contains("morph") || name.Contains("oxy")) { r.Add("Morphine"); r.Add("Oxycodone"); }
        else if (name.Contains("water")) r.Add("Water");
        else if (name.Contains("plasma")) r.Add("Liquid Plasma");
        else if (name.Contains("oxygen")) r.Add("Oxygen");
        else r.Add("Unknown Compound");
        return r;
    }

    private void RenderResults()
    {
        if (_data.Count == 0)
        {
            ImGui.TextWrapped("No reagents detected. Click Refresh to scan.");
            return;
        }

        var filtered = string.IsNullOrEmpty(_search) ? _data : _data.FindAll(x => x.ReagentId.ToLower().Contains(_search.ToLower()));

        if (filtered.Count == 0)
        {
            ImGui.Text($"No reagents match '{_search}'");
            return;
        }

        var grouped = new Dictionary<string, int>();
        foreach (var d in filtered)
        {
            if (grouped.TryGetValue(d.ReagentId, out var c))
                grouped[d.ReagentId] = c + 1;
            else
                grouped[d.ReagentId] = 1;
        }

        foreach (var kvp in grouped)
        {
            var color = GetColor(kvp.Key);
            ImGui.TextColored(color, $"{kvp.Key} - {kvp.Value} source(s)");
        }
    }

    private static Vector4 GetColor(string id)
    {
        var l = id.ToLower();
        if (l.Contains("blood")) return new Vector4(1f, 0f, 0f, 1f);
        if (l.Contains("water") || l.Contains("saline")) return new Vector4(0f, 0.53f, 1f, 1f);
        if (l.Contains("sugar") || l.Contains("glucose")) return new Vector4(1f, 0.84f, 0f, 1f);
        if (l.Contains("morphine") || l.Contains("drug")) return new Vector4(0.6f, 0.2f, 0.8f, 1f);
        if (l.Contains("acid")) return new Vector4(0.2f, 0.8f, 0.2f, 1f);
        if (l.Contains("plasma")) return new Vector4(0f, 1f, 1f, 1f);
        return new Vector4(0.8f, 0.8f, 0.8f, 1f);
    }

    private class ReagentData
    {
        public string ReagentId = "";
        public string Source = "";
        public float Distance;
    }
}