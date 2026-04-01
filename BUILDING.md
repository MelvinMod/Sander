# Building Sander Mod

## Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022 or Rider (optional)
- Space Station 14 development environment (for reference assemblies)

## Building from Source

### 1. Clone the repository
```bash
git clone <repository-url>
cd sander
```

### 2. Restore dependencies
```bash
cd Sander
dotnet restore
```

### 3. Build for your platform
```bash
# Linux
dotnet build -c Release -r linux-x64

# Windows
dotnet build -c Release -r win-x64
```

### 4. Publish for distribution
```bash
# Linux
dotnet publish -c Release -r linux-x64 --self-contained false -o bin/publish/linux

# Windows  
dotnet publish -c Release -r win-x64 --self-contained false -o bin/publish/windows
```

## Output Locations

After building, find the DLL at:
- `Sander/bin/Release/net9.0/Sander.dll`

After publishing:
- `Sander/bin/publish/linux/Sander.dll`
- `Sander/bin/publish/windows/Sander.dll`

## Project Structure

```
Sander/
├── bin/                    # Build output
├── obj/                    # Intermediate files
├── Commands/               # Chat commands
├── Overlays/               # Visual overlays (Render)
│   ├── Compass             # Navigation compass
│   ├── Implant             # Implant display
│   ├── SoundSubtitle       # Accessibility subtitles
│   ├── Footsteps           # Movement trails
│   ├── Aimbot              # Targeting assistance
│   └── SyndicatePirate     # Antagonist detection
├── Patches/                # Harmony patches
├── Resources/              # Reference assemblies
├── Systems/                # Game systems
├── UI/                     # User interface
├── MarseyEntry.cs          # Mod entry point
├── Sander.csproj           # Project file
├── SanderSearchState.cs    # Global settings
└── README.md               # This file
```

## Key Systems

### Overlay System
All visual features use the Robust Toolbox overlay system:
```csharp
public sealed class ExampleOverlay : Overlay
{
    public override OverlaySpace Space => (OverlaySpace)2;
    
    protected override void Draw(in OverlayDrawArgs args)
    {
        // Drawing code here
    }
}
```

### State Management
Global settings stored in `SanderSearchState.cs`:
```csharp
public static class SanderSearchState
{
    public static bool Enabled = true;
    public static string Query = "";
    public static Vector4 Color = new(0.2f, 0.8f, 0.2f, 1f);
}
```

## Adding New Features

1. Create overlay in `Overlays/`
2. Register in `Systems/SanderOverlayRegistrationSystem.cs`
3. Add UI controls in `UI/SanderSearchBar.cs`
4. Add state in `SanderSearchState.cs`

## Troubleshooting

### Build Errors
- Ensure .NET 9.0 is installed: `dotnet --version`
- Restore packages: `dotnet restore`

### Runtime Errors
- Verify DLL is in correct mod folder
- Check game version compatibility
- Review console output for error messages

### Performance Issues
- Reduce update intervals in overlay files
- Enable caching for entity lookups
- Limit detection range
