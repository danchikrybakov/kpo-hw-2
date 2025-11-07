using FinanceTracker.Repository;

namespace FinanceTracker.Importers;

/// <summary>
/// Точка записи для импортёров: набор репозиториев, в которые попадают распарсенные
/// счета, категории и операции. Передаётся в <c>BaseImporter.Import(...)</c>.
/// </summary>
/// <remarks>
/// • Импортёр не должен сам чистить репозитории — это делает вызывающая команда
///   (например, <c>import</c> или запуск <c>repl</c>).
/// • Предполагается, что реализации репозиториев сохраняют порядок вставки,
///   чтобы при экспорте сохранялся исходный порядок данных.
/// </remarks>
/// <param name="Accounts">Репозиторий счетов (создание/обновление/получение).</param>
/// <param name="Categories">Репозиторий категорий (income/expense).</param>
/// <param name="Operations">Репозиторий операций.</param>
public readonly record struct ImportTarget(
    IBankAccountRepository Accounts,
    ICategoryRepository    Categories,
    IOperationRepository   Operations
);