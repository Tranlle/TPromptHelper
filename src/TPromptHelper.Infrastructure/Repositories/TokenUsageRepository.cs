using Dapper;
using TPromptHelper.Core.Interfaces;
using TPromptHelper.Core.Models;
using TPromptHelper.Infrastructure.Database;

namespace TPromptHelper.Infrastructure.Repositories;

/// <summary>
/// Token 使用记录仓储实现，使用 Dapper 进行 SQLite 数据访问
/// </summary>
public sealed class TokenUsageRepository : ITokenUsageRepository
{
    private readonly AppDatabase _db;

    public TokenUsageRepository(AppDatabase db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <inheritdoc />
    public Task SaveAsync(TokenUsageRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        using var conn = _db.CreateConnection();
        conn.Execute("""
            INSERT INTO TokenUsageRecords
                (Id, Timestamp, Provider, ModelName, PromptTokens, CompletionTokens, TotalTokens, EstimatedCost, Currency)
            VALUES
                (@Id, @Timestamp, @Provider, @ModelName, @PromptTokens, @CompletionTokens, @TotalTokens, @EstimatedCost, @Currency)
            """, new
        {
            Id = record.Id.ToString(),
            Timestamp = record.Timestamp.ToString("O"),
            record.Provider,
            record.ModelName,
            record.PromptTokens,
            record.CompletionTokens,
            record.TotalTokens,
            record.EstimatedCost,
            record.Currency
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IEnumerable<TokenUsageRecord>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        var rows = conn.Query("SELECT * FROM TokenUsageRecords ORDER BY Timestamp DESC");
        return Task.FromResult(rows.Select(MapRow).AsEnumerable());
    }

    /// <inheritdoc />
    public Task ClearAsync()
    {
        using var conn = _db.CreateConnection();
        conn.Execute("DELETE FROM TokenUsageRecords");
        return Task.CompletedTask;
    }

    private static TokenUsageRecord MapRow(dynamic row) => new()
    {
        Id = Guid.Parse(row.Id),
        Timestamp = DateTime.Parse(row.Timestamp),
        Provider = row.Provider,
        ModelName = row.ModelName,
        PromptTokens = (int)row.PromptTokens,
        CompletionTokens = (int)row.CompletionTokens,
        TotalTokens = (int)row.TotalTokens,
        EstimatedCost = row.EstimatedCost is null ? null : (double?)row.EstimatedCost,
        Currency = row.Currency ?? string.Empty
    };
}
