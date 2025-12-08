using System.Windows;
using ShadowFox.UI.ViewModels;

namespace ShadowFox.UI;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
