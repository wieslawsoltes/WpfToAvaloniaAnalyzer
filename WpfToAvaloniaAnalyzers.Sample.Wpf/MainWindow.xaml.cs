using System.Windows;
using System.Windows.Controls;

namespace WpfToAvaloniaAnalyzers.Sample.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // This code uses WPF controls that should trigger analyzer warnings
        CreateDockPanelProgrammatically();
    }

    private void CreateDockPanelProgrammatically()
    {
        // Example of programmatic DockPanel usage
        var dockPanel = new DockPanel();
        var button = new Button { Content = "Docked Button" };
        DockPanel.SetDock(button, Dock.Top);
        dockPanel.Children.Add(button);
    }
}
