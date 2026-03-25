using Avalonia.Controls;
using TPromptHelper.Desktop.ViewModels;

namespace TPromptHelper.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ApiLogRequested += ShowApiLogAsync;
            vm.TokenStatsRequested += ShowTokenStatsAsync;
            vm.SettingsRequested += ShowSettingsAsync;
            await vm.InitializeAsync();
        }
    }

    private Task ShowApiLogAsync(ApiLogViewModel logVm)
    {
        var dialog = new Window
        {
            Title = "API 请求日志",
            Width = 900,
            Height = 560,
            MinWidth = 700,
            MinHeight = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new ApiLogView { DataContext = logVm }
        };
        return dialog.ShowDialog(this);
    }

    private Task ShowTokenStatsAsync(TokenStatsViewModel statsVm)
    {
        var dialog = new Window
        {
            Title = "Token 消费统计",
            Width = 780,
            Height = 520,
            MinWidth = 640,
            MinHeight = 380,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new TokenStatsView { DataContext = statsVm }
        };
        return dialog.ShowDialog(this);
    }

    private Task ShowSettingsAsync(SettingsViewModel settingsVm)
    {
        var dialog = new Window
        {
            Title = "设置",
            Width = 760,
            Height = 520,
            MinWidth = 600,
            MinHeight = 420,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new SettingsView { DataContext = settingsVm }
        };
        return dialog.ShowDialog(this);
    }
}
