namespace Sander.UI;

public static class SanderCompassOpener
{
    private static SanderCompassWindow? _window;

    public static void Show()
    {
        _window ??= new SanderCompassWindow();
        _window.Title = "Compass";
        _window.OpenCentered();
    }
}

