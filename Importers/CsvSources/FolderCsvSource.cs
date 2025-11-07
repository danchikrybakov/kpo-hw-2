using System.Text;

namespace FinanceTracker.Importers.CsvSources;

/// <summary>
/// Источник CSV «папка»: ожидает три файла в каталоге:
/// <c>accounts.csv</c>, <c>categories.csv</c>, <c>operations.csv</c>.
/// Ничего не парсит — только читает сырое содержимое для последующей обработки импортёром.
/// </summary>
public sealed class FolderCsvSource : ICsvSource
{
    /// <summary>
    /// Читает три CSV из каталога <paramref name="path"/> и возвращает их как «чанки».
    /// </summary>
    /// <exception cref="Exception">
    /// Бросает, если каталога нет или отсутствует любой из требуемых файлов.
    /// </exception>
    public CsvChunks Read(string path)
    {
        var baseDir = Path.GetFullPath(path);
        if (!Directory.Exists(baseDir))
            throw new Exception($"CSV: каталог не найден: {baseDir}");

        string ReadFile(string name)
        {
            var p = Path.Combine(baseDir, name);
            if (!File.Exists(p))
                throw new Exception($"CSV: не найден файл '{name}' в каталоге {baseDir}");
            // Читаем как UTF-8 (без заботы о BOM): CsvImporter сам обрежет BOM при разборе.
            return File.ReadAllText(p, Encoding.UTF8);
        }

        var acc = ReadFile("accounts.csv");
        var cat = ReadFile("categories.csv");
        var op  = ReadFile("operations.csv");

        // Единый секционированный CSV не используем — потому четвёртый аргумент null.
        return new CsvChunks(acc, cat, op, null);
    }
}