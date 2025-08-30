using Microsoft.Extensions.DependencyInjection;
using MovieMonitor.ViewModels;
using System.Windows;

namespace MovieMonitor.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        
        try
        {
            // DIコンテナからViewModelを取得
            var app = (App)Application.Current;
            var serviceProvider = app.ServiceProvider;
            var viewModel = serviceProvider.GetRequiredService<SettingsViewModel>();
            
            DataContext = viewModel;
            
            // ViewModelからのウィンドウクローズ要求を処理
            viewModel.RequestClose += (s, e) => Close();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"設定画面の初期化に失敗しました: {ex.Message}", "エラー", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
    }
}