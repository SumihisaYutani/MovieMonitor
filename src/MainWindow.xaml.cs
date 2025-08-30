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
        System.Diagnostics.Debug.WriteLine("MainWindow constructor called with viewModel: " + (viewModel != null ? "NOT NULL" : "NULL"));
        try
        {
            InitializeComponent();
            DataContext = viewModel;
            System.Diagnostics.Debug.WriteLine("DataContext set successfully");
            
            // ウィンドウ閉じる時の処理
            Closing += (s, e) => viewModel.OnWindowClosing();
            System.Diagnostics.Debug.WriteLine("MainWindow constructor completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Exception in MainWindow constructor: " + ex.Message);
            throw;
        }
    }
}