using Microsoft.Extensions.DependencyInjection;
using TPromptHelper.Core.Interfaces;
using TPromptHelper.Infrastructure.Database;
using TPromptHelper.Infrastructure.Repositories;

namespace TPromptHelper.Infrastructure;

/// <summary>
/// 基础设施层服务注册扩展
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// 注册基础设施层服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="dbPath">SQLite 数据库文件路径</param>
    /// <returns>服务集合（用于链式调用）</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string dbPath)
    {
        // AppDatabase 使用 Singleton 是因为 SQLite 连接池由其管理
        services.AddSingleton(new AppDatabase(dbPath));

        // Repository 使用 Transient 确保线程安全
        // SQLiteConnection 不是线程安全的，每次操作创建新连接
        services.AddTransient<ISessionRepository, SessionRepository>();
        services.AddTransient<IModelProfileRepository, ModelProfileRepository>();
        services.AddTransient<ITokenUsageRepository, TokenUsageRepository>();

        return services;
    }
}
