using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using FinanceTracker.App;
using FinanceTracker.Exporters;
using FinanceTracker.Facade;
using FinanceTracker.Repository;
using FinanceTracker.Repository.InMemory;
using FinanceTracker.Repository.Proxies;
using FinanceTracker.Services;

/// Точка входа: настраиваем культуру, DI и запускаем CLI.
/// Репозитории — in-memory, поверх них прокси с автосохранением в YAML.
internal static class Program
{
    /// Путь для автоснапшотов после любых изменений (создание/редактирование/удаление).
    private const string AutoSavePath = "Data/autosave.yaml";

    public static async Task Main(string[] args)
    {
        // Единая инвариантная культура для чисел/парсинга.
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        var services = new ServiceCollection();

        // In-memory реализации как синглтоны.
        services.AddSingleton<InMemoryBankAccountRepository>();
        services.AddSingleton<InMemoryCategoryRepository>();
        services.AddSingleton<InMemoryOperationRepository>();

        // Экспортёр, который использует снапшот-сервис (формат YAML, snake_case).
        services.AddSingleton<BaseExporter, YamlExporter>();

        // Сервис автосохранения текущего состояния в файл.
        // Важно: он зависит от "внутренних" in-memory репозиториев, а не от прокси.
        services.AddSingleton<ISnapshotWriter>(sp =>
            new FileSnapshotWriter(
                AutoSavePath,
                sp.GetRequiredService<InMemoryBankAccountRepository>(),
                sp.GetRequiredService<InMemoryCategoryRepository>(),
                sp.GetRequiredService<InMemoryOperationRepository>(),
                sp.GetRequiredService<BaseExporter>()));

        // Привязка интерфейсов к прокси (поверх in-memory) — добавляют автосейв.
        services.AddSingleton<IBankAccountRepository>(sp =>
            new FileBackedBankAccountRepositoryProxy(
                sp.GetRequiredService<InMemoryBankAccountRepository>(),
                sp.GetRequiredService<ISnapshotWriter>()));

        services.AddSingleton<ICategoryRepository>(sp =>
            new FileBackedCategoryRepositoryProxy(
                sp.GetRequiredService<InMemoryCategoryRepository>(),
                sp.GetRequiredService<ISnapshotWriter>()));

        services.AddSingleton<IOperationRepository>(sp =>
            new FileBackedOperationRepositoryProxy(
                sp.GetRequiredService<InMemoryOperationRepository>(),
                sp.GetRequiredService<ISnapshotWriter>()));

        // Фасад аналитики и печать таблиц, плюс сам CLI.
        services.AddSingleton<AnalyticsFacade>();
        services.AddSingleton<TablePrinter>();
        services.AddSingleton<Cli>();

        var provider = services.BuildServiceProvider();

        // Запуск CLI (если аргументов нет — выведет help).
        var cli = provider.GetRequiredService<Cli>();
        await cli.RunAsync(args);
    }
}
