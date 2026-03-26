using System.Text.Json;
using Dapper;
using TPromptHelper.Core.Interfaces;
using TPromptHelper.Core.Models;
using TPromptHelper.Infrastructure.Database;

namespace TPromptHelper.Infrastructure.Repositories;

/// <summary>
/// 会话仓储实现，使用 Dapper 进行 SQLite 数据访问
/// </summary>
public sealed class SessionRepository : ISessionRepository
{
    private readonly AppDatabase _db;

    public SessionRepository(AppDatabase db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OptimizationSession>> GetAllAsync()
    {
        await using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync("SELECT * FROM Sessions ORDER BY ModifiedAt DESC");
        return rows.Select(MapRow);
    }

    /// <inheritdoc />
    public async Task<OptimizationSession?> GetByIdAsync(Guid id)
    {
        await using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync(
            "SELECT * FROM Sessions WHERE Id = @Id",
            new { Id = id.ToString() });
        return row is null ? null : MapRow(row);
    }

    /// <inheritdoc />
    public async Task SaveAsync(OptimizationSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        session.ModifiedAt = DateTime.UtcNow;
        var treeJson = JsonSerializer.Serialize(session.ConversationTree);

        await using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("""
            INSERT INTO Sessions (Id, Title, CreatedAt, ModifiedAt, DefaultStrategy, CurrentModelProfileId, ConversationTreeJson)
            VALUES (@Id, @Title, @CreatedAt, @ModifiedAt, @DefaultStrategy, @CurrentModelProfileId, @ConversationTreeJson)
            ON CONFLICT(Id) DO UPDATE SET
                Title = excluded.Title,
                ModifiedAt = excluded.ModifiedAt,
                DefaultStrategy = excluded.DefaultStrategy,
                CurrentModelProfileId = excluded.CurrentModelProfileId,
                ConversationTreeJson = excluded.ConversationTreeJson
            """, new
        {
            Id = session.Id.ToString(),
            session.Title,
            CreatedAt = session.CreatedAt.ToString("O"),
            ModifiedAt = session.ModifiedAt.ToString("O"),
            DefaultStrategy = (int)session.DefaultStrategy,
            CurrentModelProfileId = session.CurrentModelProfileId?.ToString(),
            ConversationTreeJson = treeJson
        });
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        await using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM Sessions WHERE Id = @Id", new { Id = id.ToString() });
    }

    private static OptimizationSession MapRow(dynamic row) => new()
    {
        Id = Guid.Parse(row.Id),
        Title = row.Title,
        CreatedAt = DateTime.Parse(row.CreatedAt),
        ModifiedAt = DateTime.Parse(row.ModifiedAt),
        DefaultStrategy = (OptimizationStrategy)(int)row.DefaultStrategy,
        CurrentModelProfileId = row.CurrentModelProfileId is null ? null : Guid.Parse(row.CurrentModelProfileId),
        ConversationTree = JsonSerializer.Deserialize<List<ConversationNode>>((string)row.ConversationTreeJson) ?? []
    };
}
