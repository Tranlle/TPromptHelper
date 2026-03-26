using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TPromptHelper.Core.Interfaces;
using TPromptHelper.Core.Models;

namespace TPromptHelper.Desktop.ViewModels;

public record ModelUsageStat(
    string ModelName,
    string Provider,
    int RequestCount,
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    double? EstimatedCost,
    string Currency)
{
    public string CostDisplay => EstimatedCost.HasValue
        ? $"{Currency}{EstimatedCost.Value:F6}"
        : "—";
};

public partial class TokenStatsViewModel(ITokenUsageRepository repo) : ViewModelBase
{
    [ObservableProperty] private int _totalRequests;
    [ObservableProperty] private int _totalPromptTokens;
    [ObservableProperty] private int _totalCompletionTokens;
    [ObservableProperty] private int _totalTokens;
    [ObservableProperty] private string _totalCostDisplay = "—";
    [ObservableProperty] private ObservableCollection<ModelUsageStat> _modelStats = [];

    public async Task LoadAsync()
    {
        var records = (await repo.GetAllAsync()).ToList();

        TotalRequests = records.Count;
        TotalPromptTokens = records.Sum(r => r.PromptTokens);
        TotalCompletionTokens = records.Sum(r => r.CompletionTokens);
        TotalTokens = records.Sum(r => r.TotalTokens);

        var costsByCurrency = records
            .Where(r => r.EstimatedCost.HasValue && !string.IsNullOrEmpty(r.Currency))
            .GroupBy(r => r.Currency)
            .Select(g => $"{g.Key}{g.Sum(r => r.EstimatedCost!.Value):F6}")
            .ToList();
        TotalCostDisplay = costsByCurrency.Count > 0 ? string.Join("  /  ", costsByCurrency) : "—";

        var stats = records
            .GroupBy(r => (r.ModelName, r.Provider, r.Currency))
            .Select(g =>
            {
                var hasCost = g.Any(r => r.EstimatedCost.HasValue);
                double? cost = hasCost ? g.Sum(r => r.EstimatedCost ?? 0) : null;
                return new ModelUsageStat(
                    g.Key.ModelName,
                    g.Key.Provider,
                    g.Count(),
                    g.Sum(r => r.PromptTokens),
                    g.Sum(r => r.CompletionTokens),
                    g.Sum(r => r.TotalTokens),
                    cost,
                    g.Key.Currency);
            })
            .OrderByDescending(s => s.TotalTokens)
            .ToList();

        ModelStats = new ObservableCollection<ModelUsageStat>(stats);
    }

    [RelayCommand]
    private async Task ClearAsync()
    {
        await repo.ClearAsync();
        await LoadAsync();
    }
}
