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
    public Task<IEnumerable<OptimizationSession>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        var rows = conn.Query("SELECT * FROM Sessions ORDER BY ModifiedAt DESC");
        return Task.FromResult(rows.Select(MapRow).AsEnumerable());
    }

    /// <inheritdoc />
    public Task<OptimizationSession?> GetByIdAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var row = conn.QueryFirstOrDefault(
            "SELECT * FROM Sessions WHERE Id = @Id",
            new { Id = id.ToString() });
        return Task.FromResult(row is null ? null : MapRow(row));
    }

    /// <inheritdoc />
    public Task SaveAsync(OptimizationSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        session.ModifiedAt = DateTime.UtcNow;
        var treeJson = JsonSerializer.Serialize(session.ConversationTree);

        using var conn = _db.CreateConnection();
        conn.Execute("""
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

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        conn.Execute("DELETE FROM Sessions WHERE Id = @Id", new { Id = id.ToString() });
        return Task.CompletedTask;
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
