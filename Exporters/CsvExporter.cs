using System.Text;
using FinanceTracker.Repository;
using FinanceTracker.Export.Visitors;

namespace FinanceTracker.Exporters;

/// <summary>
/// Экспорт в каталог из трёх CSV-файлов: accounts.csv, categories.csv, operations.csv.
/// Порядок строк сохраняет порядок репозитория (как на входе; новые элементы — в конец).
/// </summary>
public sealed class CsvExporter : BaseExporter
{
    /// <summary>
    /// Пишет три CSV-файла в каталог <paramref name="dest"/>.
    /// Предполагается, что <paramref name="dest"/> — именно каталог (создаётся при необходимости).
    /// </summary>
    public override void Export(string dest, IBankAccountRepository acc, ICategoryRepository cat, IOperationRepository op)
    {
        Directory.CreateDirectory(dest);

        // Если понадобится другой разделитель — поменяйте конструктор CsvExportVisitor.
        var visitor = new CsvExportVisitor(',');

        // Обход доменных объектов в порядке, который возвращают репозитории (insertion order).
        visitor.Begin();
        foreach (var a in acc.All()) visitor.Visit(a);
        foreach (var c in cat.All()) visitor.Visit(c);
        foreach (var o in op.All())  visitor.Visit(o);
        visitor.End();

        // Пишем без BOM, чтобы не мешать разбору в сторонних CSV-парсерах.
        var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        File.WriteAllText(Path.Combine(dest, "accounts.csv"),   visitor.AccountsCsv,   utf8NoBom);
        File.WriteAllText(Path.Combine(dest, "categories.csv"), visitor.CategoriesCsv, utf8NoBom);
        File.WriteAllText(Path.Combine(dest, "operations.csv"), visitor.OperationsCsv, utf8NoBom);
    }
}