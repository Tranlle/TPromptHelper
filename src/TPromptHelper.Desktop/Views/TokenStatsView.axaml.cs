using Avalonia.Controls;
using Avalonia.Interactivity;
using TPromptHelper.Desktop.ViewModels;

namespace TPromptHelper.Desktop.Views;

public partial class TokenStatsView : UserControl
{
    public TokenStatsView() => InitializeComponent();

    private async void OnClearClick(object? sender, RoutedEventArgs e)
    {
        var owner = TopLevel.GetTopLevel(this);
        if (owner is null) return;

        var ok = await DialogHelper.ConfirmAsync(owner, "清空统计", "确定要清空所有 Token 消费记录吗？此操作不可撤销。", "确认清空");
        if (ok && DataContext is TokenStatsViewModel vm)
            await vm.ClearCommand.ExecuteAsync(null);
    }
}
