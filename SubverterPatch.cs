using HarmonyLib;

// Global namespace on purpose (MarseyLoader reflection lookup).
public static class SubverterPatch
{
    public static string Name = "Sander";
    public static string Description = "Top-screen item/object search overlay";
    public static Harmony Harm = new("com.sander.searcher");
}

