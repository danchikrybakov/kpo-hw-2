namespace FinanceTracker.Importers.CsvSources;

/// <summary>
/// Набор «сырых» CSV-данных для импорта.
/// Предполагается, что дальнейший парсинг выполняет <c>CsvImporter</c>.
/// </summary>
/// <param name="Accounts">Текст CSV для секции accounts (первая строка — заголовок).</param>
/// <param name="Categories">Текст CSV для секции categories (первая строка — заголовок).</param>
/// <param name="Operations">Текст CSV для секции operations (первая строка — заголовок).</param>
/// <param name="Delimiter">
/// Необязательный заранее обнаруженный разделитель полей (напр., <c>','</c>, <c>';'</c>, <c>'\t'</c>).
/// Если <c>null</c> — импортёр сам определит разделитель по содержимому.
/// </param>
public readonly record struct CsvChunks(string Accounts, string Categories, string Operations, char? Delimiter);

/// <summary>
/// Абстракция источника данных для CSV-импортёра.
/// Источник может быть папкой с тремя файлами или единым «секционированным» файлом.
/// Возвращает «сырые» тексты таблиц без разборки строк/ячеек.
/// </summary>
public interface ICsvSource
{
    /// <summary>
    /// Считывает CSV-данные из указанного источника.
    /// </summary>
    /// <param name="path">Путь к каталогу или к единому CSV-файлу.</param>
    /// <returns>Три «куска» CSV (accounts/categories/operations) и, при наличии, обнаруженный разделитель.</returns>
    /// <exception cref="Exception">Если источник отсутствует или недоступен.</exception>
    CsvChunks Read(string path);
}