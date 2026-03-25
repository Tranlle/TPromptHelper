using Microsoft.Extensions.DependencyInjection;
using TPromptHelper.Core.Interfaces;
using TPromptHelper.Desktop.ViewModels;
using TPromptHelper.Infrastructure;
using TPromptHelper.Services;

namespace TPromptHelper.Desktop;

public static class ServiceExtensions
{
    public static IServiceProvider BuildServiceProvider()
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TPromptHelper", "app.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        var services = new ServiceCollection();

        services.AddInfrastructure(dbPath);

        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddSingleton<IApiLogger, ApiLogger>();
        services.AddHttpClient<ILlmService, LlmService>()
            .AddStandardResilienceHandler();
        services.AddTransient<IPromptOptimizer, PromptOptimizer>();

        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<SessionViewModel>();
        services.AddTransient<ModelSettingsViewModel>();
        services.AddSingleton<ApiLogViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<TokenStatsViewModel>();

        return services.BuildServiceProvider();
    }
}
