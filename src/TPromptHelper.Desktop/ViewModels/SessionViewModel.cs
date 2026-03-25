using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TPromptHelper.Core.Interfaces;
using TPromptHelper.Core.Models;

namespace TPromptHelper.Desktop.ViewModels;

public partial class SessionViewModel(IPromptOptimizer optimizer, ISessionRepository sessionRepo) : ViewModelBase
{
    [ObservableProperty] private OptimizationSession? _session;
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(OptimizeCommand))] private ModelProfile? _model;
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(OptimizeCommand))] private string _rawInput = string.Empty;
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(ContinueOptimizeCommand))] private string _optimizedOutput = string.Empty;
    [ObservableProperty] private OptimizationStrategy _selectedStrategy = OptimizationStrategy.Structured;
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(OptimizeCommand))] private bool _isOptimizing;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _continueSuggestion = string.Empty;
    [ObservableProperty] private ObservableCollection<ConversationNode> _history = [];

    public OptimizationStrategy[] Strategies { get; } = Enum.GetValues<OptimizationStrategy>();
    private CancellationTokenSource? _cts;

    public void Load(OptimizationSession session, ModelProfile? model)
    {
        Session = session;
        Model = model;
        SelectedStrategy = session.DefaultStrategy;
        History = new ObservableCollection<ConversationNode>(FlattenTree(session.ConversationTree));
    }

    [RelayCommand(CanExecute = nameof(CanOptimize))]
    private async Task OptimizeAsync()
    {
        if (Model is null || Session is null) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        IsOptimizing = true;
        OptimizedOutput = string.Empty;
        StatusMessage = "优化中...";

        try
        {
            await foreach (var chunk in optimizer.OptimizeStreamAsync(RawInput, SelectedStrategy, Model, _cts.Token))
            {
                OptimizedOutput += chunk;
            }

            var node = new ConversationNode
            {
                RawInput = RawInput,
                OptimizedOutput = OptimizedOutput,
                Strategy = SelectedStrategy,
                ModelProfileId = Model.Id
            };
            Session.ConversationTree.Add(node);
            History.Add(node);
            await sessionRepo.SaveAsync(Session);
            StatusMessage = "优化完成";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "已取消";
        }
        catch (Exception ex)
        {
            StatusMessage = $"错误：{ex.Message}";
        }
        finally
        {
            IsOptimizing = false;
        }
    }

    [RelayCommand]
    private void CancelOptimize() => _cts?.Cancel();

    [RelayCommand]
    private async Task CopyOutputAsync()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime { MainWindow: { } win })
            await (win.Clipboard?.SetTextAsync(OptimizedOutput) ?? Task.CompletedTask);
    }

    [RelayCommand(CanExecute = nameof(CanContinueOptimize))]
    private void ContinueOptimize()
    {
        RawInput = string.IsNullOrWhiteSpace(ContinueSuggestion)
            ? OptimizedOutput
            : $"{OptimizedOutput}\n\n【优化建议】{ContinueSuggestion.Trim()}";
        OptimizedOutput = string.Empty;
        ContinueSuggestion = string.Empty;
    }

    [RelayCommand]
    private void LoadFromHistory(ConversationNode node)
    {
        RawInput = node.RawInput;
        OptimizedOutput = node.OptimizedOutput;
        SelectedStrategy = node.Strategy;
    }

    private bool CanOptimize() => !IsOptimizing && !string.IsNullOrWhiteSpace(RawInput) && Model is not null;
    private bool CanContinueOptimize() => !string.IsNullOrWhiteSpace(OptimizedOutput);

    private static IEnumerable<ConversationNode> FlattenTree(List<ConversationNode> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            foreach (var child in FlattenTree(node.Children))
                yield return child;
        }
    }
}
