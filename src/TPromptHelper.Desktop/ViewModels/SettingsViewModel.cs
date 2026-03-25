using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TPromptHelper.Desktop.ViewModels;

public partial class SettingsViewModel(ModelSettingsViewModel modelSettings) : ViewModelBase
{
    [ObservableProperty] private bool _isDarkTheme = true;

    public ModelSettingsViewModel ModelSettings { get; } = modelSettings;

    public async Task InitializeAsync()
    {
        IsDarkTheme = Avalonia.Application.Current?.RequestedThemeVariant != ThemeVariant.Light;
        await ModelSettings.LoadAsync();
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        if (Avalonia.Application.Current is null) return;
        Avalonia.Application.Current.RequestedThemeVariant = value ? ThemeVariant.Dark : ThemeVariant.Light;
    }
}
