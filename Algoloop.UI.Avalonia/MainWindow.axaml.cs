using Avalonia.Controls;
using Algoloop.UI.Avalonia.ViewModels;

namespace Algoloop.UI.Avalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Create and set DataContext
        DataContext = new MainViewModel();
    }
}
