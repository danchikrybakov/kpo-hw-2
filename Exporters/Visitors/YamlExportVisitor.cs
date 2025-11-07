using FinanceTracker.Domain;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FinanceTracker.Export.Visitors;

/// <summary>
/// Визитёр формирования YAML в целевом формате задания:
/// секции <c>accounts</c>, <c>categories</c>, <c>operations</c> со snake_case полями,
/// а поле <c>type</c> — строкой "income"/"expense".
/// </summary>
public sealed class YamlExportVisitor : IFinanceVisitor
{
    // Внутренние DTO оставляем в PascalCase — конвенция UnderscoredNaming
    // автоматически преобразует имена свойств в snake_case.
    private sealed class AccDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public double Balance { get; set; }
    }

    private sealed class CatDto
    {
        public long Id { get; set; }
        public string Type { get; set; } = ""; // "income"/"expense"
        public string Name { get; set; } = "";
    }

    private sealed class OpDto
    {
        public long Id { get; set; }
        public string Type { get; set; } = ""; // "income"/"expense"
        public long BankAccountId { get; set; }
        public long CategoryId { get; set; }
        public double Amount { get; set; }
        public string Date { get; set; } = ""; // YYYY-MM-DD
        public string? Description { get; set; }
    }

    private readonly List<AccDto> _acc = new();
    private readonly List<CatDto> _cat = new();
    private readonly List<OpDto>  _op  = new();

    /// <summary>Итоговая YAML-строка (заполняется в <see cref="End"/>).</summary>
    public string Result { get; private set; } = string.Empty;

    public void Begin() { }

    public void Visit(BankAccount a)
        => _acc.Add(new AccDto { Id = a.Id, Name = a.Name, Balance = a.Balance });

    public void Visit(Category c)
        => _cat.Add(new CatDto { Id = c.Id, Type = c.Type.ToString().ToLowerInvariant(), Name = c.Name });

    public void Visit(Operation o)
        => _op.Add(new OpDto
        {
            Id = o.Id,
            Type = o.Type.ToString().ToLowerInvariant(),
            BankAccountId = o.BankAccountId,
            CategoryId = o.CategoryId,
            Amount = o.Amount,
            Date = o.Date,
            Description = o.Description
        });

    /// <summary>
    /// Сериализация накопленных данных в YAML. 
    /// Порядок элементов сохраняется такой же, в каком они переданы экспортёром.
    /// </summary>
    public void End()
    {
        var root = new
        {
            Accounts   = _acc,
            Categories = _cat,
            Operations = _op
        };

        // UnderscoredNamingConvention: Accounts -> accounts, BankAccountId -> bank_account_id
        // OmitNull: не сериализуем пустые описания.
        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        Result = serializer.Serialize(root);
    }
}
