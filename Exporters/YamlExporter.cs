using System.Text.RegularExpressions;
using FinanceTracker.Repository;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;
using YamlDotNet.Serialization.NamingConventions;

namespace FinanceTracker.Exporters;

/// <summary>
/// Экспорт в YAML в формате, совместимом с импортом:
/// секции <c>accounts</c>, <c>categories</c>, <c>operations</c> со snake_case полями,
/// между секциями — пустая строка, строки по возможности без кавычек.
/// </summary>
public sealed class YamlExporter : BaseExporter
{
    // Узкие DTO под сериализацию (PascalCase → snake_case конвенцией)
    private sealed class AccOut { public long Id { get; set; } public string Name { get; set; } = ""; public double Balance { get; set; } }
    private sealed class CatOut { public long Id { get; set; } public string Type { get; set; } = ""; public string Name { get; set; } = ""; }
    private sealed class OpOut
    {
        public long Id { get; set; }
        public string Type { get; set; } = "";
        public long BankAccountId { get; set; }
        public long CategoryId { get; set; }
        public double Amount { get; set; }
        public string Date { get; set; } = ""; // YYYY-MM-DD
        public string? Description { get; set; }
    }
    private sealed class RootOut
    {
        public List<AccOut> Accounts { get; } = new();
        public List<CatOut> Categories { get; } = new();
        public List<OpOut> Operations { get; } = new();
    }

    /// <summary>
    /// Эмиттер, который старается выводить строки как plain (без кавычек),
    /// но делает это только когда это безопасно для YAML.
    /// </summary>
    private sealed class SafePlainScalarEmitter : ChainedEventEmitter
    {
        public SafePlainScalarEmitter(IEventEmitter next) : base(next) { }

        public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
        {
            if (eventInfo.Source.Value is string s && IsPlainSafe(s))
            {
                // Просим YAML-писатель использовать стиль Plain
                eventInfo = new ScalarEventInfo(eventInfo.Source) { Style = ScalarStyle.Plain };
            }
            base.Emit(eventInfo, emitter);
        }

        // Минимальная проверка «безопасности» plain-скаляра для YAML 1.2.
        // Избегаем кавычек, перевода строк, начальных/конечных пробелов, табов
        // и некоторых проблемных первых символов.
        private static bool IsPlainSafe(string s)
        {
            if (string.IsNullOrEmpty(s)) return true;
            if (char.IsWhiteSpace(s[0]) || char.IsWhiteSpace(s[^1])) return false;
            if (s.IndexOfAny(new[] { '\r', '\n', '\t', '"' }) >= 0) return false;

            // Не начинаем с спецсимволов YAML, чтобы не провоцировать неоднозначность
            const string badStart = "-?:,[]{}#&*!|>'%@`";
            if (badStart.IndexOf(s[0]) >= 0) return false;

            // «#» внутри строки допустим, но « :»/« : » рядом с двоеточием может быть проблемой.
            // Для простоты оставим библиотеке право поставить кавычки в таких случаях.
            if (s.Contains(": ") || s.Contains(" :")) return false;

            return true;
        }
    }

    public override void Export(string path, IBankAccountRepository acc, ICategoryRepository cat, IOperationRepository op)
    {
        var root = new RootOut();

        // Сохраняем порядок из репозиториев (insertion order)
        foreach (var a in acc.All())
            root.Accounts.Add(new AccOut { Id = a.Id, Name = a.Name, Balance = a.Balance });

        foreach (var c in cat.All())
            root.Categories.Add(new CatOut { Id = c.Id, Type = c.Type.ToString().ToLowerInvariant(), Name = c.Name });

        foreach (var o in op.All())
            root.Operations.Add(new OpOut
            {
                Id = o.Id,
                Type = o.Type.ToString().ToLowerInvariant(),
                BankAccountId = o.BankAccountId,
                CategoryId = o.CategoryId,
                Amount = o.Amount,
                Date = o.Date,
                Description = o.Description
            });

        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)   // Accounts → accounts, BankAccountId → bank_account_id
            .WithIndentedSequences()
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull) // не выводим пустые description
            .WithEventEmitter(next => new SafePlainScalarEmitter(next))     // строки без кавычек, когда можно
            .Build();

        var yaml = serializer.Serialize(root);

        // Визуальный интервал между секциями, как в эталонных файлах.
        yaml = Regex.Replace(yaml, "\ncategories:\n", "\n\ncategories:\n");
        yaml = Regex.Replace(yaml, "\noperations:\n", "\n\noperations:\n");

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, yaml);
    }
}
