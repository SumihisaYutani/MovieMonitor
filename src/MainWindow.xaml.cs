using MovieMonitor.ViewModels;
using System.Windows;

namespace MovieMonitor;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // ウィンドウ閉じる時の処理
        Closing += (s, e) => viewModel.OnWindowClosing();
    }
}