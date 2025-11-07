using System.IO;
using FinanceTracker.Exporters;
using FinanceTracker.Repository;

namespace FinanceTracker.Services;

/// <summary>
/// Сервис «снимка состояния»: при вызове <see cref="Save"/> сериализует текущее
/// состояние репозиториев через переданный экспортер в указанный путь.
/// Формат/структуру вывода определяет инжектированный <see cref="BaseExporter"/>.
/// </summary>
public sealed class FileSnapshotWriter : ISnapshotWriter
{
    private readonly string _path;
    private readonly IBankAccountRepository _acc;
    private readonly ICategoryRepository _cat;
    private readonly IOperationRepository _op;
    private readonly BaseExporter _exporter;

    /// <param name="path">Целевой путь для снапшота (файл для JSON/YAML или каталог для CSV).</param>
    /// <param name="acc">Репозиторий счетов.</param>
    /// <param name="cat">Репозиторий категорий.</param>
    /// <param name="op">Репозиторий операций.</param>
    /// <param name="exporter">Конкретный экспортер (JSON/YAML/CSV).</param>
    public FileSnapshotWriter(
        string path,
        IBankAccountRepository acc,
        ICategoryRepository cat,
        IOperationRepository op,
        BaseExporter exporter)
    {
        _path     = path;
        _acc      = acc;
        _cat      = cat;
        _op       = op;
        _exporter = exporter;
    }

    /// <summary>
    /// Сохраняет актуальное состояние в <see cref="_path"/>.
    /// </summary>
    public void Save()
    {
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        // Делегируем формат и структуру вывода конкретному экспортеру.
        _exporter.Export(_path, _acc, _cat, _op);
    }
}