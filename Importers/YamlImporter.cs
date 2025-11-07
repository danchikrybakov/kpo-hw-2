using System.Globalization;
using System.Linq;
using FinanceTracker.Domain;
using FinanceTracker.Repository;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FinanceTracker.Importers;

/// Импорт YAML с акцентом на устойчивость:
/// • Поддержка snake_case / PascalCase имён полей (UnderscoredNamingConvention).
/// • Поле type допускает "income"/"expense" или 0/1.
/// • Понятные сообщения об ошибках.
/// • Фоллбэк-парсинг через модель узлов (YamlStream) на случай «нетипичного» YAML.
public sealed class YamlImporter : BaseImporter
{
    // DTO для «обычного» пути: UnderscoredNamingConvention преобразует
    //   accounts → Accounts, bank_account_id → BankAccountId и т.п.
    private sealed class RootDto
    {
        public System.Collections.Generic.List<AccDto>? Accounts   { get; set; }
        public System.Collections.Generic.List<CatDto>? Categories { get; set; }
        public System.Collections.Generic.List<OpDto>?  Operations { get; set; }
    }
    private sealed class AccDto { public long Id { get; set; } public string Name { get; set; } = ""; public double Balance { get; set; } }
    private sealed class CatDto { public long Id { get; set; } public object? Type { get; set; } public string Name { get; set; } = ""; }
    private sealed class OpDto
    {
        public long Id { get; set; }
        public object? Type { get; set; }
        public long BankAccountId { get; set; } // snake_case → PascalCase конвенцией
        public long CategoryId    { get; set; }
        public double Amount      { get; set; }
        public string Date        { get; set; } = "";
        public string? Description { get; set; }
    }

    protected override ImportResult DoImport(string path, ImportTarget t)
    {
        var a0 = t.Accounts.All().Count;
        var c0 = t.Categories.All().Count;
        var o0 = t.Operations.All().Count;

        string yaml;
        try { yaml = File.ReadAllText(path); }
        catch (System.Exception e) { throw new System.Exception($"Не удалось открыть YAML: {path}. {e.Message}"); }

        // Попытка №1: «нормальный» разбор через DTO и конвенции
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance) // bank_account_id -> BankAccountId
                .IgnoreUnmatchedProperties()
                .Build();

            var r = deserializer.Deserialize<RootDto?>(yaml) ?? new RootDto();

            if (IsAllNull(r))
                throw new System.Exception("Пустой или несовместимый корень (нет accounts/categories/operations)");

            if (r.Accounts is not null)
                foreach (var x in r.Accounts)
                    t.Accounts.Add(new BankAccount(x.Id, x.Name, x.Balance));

            if (r.Categories is not null)
                foreach (var x in r.Categories)
                    t.Categories.Add(new Category(x.Id, ParseOpType(x.Type), x.Name));

            if (r.Operations is not null)
                foreach (var x in r.Operations)
                    t.Operations.Add(new Operation(x.Id, ParseOpType(x.Type), x.BankAccountId, x.CategoryId, x.Amount, x.Date, x.Description));

