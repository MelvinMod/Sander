using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Sander.Commands;

[AnyCommand]
public sealed class SanderSearchCommand : IConsoleCommand
{
    public string Command => "sander.search";
    public string Description => "Set search query text";
    public string Help => "sander.search <text>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (string.IsNullOrWhiteSpace(argStr))
        {
            shell.WriteLine($"Current search query: '{SanderSearchState.Query}'");
            return;
        }

        SanderSearchState.Query = argStr.Trim();
        SanderSearchState.Enabled = true;
        shell.WriteLine($"Sander search query set to: '{SanderSearchState.Query}'");
    }
}

[AnyCommand]
public sealed class SanderHelpCommand : IConsoleCommand
{
    public string Command => "sander.help";
    public string Description => "Show Sander commands";
    public string Help => "sander.help";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        shell.WriteLine("Sander commands:");
        shell.WriteLine("  sander.search <text>         - set item/object search text");
        shell.WriteLine("  sander.search_enable         - enable search overlay");
        shell.WriteLine("  sander.search_clear          - disable search overlay");
        shell.WriteLine("  sander.search_names_on       - show found item names");
        shell.WriteLine("  sander.search_names_off      - hide found item names");
        shell.WriteLine("  sander.implants_on           - enable implant detector");
        shell.WriteLine("  sander.implants_off          - disable implant detector");
        shell.WriteLine("  sander.implants_names_on     - show implant type names");
        shell.WriteLine("  sander.implants_names_off    - show generic IMPLANT only");
    }
}

[AnyCommand]
public sealed class SanderSearchClearCommand : IConsoleCommand
{
    public string Command => "sander.search_clear";
    public string Description => "Disable item search overlay";
    public string Help => "sander.search_clear";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        SanderSearchState.Enabled = false;
        shell.WriteLine("Sander search disabled.");
    }
}

[AnyCommand]
public sealed class SanderSearchEnableCommand : IConsoleCommand
{
    public string Command => "sander.search_enable";
    public string Description => "Enable item search overlay";
    public string Help => "sander.search_enable";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        SanderSearchState.Enabled = true;
        shell.WriteLine($"Sander search enabled for query: '{SanderSearchState.Query}'");
    }
}

[AnyCommand]
public sealed class SanderSearchNamesOnCommand : IConsoleCommand
{
    public string Command => "sander.search_names_on";
    public string Description => "Show names for searched entities";
    public string Help => "sander.search_names_on";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        SanderSearchState.ShowNames = true;
        shell.WriteLine("Sander search names enabled.");
    }
}

[AnyCommand]
public sealed class SanderSearchNamesOffCommand : IConsoleCommand
{
    public string Command => "sander.search_names_off";
    public string Description => "Hide names for searched entities";
    public string Help => "sander.search_names_off";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        SanderSearchState.ShowNames = false;
        shell.WriteLine("Sander search names disabled.");
    }
}

[AnyCommand]
public sealed class SanderImplantsOnCommand : IConsoleCommand
{
    public string Command => "sander.implants_on";
    public string Description => "Enable implant detector overlay";
    public string Help => "sander.implants_on";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        SanderSearchState.ImplantEnabled = true;
        shell.WriteLine("Sander implant detector enabled.");
    }
}

[AnyCommand]
public sealed class SanderImplantsOffCommand : IConsoleCommand
{
    public string Command => "sander.implants_off";
    public string Description => "Disable implant detector overlay";
    public string Help => "sander.implants_off";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        SanderSearchState.ImplantEnabled = false;
        shell.WriteLine("Sander implant detector disabled.");
    }
}

[AnyCommand]
public sealed class SanderImplantsNamesOnCommand : IConsoleCommand
{
    public string Command => "sander.implants_names_on";
    public string Description => "Show implant type names over entities";
    public string Help => "sander.implants_names_on";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        SanderSearchState.ImplantShowNames = true;
        shell.WriteLine("Sander implant names enabled.");
    }
}

[AnyCommand]
public sealed class SanderImplantsNamesOffCommand : IConsoleCommand
{
    public string Command => "sander.implants_names_off";
    public string Description => "Show generic IMPLANT label only";
    public string Help => "sander.implants_names_off";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        SanderSearchState.ImplantShowNames = false;
        shell.WriteLine("Sander implant names disabled.");
    }
}

