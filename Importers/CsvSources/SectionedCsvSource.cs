using System.Text;

namespace FinanceTracker.Importers.CsvSources;

/// <summary>
/// Источник «единый CSV»: в одном файле содержатся три секции,
/// размеченные строками вида <c>#%table:accounts</c>, <c>#%table:categories</c>, <c>#%table:operations</c>.
/// Класс не парсит CSV-поля — он только разрезает файл на три текстовых блока.
/// </summary>
public sealed class SectionedCsvSource : ICsvSource
{
    private enum Sec { None, Accounts, Categories, Operations }

    public CsvChunks Read(string path)
    {
        if (!File.Exists(path))
            throw new Exception($"CSV: файл не найден: {path}");

        var text = File.ReadAllText(path, Encoding.UTF8);

        var acc = new StringBuilder();
        var cat = new StringBuilder();
        var op  = new StringBuilder();

        Sec sec = Sec.None;

        using var sr = new StringReader(text);
        string? line;
        bool first = true;

        while ((line = sr.ReadLine()) is not null)
        {
            // Уберём BOM только у самой первой физической строки файла
            if (first) { line = StripBom(line); first = false; }

            var trimmed = line.Trim();

            // Переключение секций по маркеру #%table:<name>
            if (trimmed.StartsWith("#%table:", StringComparison.OrdinalIgnoreCase))
            {
                var tag = NormalizeTag(trimmed.Substring(8));
                sec = tag switch
                {
                    "accounts"   => Sec.Accounts,
                    "categories" => Sec.Categories,
                    "operations" => Sec.Operations,
                    _ => throw new Exception($"CSV: неизвестная секция {tag}")
                };
                continue;
            }

            // Копим строки в соответствующий буфер секции
            switch (sec)
            {
                case Sec.Accounts:   acc.AppendLine(line); break;
                case Sec.Categories: cat.AppendLine(line); break;
                case Sec.Operations: op.AppendLine(line);  break;
                default:
                    // До первой секции допускаем только пустые строки
                    if (trimmed.Length == 0) { /* ok */ }
                    else throw new Exception("CSV: данные вне секции");
                    break;
            }
        }

        // Пытаемся угадать разделитель по верхней непустой строке любой из секций
        var autod = DetectDelimiter(acc.ToString())
                 ?? DetectDelimiter(cat.ToString())
                 ?? DetectDelimiter(op.ToString());

        // Минимальная валидация: все три секции должны присутствовать
        if (acc.Length == 0 || cat.Length == 0 || op.Length == 0)
            throw new Exception("CSV: требуется три секции: accounts, categories, operations");

        return new CsvChunks(acc.ToString(), cat.ToString(), op.ToString(), autod);
    }

    private static string StripBom(string s)
        => s.Length >= 1 && s[0] == '\uFEFF' ? s[1..] : s;

    /// <summary>Нормализуем имя секции: обрезаем пробелы, приводим к нижнему регистру и берём только [a-z0-9_].</summary>
    private static string NormalizeTag(string s)
    {
        s = s.Trim().ToLowerInvariant();
        var i = 0;
        while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
        return s[..i];
    }

    /// <summary>
    /// Простейшее определение разделителя по счёту наиболее частого символа из списка кандидатов.
    /// Возвращает <c>null</c>, если по строкам ничего не удалось предположить.
    /// </summary>
    private static char? DetectDelimiter(string content)
    {
        using var sr = new StringReader(content);
        string? line;
        while ((line = sr.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            int cComma = 0, cSemi = 0, cTab = 0, cPipe = 0;
            foreach (var ch in line)
            {
                if (ch == ',')  cComma++;
                else if (ch == ';') cSemi++;
                else if (ch == '\t') cTab++;
                else if (ch == '|')  cPipe++;
            }

            var bestCnt = 0;
            char? best = null;

            void consider(char ch, int cnt)
            {
                if (cnt > bestCnt) { bestCnt = cnt; best = ch; }
            }

            consider(',', cComma);
            consider(';', cSemi);
            consider('\t', cTab);
            consider('|', cPipe);

            if (bestCnt > 0) return best; // нашли кандидата на разделитель
        }
        return null;
    }
}
