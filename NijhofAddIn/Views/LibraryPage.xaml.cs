using System.Windows;
using System.Windows.Controls;
using NijhofAddIn.ViewModels;

namespace NijhofAddIn.Views;

public partial class LibraryPage : Page
{
    private void SfTreeView_QueryNodeSize(object sender, Syncfusion.UI.Xaml.TreeView.QueryNodeSizeEventArgs e)
    {
        // Controleer of het argument geldig is
        if (e == null) return;

        // Dynamisch de hoogte berekenen op basis van inhoud
        double autoFitHeight = e.GetAutoFitNodeHeight();

        // Stel een minimumhoogte in
        double minHeight = 30; // Pas dit aan naar je gewenste minimumhoogte
        e.Height = Math.Max(autoFitHeight, minHeight);

        e.Handled = true; // Voorkom dat de standaardhoogte wordt gebruikt
    }
    public LibraryPage()
    {
        InitializeComponent();
        DataContext = new LibraryViewModel();
    }
}