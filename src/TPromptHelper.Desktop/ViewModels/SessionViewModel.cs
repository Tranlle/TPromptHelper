using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TPromptHelper.Core.Interfaces;
using TPromptHelper.Core.Models;

namespace TPromptHelper.Desktop.ViewModels;

public partial class SessionViewModel : ViewModelBase, IDisposable
{
    private readonly IPromptOptimizer _optimizer;
    private readonly ISessionRepository _sessionRepo;
    private readonly System.Timers.Timer _typewriterTimer;
    private readonly StringBuilder _pendingOutput = new();
    private CancellationTokenSource? _cts;
    private bool _isTypewriterRunning;
    private const int TypewriterDelayMs = 15; // 每字符延迟15ms，60+字符/秒

    [ObservableProperty] private OptimizationSession? _session;
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(OptimizeCommand))] private ModelProfile? _model;
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(OptimizeCommand))] private string _rawInput = string.Empty;
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(ContinueOptimizeCommand))] private string _optimizedOutput = string.Empty;
    [ObservableProperty] private OptimizationStrategy _selectedStrategy = OptimizationStrategy.Structured;
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(OptimizeCommand))] private bool _isOptimizing;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _continueSuggestion = string.Empty;
    [ObservableProperty] private ObservableCollection<ConversationNode> _history = [];
    [ObservableProperty] private bool _isTypewriterActive;

    public OptimizationStrategy[] Strategies { get; } = Enum.GetValues<OptimizationStrategy>();

    public SessionViewModel(IPromptOptimizer optimizer, ISessionRepository sessionRepo)
    {
        _optimizer = optimizer;
        _sessionRepo = sessionRepo;
        _typewriterTimer = new System.Timers.Timer(TypewriterDelayMs);
        _typewriterTimer.Elapsed += OnTypewriterTick;
        _typewriterTimer.AutoReset = true;
    }

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
        IsTypewriterActive = true;
        OptimizedOutput = string.Empty;
        _pendingOutput.Clear();
        StatusMessage = "优化中...";

        try
        {
            await foreach (var chunk in _optimizer.OptimizeStreamAsync(RawInput, SelectedStrategy, Model, _cts.Token))
            {
                _pendingOutput.Append(chunk);
                if (!_isTypewriterRunning)
                {
                    _isTypewriterRunning = true;
                    _typewriterTimer.Start();
                }
            }

            // 等待打字机效果完成
            while (_pendingOutput.Length > 0 && !_cts.Token.IsCancellationRequested)
            {
                await Task.Delay(50, _cts.Token);
            }

            // 确保最终文本完全显示
            if (OptimizedOutput != _pendingOutput.ToString())
            {
                OptimizedOutput = _pendingOutput.ToString();
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
            await _sessionRepo.SaveAsync(Session);
            StatusMessage = "优化完成";
        }
        catch (OperationCanceledException)
        {
            // 打字机效果立即停止
            _typewriterTimer.Stop();
            _isTypewriterRunning = false;
            _pendingOutput.Clear();
            StatusMessage = "已取消";
        }
        catch (Exception ex)
        {
            _typewriterTimer.Stop();
            _isTypewriterRunning = false;
            StatusMessage = $"错误：{ex.Message}";
        }
        finally
        {
            _typewriterTimer.Stop();
            _isTypewriterRunning = false;
            IsTypewriterActive = false;
            IsOptimizing = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void OnTypewriterTick(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (_pendingOutput.Length == 0)
        {
            _typewriterTimer.Stop();
            _isTypewriterRunning = false;
            return;
        }

        // 每次处理多个字符，减少UI更新频率，提升流畅度
        var charsToProcess = Math.Min(3, _pendingOutput.Length);
        var chunk = _pendingOutput.ToString(0, charsToProcess);
        _pendingOutput.Remove(0, charsToProcess);

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            OptimizedOutput += chunk;
        });
    }

    [RelayCommand]
    private void CancelOptimize()
    {
        _cts?.Cancel();
        _typewriterTimer.Stop();
        _isTypewriterRunning = false;
        _pendingOutput.Clear();
    }

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
    private bool CanContinueOptimize() => !IsOptimizing && !string.IsNullOrWhiteSpace(OptimizedOutput) && !IsTypewriterActive;

    private static IEnumerable<ConversationNode> FlattenTree(List<ConversationNode> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            foreach (var child in FlattenTree(node.Children))
                yield return child;
        }
    }

    public void Dispose()
    {
        _typewriterTimer.Stop();
        _typewriterTimer.Dispose();
        _cts?.Cancel();
        _cts?.Dispose();
        GC.SuppressFinalize(this);
    }
}
