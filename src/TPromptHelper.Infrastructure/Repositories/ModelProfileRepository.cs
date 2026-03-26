using Dapper;
using TPromptHelper.Core.Interfaces;
using TPromptHelper.Core.Models;
using TPromptHelper.Infrastructure.Database;

namespace TPromptHelper.Infrastructure.Repositories;

/// <summary>
/// 模型配置仓储实现，使用 Dapper 进行 SQLite 数据访问
/// </summary>
public sealed class ModelProfileRepository : IModelProfileRepository
{
    private readonly AppDatabase _db;

    public ModelProfileRepository(AppDatabase db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ModelProfile>> GetAllAsync()
    {
        await using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync("SELECT * FROM ModelProfiles ORDER BY CreatedAt ASC");
        return rows.Select(MapRow);
    }

    /// <inheritdoc />
    public async Task<ModelProfile?> GetByIdAsync(Guid id)
    {
        await using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync(
            "SELECT * FROM ModelProfiles WHERE Id = @Id",
            new { Id = id.ToString() });
        return row is null ? null : MapRow(row);
    }

    /// <inheritdoc />
    public async Task<ModelProfile?> GetDefaultAsync()
    {
        await using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync(
            "SELECT * FROM ModelProfiles WHERE IsDefault = 1 LIMIT 1");
        return row is null ? null : MapRow(row);
    }

    /// <inheritdoc />
    public async Task SaveAsync(ModelProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        await using var conn = _db.CreateConnection();
        if (profile.IsDefault)
            await conn.ExecuteAsync("UPDATE ModelProfiles SET IsDefault = 0");

        await conn.ExecuteAsync("""
            INSERT INTO ModelProfiles (Id, Name, Provider, ApiEndpoint, ModelName, MaxTokens, Temperature, EncryptedApiKey, IsDefault, CreatedAt, Currency, InputPricePer1M, OutputPricePer1M)
            VALUES (@Id, @Name, @Provider, @ApiEndpoint, @ModelName, @MaxTokens, @Temperature, @EncryptedApiKey, @IsDefault, @CreatedAt, @Currency, @InputPricePer1M, @OutputPricePer1M)
            ON CONFLICT(Id) DO UPDATE SET
                Name = excluded.Name,
                Provider = excluded.Provider,
                ApiEndpoint = excluded.ApiEndpoint,
                ModelName = excluded.ModelName,
                MaxTokens = excluded.MaxTokens,
                Temperature = excluded.Temperature,
                EncryptedApiKey = excluded.EncryptedApiKey,
                IsDefault = excluded.IsDefault,
                Currency = excluded.Currency,
                InputPricePer1M = excluded.InputPricePer1M,
                OutputPricePer1M = excluded.OutputPricePer1M
            """, new
        {
            Id = profile.Id.ToString(),
            profile.Name,
            profile.Provider,
            profile.ApiEndpoint,
            profile.ModelName,
            profile.MaxTokens,
            profile.Temperature,
            profile.EncryptedApiKey,
            IsDefault = profile.IsDefault ? 1 : 0,
            CreatedAt = profile.CreatedAt.ToString("O"),
            profile.Currency,
            profile.InputPricePer1M,
            profile.OutputPricePer1M
        });
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        await using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM ModelProfiles WHERE Id = @Id", new { Id = id.ToString() });
    }

    private static ModelProfile MapRow(dynamic row) => new()
    {
        Id = Guid.Parse(row.Id),
        Name = row.Name,
        Provider = row.Provider,
        ApiEndpoint = row.ApiEndpoint,
        ModelName = row.ModelName,
        MaxTokens = (int)row.MaxTokens,
        Temperature = (double)row.Temperature,
        EncryptedApiKey = row.EncryptedApiKey,
        IsDefault = row.IsDefault == 1,
        CreatedAt = DateTime.Parse(row.CreatedAt),
        Currency = row.Currency ?? "¥",
        InputPricePer1M = row.InputPricePer1M ?? 0.0,
        OutputPricePer1M = row.OutputPricePer1M ?? 0.0,
    };
}
