using TPromptHelper.Core.Interfaces;
using TPromptHelper.Core.Models;

namespace TPromptHelper.Services;

/// <summary>
/// 内存中的 API 日志记录器实现，用于调试和审计
/// </summary>
public sealed class ApiLogger : IApiLogger
{
    private readonly List<ApiLogEntry> _entries = [];
    private readonly object _lock = new();

    /// <inheritdoc />
    public IReadOnlyList<ApiLogEntry> Entries
    {
        get
        {
            lock (_lock)
            {
                return _entries.ToList();
            }
        }
    }

    /// <inheritdoc />
    public event Action? LogChanged;

    /// <inheritdoc />
    public void Log(ApiLogEntry entry)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        lock (_lock)
        {
            _entries.Insert(0, entry);
        }
        LogChanged?.Invoke();
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
        }
        LogChanged?.Invoke();
    }
}
