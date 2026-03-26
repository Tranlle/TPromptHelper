using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace TPromptHelper.Desktop.Views;

public static class DialogHelper
{
    public static async Task<bool> ConfirmAsync(
        TopLevel owner, string title, string message, string confirmText = "确认")
    {
        var confirmed = false;

        var msgBlock = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.Parse("#CCCCCC")),
            Margin = new Thickness(0, 0, 0, 24)
        };

        var noBtn = new Button { Content = "取消", MinWidth = 80 };
        noBtn.Classes.Add("secondary");

        var yesBtn = new Button { Content = confirmText, MinWidth = 80 };
        yesBtn.Classes.Add("danger");

        var btnRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        btnRow.Children.Add(noBtn);
        btnRow.Children.Add(yesBtn);

        var content = new StackPanel { Spacing = 0 };
        content.Children.Add(msgBlock);
        content.Children.Add(btnRow);

        var border = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#1E1E1E")),
            Padding = new Thickness(28, 24),
            Child = content
        };

        var dialog = new Window
        {
            Title = title,
            Width = 360,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = border
        };

        if (Application.Current?.RequestedThemeVariant is { } theme)
            dialog.RequestedThemeVariant = theme;

        noBtn.Click += (_, _) => dialog.Close();
        yesBtn.Click += (_, _) => { confirmed = true; dialog.Close(); };

        if (owner is Window ownerWindow)
            await dialog.ShowDialog(ownerWindow);
        else
            dialog.Show();

        return confirmed;
    }
}
