using Dapper;
using Microsoft.Data.Sqlite;

namespace TPromptHelper.Infrastructure.Database;

/// <summary>
/// SQLite 数据库连接管理器，负责数据库初始化和连接创建
/// </summary>
public sealed class AppDatabase
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public AppDatabase(string dbPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dbPath);

        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        _connectionString = $"Data Source={dbPath}";
        Initialize();
    }

    /// <summary>
    /// 创建新的 SQLite 数据库连接
    /// </summary>
    /// <returns>已打开的数据库连接（调用方负责释放）</returns>
    public SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    /// <summary>
    /// 确保数据库已初始化
    /// </summary>
    private void EnsureInitialized()
    {
        if (_initialized) return;

        _initLock.Wait();
        try
        {
            if (_initialized) return;
            Initialize();
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private void Initialize()
    {
        using var conn = CreateConnection();

        conn.Execute("""
            CREATE TABLE IF NOT EXISTS Sessions (
                Id TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                ModifiedAt TEXT NOT NULL,
                DefaultStrategy INTEGER NOT NULL DEFAULT 0,
                CurrentModelProfileId TEXT,
                ConversationTreeJson TEXT NOT NULL DEFAULT '[]'
            );
            """);

        conn.Execute("""
            CREATE TABLE IF NOT EXISTS TokenUsageRecords (
                Id TEXT PRIMARY KEY,
                Timestamp TEXT NOT NULL,
                Provider TEXT NOT NULL,
                ModelName TEXT NOT NULL,
                PromptTokens INTEGER NOT NULL DEFAULT 0,
                CompletionTokens INTEGER NOT NULL DEFAULT 0,
                TotalTokens INTEGER NOT NULL DEFAULT 0,
                EstimatedCost REAL,
                Currency TEXT NOT NULL DEFAULT ''
            );
            """);

        conn.Execute("""
            CREATE TABLE IF NOT EXISTS ModelProfiles (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Provider TEXT NOT NULL,
                ApiEndpoint TEXT NOT NULL DEFAULT '',
                ModelName TEXT NOT NULL,
                MaxTokens INTEGER NOT NULL DEFAULT 2048,
                Temperature REAL NOT NULL DEFAULT 1.0,
                EncryptedApiKey TEXT NOT NULL DEFAULT '',
                IsDefault INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                Currency TEXT NOT NULL DEFAULT '¥',
                InputPricePer1M REAL NOT NULL DEFAULT 0,
                OutputPricePer1M REAL NOT NULL DEFAULT 0
            );
            """);
    }
}
