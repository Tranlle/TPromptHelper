using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using TPromptHelper.Core.Interfaces;
using TPromptHelper.Core.Models;

namespace TPromptHelper.Desktop.ViewModels;

public partial class MainWindowViewModel(ISessionRepository sessionRepo, IModelProfileRepository modelRepo, IServiceProvider serviceProvider) : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<OptimizationSession> _sessions = [];
    [ObservableProperty] private ObservableCollection<ModelProfile> _modelProfiles = [];
    [ObservableProperty] private SessionViewModel? _activeSession;
    [ObservableProperty] private ModelProfile? _selectedModel;
    [ObservableProperty] private OptimizationSession? _selectedSession;
    [ObservableProperty] private OptimizationSession? _renamingSession;
    [ObservableProperty] private string _renameTitle = string.Empty;

    partial void OnSelectedSessionChanged(OptimizationSession? value)
    {
        if (value != null && ActiveSession?.Session?.Id != value.Id)
            OpenSession(value);
    }

    partial void OnSelectedModelChanged(ModelProfile? value)
    {
        if (ActiveSession != null)
            ActiveSession.Model = value;
    }

    public async Task InitializeAsync()
    {
        var sessions = await sessionRepo.GetAllAsync();
        Sessions = new ObservableCollection<OptimizationSession>(sessions);

        var profiles = await modelRepo.GetAllAsync();
        ModelProfiles = new ObservableCollection<ModelProfile>(profiles);
        SelectedModel = ModelProfiles.FirstOrDefault(p => p.IsDefault) ?? ModelProfiles.FirstOrDefault();
    }

    [RelayCommand]
    private void NewSession()
    {
        var session = new OptimizationSession { Title = $"会话 {Sessions.Count + 1}" };
        Sessions.Insert(0, session);
        SelectedSession = session;
    }

    [RelayCommand]
    private void OpenSession(OptimizationSession session)
    {
        if (ActiveSession != null)
        {
            ActiveSession.Dispose();
        }
        var vm = serviceProvider.GetRequiredService<SessionViewModel>();
        vm.Load(session, SelectedModel);
        ActiveSession = vm;
    }

    [RelayCommand]
    private async Task DeleteSession(OptimizationSession session)
    {
        Sessions.Remove(session);
        await sessionRepo.DeleteAsync(session.Id);
        if (ActiveSession?.Session?.Id == session.Id)
        {
            ActiveSession.Dispose();
            ActiveSession = null;
        }
    }

    [RelayCommand]
    private void StartRename(OptimizationSession session)
    {
        RenamingSession = session;
        RenameTitle = session.Title;
    }

    [RelayCommand]
    private async Task ConfirmRename()
    {
        if (RenamingSession is null) return;
        var trimmed = RenameTitle.Trim();
        if (!string.IsNullOrEmpty(trimmed))
        {
            RenamingSession.Title = trimmed;
            await sessionRepo.SaveAsync(RenamingSession);
            // 强制刷新列表项（Model 不实现 INotifyPropertyChanged）
            var idx = Sessions.IndexOf(RenamingSession);
            if (idx >= 0) { Sessions.RemoveAt(idx); Sessions.Insert(idx, RenamingSession); }
        }
        RenamingSession = null;
    }

    [RelayCommand]
    private void CancelRename() => RenamingSession = null;

    public event Func<ApiLogViewModel, Task>? ApiLogRequested;
    public event Func<TokenStatsViewModel, Task>? TokenStatsRequested;
    public event Func<SettingsViewModel, Task>? SettingsRequested;

    [RelayCommand]
    private async Task OpenApiLogAsync()
    {
        var vm = serviceProvider.GetRequiredService<ApiLogViewModel>();
        if (ApiLogRequested != null)
            await ApiLogRequested.Invoke(vm);
    }

    [RelayCommand]
    private async Task OpenTokenStatsAsync()
    {
        var vm = serviceProvider.GetRequiredService<TokenStatsViewModel>();
        await vm.LoadAsync();
        if (TokenStatsRequested != null)
            await TokenStatsRequested.Invoke(vm);
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        var vm = serviceProvider.GetRequiredService<SettingsViewModel>();
        await vm.InitializeAsync();
        if (SettingsRequested != null)
            await SettingsRequested.Invoke(vm);

        // Refresh model list after settings close
        var profiles = await modelRepo.GetAllAsync();
        ModelProfiles = new ObservableCollection<ModelProfile>(profiles);
        SelectedModel = ModelProfiles.FirstOrDefault(p => p.IsDefault) ?? ModelProfiles.FirstOrDefault();
    }
}
