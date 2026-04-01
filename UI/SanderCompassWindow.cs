using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;

namespace Sander.UI;

public sealed class SanderCompassWindow : DefaultWindow
{
    public SanderCompassWindow()
    {
        Resizable = false;
        MinSize = new(280, 280);
        SetSize = new(280, 280);

        // DefaultWindow implementations differ across builds; keep UI simple.
        var panel = new PanelContainer { Margin = new Thickness(8) };
        panel.AddChild(new SanderCompassControl());
        Contents.AddChild(panel);
    }
}

