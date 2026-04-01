using System.Reflection;
using HarmonyLib;

public static class MarseyEntry
{
    public static void Entry()
    {
        Harmony.DEBUG = false;

        if (!TryGetAssembly("Content.Client"))
            return;

        var asm = Assembly.GetExecutingAssembly();
        SubverterPatch.Harm.PatchAll(asm);
    }

    private static bool TryGetAssembly(string assembly)
    {
        for (var i = 0; i < 50; i++)
        {
            if (FindAssembly(assembly) != null)
                return true;

            Thread.Sleep(200);
        }

        return false;
    }

    private static Assembly? FindAssembly(string assemblyName)
    {
        var asmList = AppDomain.CurrentDomain.GetAssemblies();
        return asmList.FirstOrDefault(asm => asm.FullName?.Contains(assemblyName) == true);
    }
}

