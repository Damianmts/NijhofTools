using Nice3point.Revit.Toolkit.External;
using NijhofAddIn.Commands;

namespace NijhofAddIn;

[UsedImplicitly]
public class Application : ExternalApplication
{
    public override void OnStartup()
    {
        CreateRibbon();
    }

    private void CreateRibbon()
    {
        var panel = Application.CreatePanel("Commands", "NijhofAddIn");

        panel.AddPushButton<StartupCommand>("Execute")
            .SetImage("/NijhofAddIn;component/Resources/Icons/RibbonIcon16.png")
            .SetLargeImage("/NijhofAddIn;component/Resources/Icons/RibbonIcon32.png");
    }
}