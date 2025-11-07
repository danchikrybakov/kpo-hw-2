using FinanceTracker.Repository;

namespace FinanceTracker.Exporters;

/// <summary>
/// Базовый контракт экспортёра. Конкретные экспортёры (JSON/CSV/YAML)
/// реализуют единственный метод <see cref="Export"/> и получают доступ к данным
/// через репозитории, не зная деталей их хранения.
/// </summary>
public abstract class BaseExporter
{
    /// <summary>
    /// Выполняет экспорт текущего состояния данных в указанный путь.
    /// </summary>
    /// <param name="path">
    /// Куда писать результат:
    /// для JSON/YAML — путь к файлу; для CSV — путь к каталогу (ожидаются 3 файла).
    /// </param>
    /// <param name="acc">Репозиторий счетов.</param>
    /// <param name="cat">Репозиторий категорий.</param>
    /// <param name="op">Репозиторий операций.</param>
    public abstract void Export(string path, IBankAccountRepository acc, ICategoryRepository cat, IOperationRepository op);
}