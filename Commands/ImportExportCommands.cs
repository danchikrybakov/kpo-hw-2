using Spectre.Console;
using FinanceTracker.Importers;
using FinanceTracker.Exporters;
using FinanceTracker.Repository;

namespace FinanceTracker.Commands;

/// <summary>
/// Команда импорта данных во внутренние репозитории приложения.
/// </summary>
public sealed class ImportCommand : ICommand
{
    private readonly BaseImporter _imp; 
    private readonly IBankAccountRepository _acc;
    private readonly ICategoryRepository _cat; 
    private readonly IOperationRepository _op;
    private readonly string _path;

    /// <param name="imp">Конкретный импортёр (csv/json/yaml).</param>
    /// <param name="acc">Репозиторий счетов.</param>
    /// <param name="cat">Репозиторий категорий.</param>
    /// <param name="op">Репозиторий операций.</param>
    /// <param name="path">Путь к источнику данных (файл или каталог, зависит от формата).</param>
    public ImportCommand(BaseImporter imp, IBankAccountRepository acc, ICategoryRepository cat, IOperationRepository op, string path)
    { _imp=imp; _acc=acc; _cat=cat; _op=op; _path=path; }

    /// <summary>
    /// Полностью очищает текущее состояние и загружает данные из источника.
    /// </summary>
    public int Execute()
    {
        // Импорт всегда начинается «с чистого листа», чтобы не смешивать с предыдущими данными.
        _acc.Clear(); _cat.Clear(); _op.Clear();

        _imp.Import(_path, new ImportTarget(_acc, _cat, _op));

        // Краткий отчёт по количествам объектов, попавшим в память.
        AnsiConsole.MarkupLine("[green]Импортировано:[/] accounts={0}, categories={1}, operations={2}",
            _acc.All().Count, _cat.All().Count, _op.All().Count);
        return 0;
    }
}

/// <summary>
/// Команда экспорта: либо экспорт текущего состояния, либо связка import→export в один шаг.
/// </summary>
public sealed class ExportCommand : ICommand
{
    private readonly BaseExporter _exp; 
    private readonly IBankAccountRepository _acc;
    private readonly ICategoryRepository _cat; 
    private readonly IOperationRepository _op;
    private readonly string _dest;

    // Если заданы, выполняется промежуточный импорт из src перед экспортом.
    private readonly BaseImporter? _imp; 
    private readonly string? _src; 
    private readonly string _outFmt;

    /// <param name="exp">Экспортёр (json/csv/yaml).</param>
    /// <param name="acc">Репозиторий счетов (источник данных для экспорта).</param>
    /// <param name="cat">Репозиторий категорий.</param>
    /// <param name="op">Репозиторий операций.</param>
    /// <param name="outFmt">Формат вывода: json | csv | yaml.</param>
    /// <param name="dest">Путь назначения: файл для json/yaml, каталог для csv.</param>
    /// <param name="imp">Необязательный импортёр для связки import→export.</param>
    /// <param name="src">Необязательный источник для связки import→export.</param>
    public ExportCommand(BaseExporter exp, IBankAccountRepository acc, ICategoryRepository cat, IOperationRepository op,
                         string outFmt, string dest, BaseImporter? imp=null, string? src=null)
    { _exp=exp; _acc=acc; _cat=cat; _op=op; _outFmt=outFmt; _dest=dest; _imp=imp; _src=src; }

    /// <summary>
    /// Выполняет экспорт. Если задан импортёр и путь источника — сначала выполняется импорт (с очисткой).
    /// </summary>
    public int Execute()
    {
        if (_imp is not null && _src is not null)
        {
            // Для «сквозного» сценария import→export сначала подменяем состояние на данные из src.
            _acc.Clear(); _cat.Clear(); _op.Clear();
            _imp.Import(_src, new ImportTarget(_acc, _cat, _op));
        }

        var finalDest = PrepareDest(_outFmt, _dest);
        _exp.Export(finalDest, _acc, _cat, _op);

        // Сообщение подтверждения: формат → путь назначения.
        Spectre.Console.AnsiConsole.MarkupLine("[green]Экспортировано:[/] {0} → {1}", _outFmt, _dest);
        return 0;
    }

    /// <summary>
    /// Нормализация пути назначения: для CSV — всегда каталог; для JSON/YAML — файл
    /// (создаём родительскую директорию, если её нет).
    /// </summary>
    private static string PrepareDest(string fmtOut, string dest)
    {
        var f = fmtOut.ToLowerInvariant();
        if (f == "csv"){ Directory.CreateDirectory(dest); return dest; }
        var dir = Path.GetDirectoryName(dest);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        return dest;
    }
}
