using System.Globalization;
using System.Linq;
using System.Text;
using FinanceTracker.Domain;
using FinanceTracker.Importers.CsvSources;

namespace FinanceTracker.Importers;

/// <summary>
/// Импортёр CSV. Поддерживает два источника:
/// 1) папка с тремя файлами (accounts.csv, categories.csv, operations.csv);
/// 2) единый файл с секциями, размеченными строками вида #%table:accounts|categories|operations.
/// Парсинг полей — RFC4180-like (кавычки, экранирование "" внутри).
/// </summary>
public sealed class CsvImporter : BaseImporter
{
    // ——— утилиты парсинга одной строки CSV ———
    private static string StripBom(string s) => s.Length >= 1 && s[0] == '\uFEFF' ? s[1..] : s;

    /// <summary>Бережно разбивает строку CSV по разделителю с учётом кавычек и "" внутри.</summary>
    private static List<string> SplitCsvLine(string line, char sep)
    {
        var res = new List<string>();
        var cur = new StringBuilder();
        bool inQ = false;

        for (int i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (inQ)
            {
                if (ch == '"')
                {
                    // удвоенная кавычка внутри строки → одна кавычка в значении
                    if (i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; }
                    else inQ = false;
                }
                else cur.Append(ch);
            }
            else
            {
                if (ch == '"') inQ = true;
                else if (ch == sep) { res.Add(cur.ToString().Trim()); cur.Clear(); }
                else cur.Append(ch);
            }
        }
        res.Add(cur.ToString().Trim());
        return res;
    }

    /// <summary>Грубое угадывание разделителя по первой строке заголовка.</summary>
    private static char GuessDelimFromHeader(string header)
    {
        int cComma = 0, cSemi = 0, cTab = 0, cPipe = 0;
        foreach (var ch in header)
        {
            if (ch == ',')  cComma++;
            else if (ch == ';') cSemi++;
            else if (ch == '\t') cTab++;
            else if (ch == '|')  cPipe++;
        }
        var best = new (char ch, int cnt)[] { (',', cComma), (';', cSemi), ('\t', cTab), ('|', cPipe) }
                   .OrderByDescending(x => x.cnt).First();
        return best.cnt > 0 ? best.ch : ',';
    }

    /// <summary>Точка входа: выбирает источник (папка/единый файл), режет на таблицы и загружает в репозитории.</summary>
    protected override ImportResult DoImport(string path, ImportTarget t)
    {
        // Adapter: источник — папка (3 файла) или единый секционный CSV
        ICsvSource src = Directory.Exists(path) ? new FolderCsvSource() : new SectionedCsvSource();
        var chunks = src.Read(path);

        var accTable = ParseTable(chunks.Accounts,   chunks.Delimiter);
        var catTable = ParseTable(chunks.Categories, chunks.Delimiter);
        var opTable  = ParseTable(chunks.Operations, chunks.Delimiter);

        int a = LoadAccounts(accTable, t);
        int c = LoadCategories(catTable, t);
        int o = LoadOperations(opTable, t);

        return new ImportResult(a, c, o);
    }

    /// <summary>
    /// Текст секции → таблица строк (список списков).
    /// Учитывает:
    ///  • BOM у первой строки,
    ///  • строку вида <c>sep=;</c> для явного выбора разделителя,
    ///  • если разделитель не задан — пытается угадать по заголовку.
    /// </summary>
    private static List<List<string>> ParseTable(string content, char? forcedDelim = null)
    {
        using var sr = new StringReader(content);
        string? line;
        bool first = true;
        var rows = new List<List<string>>();
        char delim = forcedDelim ?? ',';
        bool delimFixed = forcedDelim.HasValue;

        while ((line = sr.ReadLine()) is not null)
        {
            if (first) { line = StripBom(line); first = false; }

            var probe = line.Trim();
            if (probe.StartsWith("sep=", StringComparison.OrdinalIgnoreCase) && probe.Length >= 5)
            {
                // директива Excel/LibreOffice: первая строка «sep=;»
                delim = probe[4];
                delimFixed = true;
                continue;
            }

            if (!delimFixed && rows.Count == 0) delim = GuessDelimFromHeader(line);
            rows.Add(SplitCsvLine(line, delim));
        }
        return rows;
    }

