using System.Globalization;
using System.Text;
using FinanceTracker.Domain;

namespace FinanceTracker.Export.Visitors;

/// <summary>
/// Визитёр для экспорта в CSV. 
/// Формирует три независимых CSV-потока (accounts/categories/operations) со snake_case-заголовками.
/// </summary>
public sealed class CsvExportVisitor : IFinanceVisitor
{
    private readonly char _delim;
    private readonly StringBuilder _acc = new();
    private readonly StringBuilder _cat = new();
    private readonly StringBuilder _op  = new();

    /// <param name="delimiter">Разделитель полей (по умолчанию <c>,</c>).</param>
    public CsvExportVisitor(char delimiter = ',')
    {
        _delim = delimiter;
    }

    /// <summary>Готовый CSV для таблицы <c>accounts</c>.</summary>
    public string AccountsCsv   => _acc.ToString();

    /// <summary>Готовый CSV для таблицы <c>categories</c>.</summary>
    public string CategoriesCsv => _cat.ToString();

    /// <summary>Готовый CSV для таблицы <c>operations</c>.</summary>
    public string OperationsCsv => _op.ToString();

    /// <summary>
    /// Инициализация выгрузки: записываются заголовки колонок в каждый CSV-буфер.
    /// </summary>
    public void Begin()
    {
        WriteHeader(_acc, new[] { "id", "name", "balance" });
        WriteHeader(_cat, new[] { "id", "type", "name" });
        WriteHeader(_op,  new[] { "id", "type", "bank_account_id", "category_id", "amount", "date", "description" });
    }

    /// <summary>Добавляет строку в CSV <c>accounts</c>.</summary>
    public void Visit(BankAccount a)
        => WriteRow(_acc, a.Id, a.Name, a.Balance);

    /// <summary>Добавляет строку в CSV <c>categories</c>.</summary>
    public void Visit(Category c)
        => WriteRow(_cat, c.Id, c.Type.ToString().ToLowerInvariant(), c.Name);

    /// <summary>Добавляет строку в CSV <c>operations</c>.</summary>
    public void Visit(Operation o)
        => WriteRow(_op, o.Id, o.Type.ToString().ToLowerInvariant(), o.BankAccountId, o.CategoryId, o.Amount, o.Date, o.Description ?? "");

    /// <summary>
    /// Завершение экспорта. Ничего не делает, оставлено для симметрии и будущих расширений.
    /// </summary>
    public void End() { }

    // --- служебные методы ---

    /// <summary>Пишет строку заголовка в указанный буфер.</summary>
    private void WriteHeader(StringBuilder sb, IEnumerable<string> cols)
        => sb.AppendLine(string.Join(_delim, cols.Select(Escape)));

    /// <summary>
    /// Пишет строку данных в указанный буфер с экранированием и инвариантным форматированием чисел.
    /// </summary>
    private void WriteRow(StringBuilder sb, params object?[] cells)
        => sb.AppendLine(string.Join(_delim, cells.Select(c => Escape(ToInvariant(c)))));

    /// <summary>
    /// Приводит значения к строке с инвариантной культурой (для чисел — точка как разделитель).
    /// </summary>
    private static string ToInvariant(object? x) => x switch
    {
        null      => "",
        double d  => d.ToString(CultureInfo.InvariantCulture),
        float f   => f.ToString(CultureInfo.InvariantCulture),
        decimal m => m.ToString(CultureInfo.InvariantCulture),
        _         => x?.ToString() ?? ""
    };

    /// <summary>
    /// Экранирует поле для CSV: если содержит разделитель/кавычки/переводы строк — 
    /// обрамляет в кавычки и удваивает внутренние кавычки.
    /// </summary>
    private string Escape(string s)
    {
        if (s.IndexOfAny(new[] { _delim, '"', '\n', '\r' }) >= 0)
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }
}
