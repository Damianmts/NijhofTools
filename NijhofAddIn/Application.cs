using Nice3point.Revit.Toolkit.External;
using NijhofAddIn.Commands;
using NijhofAddIn.Views;
using Syncfusion.Licensing;

namespace NijhofAddIn;

[UsedImplicitly]
public class Application : ExternalApplication
{
    public override void OnStartup()
    {
        SyncfusionLicenseProvider.RegisterLicense
            ("Ngo9BigBOggjHTQxAR8/V1NMaF5cXmBCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWX5fdHRcQ2JZWEd3W0o=");
        
        System.Windows.Application.ResourceAssembly = typeof(NijhofAddInView).Assembly;
        
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