    // ——— загрузчики секций (минимальная валидация заголовков) ———

    private int LoadAccounts(List<List<string>> table, ImportTarget it)
    {
        if (table.Count == 0) return 0;
        var head = Index(table[0]);
        int id = Need(head, "id"), name = Need(head, "name"), bal = Need(head, "balance");

        int cnt = 0;
        for (int r = 1; r < table.Count; r++)
        {
            var row = table[r]; if (Blank(row)) continue;
            long i   = long.Parse(Get(row, id), CultureInfo.InvariantCulture);
            string n = Get(row, name);
            double b = double.Parse(Get(row, bal), CultureInfo.InvariantCulture);
            it.Accounts.Add(Factory.MakeAccount(i, n, b));
            cnt++;
        }
        return cnt;
    }

    private int LoadCategories(List<List<string>> table, ImportTarget it)
    {
        if (table.Count == 0) return 0;
        var head = Index(table[0]);
        int id = Need(head, "id"), type = Need(head, "type"), name = Need(head, "name");

        int cnt = 0;
        for (int r = 1; r < table.Count; r++)
        {
            var row = table[r]; if (Blank(row)) continue;
            long i   = long.Parse(Get(row, id), CultureInfo.InvariantCulture);
            var tp   = Types.ParseOperationType(Get(row, type));
            string n = Get(row, name);
            it.Categories.Add(Factory.MakeCategory(i, tp, n));
            cnt++;
        }
        return cnt;
    }

    private int LoadOperations(List<List<string>> table, ImportTarget it)
    {
        if (table.Count == 0) return 0;
        var head = Index(table[0]);

        int id   = Need(head, "id"),
            type = Need(head, "type"),
            acc  = Need(head, "bank_account_id"),
            cat  = Need(head, "category_id"),
            amt  = Need(head, "amount"),
            date = Need(head, "date");
        int descc = head.TryGetValue("description", out var d) ? d : -1;

        int cnt = 0;
        for (int r = 1; r < table.Count; r++)
        {
            var row = table[r]; if (Blank(row)) continue;
            long i   = long.Parse(Get(row, id),   CultureInfo.InvariantCulture);
            var tp   = Types.ParseOperationType(Get(row, type));
            long a   = long.Parse(Get(row, acc),  CultureInfo.InvariantCulture);
            long c   = long.Parse(Get(row, cat),  CultureInfo.InvariantCulture);
            double m = double.Parse(Get(row, amt),CultureInfo.InvariantCulture);
            string dte   = Get(row, date);
            string? desc = descc >= 0 ? Get(row, descc) : null;

            it.Operations.Add(Factory.MakeOperation(i, tp, a, c, m, dte, desc));
            cnt++;
        }
        return cnt;
    }

    // ——— служебные ———

    /// <summary>Хеш-таблица «имя столбца (lower) → индекс» по первой строке.</summary>
    private static Dictionary<string, int> Index(List<string> header)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < header.Count; i++)
        {
            var key = header[i].Trim().ToLowerInvariant();
            if (!map.ContainsKey(key)) map[key] = i;
        }
        return map;
    }

    /// <summary>Гарантирует наличие столбца; бросает понятную ошибку, если его нет.</summary>
    private static int Need(Dictionary<string, int> idx, string col)
        => idx.TryGetValue(col, out var i) ? i : throw new Exception($"CSV: отсутствует столбец '{col}'");

    private static bool Blank(List<string> row) => row.All(f => string.IsNullOrWhiteSpace(f));
    private static string Get(List<string> row, int idx) => idx < row.Count ? row[idx].Trim() : string.Empty;
}
