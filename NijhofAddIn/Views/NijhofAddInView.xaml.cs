using NijhofAddIn.ViewModels;

namespace NijhofAddIn.Views;

public sealed partial class NijhofAddInView
{
    public NijhofAddInView(NijhofAddInViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}