            return Delta(t, a0, c0, o0);
        }
        catch
        {
            // Попытка №2: фоллбэк — парсинг структуры через RepresentationModel (YamlStream)
            try
            {
                var ys = new YamlStream();
                using var sr = new StringReader(yaml);
                ys.Load(sr);
                if (ys.Documents.Count == 0) throw new System.Exception("Пустой YAML");

                var root = ys.Documents[0].RootNode as YamlMappingNode
                           ?? throw new System.Exception("Ожидался mapping на верхнем уровне");

                // Ищем секции без учёта регистра/подчёркиваний
                var accSeq = TryGetSeq(root, "accounts");
                var catSeq = TryGetSeq(root, "categories");
                var opSeq  = TryGetSeq(root, "operations");

                if (accSeq is null && catSeq is null && opSeq is null)
                    throw new System.Exception("Нет секций accounts/categories/operations");

                if (accSeq is not null)
                {
                    foreach (var n in accSeq)
                    {
                        var m = n as YamlMappingNode ?? throw new System.Exception("accounts: запись не объект");
                        long   id   = ReadInt(m, "id");
                        string name = ReadString(m, "name");
                        double bal  = ReadDouble(m, "balance");
                        t.Accounts.Add(new BankAccount(id, name, bal));
                    }
                }

                if (catSeq is not null)
                {
                    foreach (var n in catSeq)
                    {
                        var m = n as YamlMappingNode ?? throw new System.Exception("categories: запись не объект");
                        long   id   = ReadInt(m, "id");
                        var    ot   = ParseOpType(ReadScalar(m, "type"));
                        string name = ReadString(m, "name");
                        t.Categories.Add(new Category(id, ot, name));
                    }
                }

                if (opSeq is not null)
                {
                    foreach (var n in opSeq)
                    {
                        var m = n as YamlMappingNode ?? throw new System.Exception("operations: запись не объект");
                        long   id    = ReadInt(m, "id");
                        var    ot    = ParseOpType(ReadScalar(m, "type"));
                        long   accId = ReadInt(m, "bank_account_id", "bankaccountid", "bank_accountid", "bankaccount_id");
                        long   catId = ReadInt(m, "category_id", "categoryid");
                        double amt   = ReadDouble(m, "amount");
                        string date  = ReadString(m, "date");
                        string? desc = TryReadString(m, "description");
                        t.Operations.Add(new Operation(id, ot, accId, catId, amt, date, desc));
                    }
                }

                return Delta(t, a0, c0, o0);
            }
            catch (System.Exception ex2)
            {
                // Выводим одно «чистое» сообщение для пользователя
                throw new System.Exception($"Ошибка чтения YAML: {ex2.Message}");
            }
        }
    }

    // ——— Вспомогательные методы ———

    private static bool IsAllNull(RootDto r)
        => r.Accounts is null && r.Categories is null && r.Operations is null;

    private static ImportResult Delta(ImportTarget t, int a0, int c0, int o0)
        => new ImportResult(
            t.Accounts.All().Count   - a0,
            t.Categories.All().Count - c0,
            t.Operations.All().Count - o0
        );

    /// Допускаем "income"/"expense" или 0/1; для всего прочего — парсим строку.
    private static OperationType ParseOpType(object? raw) => raw switch
    {
        null        => throw new System.Exception("Не указан 'type'"),
        string s    => Types.ParseOperationType(s),
        int i       => i switch { 0 => OperationType.Income, 1 => OperationType.Expense, _ => throw new System.Exception($"type={i} недопустим") },
        long l      => l switch { 0 => OperationType.Income, 1 => OperationType.Expense, _ => throw new System.Exception($"type={l} недопустим") },
        _           => Types.ParseOperationType(raw.ToString() ?? "")
    };

    // Нормализация ключей: убираем '_' и '-' + приводим к нижнему регистру.
    private static string KeyNorm(string s)
        => new string(s.Where(ch => ch != '_' && ch != '-').ToArray()).ToLowerInvariant();

    private static YamlSequenceNode? TryGetSeq(YamlMappingNode map, params string[] names)
    {
        var want = names.Select(KeyNorm).ToHashSet();
        foreach (var kv in map.Children)
        {
            if (kv.Key is YamlScalarNode ks && want.Contains(KeyNorm(ks.Value ?? "")))
                return kv.Value as YamlSequenceNode;
        }
        return null;
    }

    private static YamlNode? TryGet(YamlMappingNode map, params string[] names)
    {
        var want = names.Select(KeyNorm).ToHashSet();
        foreach (var kv in map.Children)
            if (kv.Key is YamlScalarNode ks && want.Contains(KeyNorm(ks.Value ?? "")))
                return kv.Value;
        return null;
    }

    private static string ReadString(YamlMappingNode map, params string[] names)
    {
        var n = TryGet(map, names) as YamlScalarNode
                ?? throw new System.Exception($"Отсутствует строковое поле: {string.Join("/", names)}");
        return n.Value ?? "";
    }

    private static string? TryReadString(YamlMappingNode map, params string[] names)
        => TryGet(map, names) is YamlScalarNode s ? (s.Value ?? "") : null;

    private static object? ReadScalar(YamlMappingNode map, params string[] names)
        => TryGet(map, names) is YamlScalarNode s ? (object?)s.Value : null;

    private static long ReadInt(YamlMappingNode map, params string[] names)
    {
        var n = TryGet(map, names) as YamlScalarNode
                ?? throw new System.Exception($"Отсутствует числовое поле: {string.Join("/", names)}");
        if (n.Value is null) throw new System.Exception($"Пустое числовое поле: {string.Join("/", names)}");
        if (long.TryParse(n.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) return v;
        throw new System.Exception($"Поле {string.Join("/", names)} должно быть целым числом");
    }

    private static double ReadDouble(YamlMappingNode map, params string[] names)
    {
        var n = TryGet(map, names) as YamlScalarNode
                ?? throw new System.Exception($"Отсутствует числовое поле: {string.Join("/", names)}");
        if (n.Value is null) throw new System.Exception($"Пустое числовое поле: {string.Join("/", names)}");
        if (double.TryParse(n.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) return v;
        throw new System.Exception($"Поле {string.Join("/", names)} должно быть числом");
    }
}
