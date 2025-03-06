using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using NijhofAddIn.ViewModels;
using NijhofAddIn.Views;

namespace NijhofAddIn.Commands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class StartupCommand : ExternalCommand
{
    public override void Execute()
    {
        var viewModel = new NijhofAddInViewModel();
        var view = new NijhofAddInView(viewModel);
        view.ShowDialog();
    }
}