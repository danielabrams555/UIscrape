using System.Windows;
using System.Windows.Controls;
using UIScrape.Models;
using UIScrape.ViewModels;

namespace UIScrape;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
        Closed += MainWindow_Closed;
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        ViewModel.Cleanup();
    }

    private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
    {
        if (sender is TreeViewItem item && item.DataContext is UIElementInfo elementInfo)
        {
            ViewModel.SelectedElement = elementInfo;
            e.Handled = true;
        }
    }
}
