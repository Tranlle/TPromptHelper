using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TPromptHelper.Core.Interfaces;
using TPromptHelper.Core.Models;

namespace TPromptHelper.Desktop.ViewModels;

public partial class ApiLogViewModel : ViewModelBase
{
    private readonly IApiLogger _logger;

    [ObservableProperty] private ObservableCollection<ApiLogEntry> _entries = [];
    [ObservableProperty] private ApiLogEntry? _selectedEntry;

    public ApiLogViewModel(IApiLogger logger)
    {
        _logger = logger;
        Refresh();
        _logger.LogChanged += Refresh;
    }

    private void Refresh()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            Entries = new ObservableCollection<ApiLogEntry>(_logger.Entries));
    }

    [RelayCommand]
    private void Clear()
    {
        _logger.Clear();
        SelectedEntry = null;
    }
}
