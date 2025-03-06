using System.Windows.Controls;
using NijhofAddIn.Models;
using NijhofAddIn.ViewModels;

namespace NijhofAddIn.Views;

public sealed partial class NijhofAddInView
{
    public NijhofAddInView(NijhofAddInViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    private void sidebar_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = sidebar.SelectedItem as NavButton;

        navframe.Navigate(selected.Navlink);
    }
}