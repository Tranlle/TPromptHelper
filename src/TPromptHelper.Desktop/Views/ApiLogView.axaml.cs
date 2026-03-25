using Avalonia.Controls;
using Avalonia.Interactivity;
using TPromptHelper.Desktop.ViewModels;

namespace TPromptHelper.Desktop.Views;

public partial class ApiLogView : UserControl
{
    public ApiLogView() => InitializeComponent();

    private async void OnClearClick(object? sender, RoutedEventArgs e)
    {
        var owner = TopLevel.GetTopLevel(this);
        if (owner is null) return;

        var ok = await DialogHelper.ConfirmAsync(owner, "清空日志", "确定要清空所有 API 请求日志吗？此操作不可撤销。", "确认清空");
        if (ok && DataContext is ApiLogViewModel vm)
            vm.ClearCommand.Execute(null);
    }
}
