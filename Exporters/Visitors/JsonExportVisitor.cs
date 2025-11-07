using System.Text.Json;
using FinanceTracker.Domain;

namespace FinanceTracker.Export.Visitors;

/// <summary>
/// Визитёр формирования JSON-представления в «единый» объект:
/// { "accounts": [...], "categories": [...], "operations": [...] }.
/// Используется экспортёром JSON для накопления данных и последующей сериализации.
/// </summary>
public sealed class JsonExportVisitor : IFinanceVisitor
{
    private readonly JsonSerializerOptions _options;

    // Буферы для последовательного накопления элементов трёх типов.
    private readonly List<BankAccount> _accounts   = new();
    private readonly List<Category>     _categories = new();
    private readonly List<Operation>    _operations = new();

    /// <param name="options">Опции сериализации (индентация, политика именования и т.п.).</param>
    public JsonExportVisitor(JsonSerializerOptions options) => _options = options;

    /// <summary>
    /// Итоговая строка JSON. Заполняется в <see cref="End"/>.
    /// </summary>
    public string Result { get; private set; } = string.Empty;

    /// <summary>Инициализация обхода (ничего не требуется).</summary>
    public void Begin() { }

    /// <summary>Добавляет счёт в буфер.</summary>
    public void Visit(BankAccount a) => _accounts.Add(a);

    /// <summary>Добавляет категорию в буфер.</summary>
    public void Visit(Category c)     => _categories.Add(c);

    /// <summary>Добавляет операцию в буфер.</summary>
    public void Visit(Operation o)    => _operations.Add(o);

    /// <summary>
    /// Завершает формирование и сериализует накопленные данные в JSON согласно заданным опциям.
    /// </summary>
    public void End()
    {
        var payload = new
        {
            accounts   = (IEnumerable<BankAccount>)_accounts,
            categories = (IEnumerable<Category>)_categories,
            operations = (IEnumerable<Operation>)_operations
        };

        // Важно: сериализация выполняется один раз в конце, чтобы сохранить порядок,
        // накопленный экспортёром (например, «как на входе + новые в конец»).
        Result = JsonSerializer.Serialize(payload, _options);
    }
}
