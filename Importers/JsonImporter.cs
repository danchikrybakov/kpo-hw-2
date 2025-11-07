using System.Text.Json;
using FinanceTracker.Domain;

namespace FinanceTracker.Importers;

/// <summary>
/// Импорт JSON в формате, совместимом с нашим экспортом:
/// {
///   "accounts":   [{ "id":1, "name":"...", "balance":100.0 }, ...],
///   "categories": [{ "id":1, "type":"income|expense", "name":"..." }, ...],
///   "operations": [{ "id":..., "type":"...", "bank_account_id":..., "category_id":..., "amount":..., "date":"YYYY-MM-DD", "description":"..." }, ...]
/// }
/// Поля читаются регистронезависимо; отсутствующие секции трактуются как пустые.
/// </summary>
public sealed class JsonImporter : BaseImporter
{
    // DTO ровно под ожидаемые имена полей (snake_case для совместимости с входом/выходом)
    private sealed record Root(
        List<BankAccountDto>? accounts,
        List<CategoryDto>?    categories,
        List<OperationDto>?   operations
    );

    private sealed record BankAccountDto(long id, string name, double balance);
    private sealed record CategoryDto(long id, string type, string name);
    private sealed record OperationDto(
        long   id,
        string type,
        long   bank_account_id,
        long   category_id,
        double amount,
        string date,
        string? description
    );

    protected override ImportResult DoImport(string path, ImportTarget t)
    {
        if (!File.Exists(path))
            throw new Exception($"JSON: файл не найден: {path}");

        string json;
        try
        {
            // UTF-8 по умолчанию; System.Text.Json ожидает именно такой ввод
            json = File.ReadAllText(path);
        }
        catch (Exception e)
        {
            throw new Exception("JSON: не удалось прочитать файл", e);
        }

        var opt = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        Root root;
        try
        {
            root = JsonSerializer.Deserialize<Root>(json, opt)
                   ?? throw new Exception("JSON: пустой или некорректный документ");
        }
        catch (JsonException je)
        {
            // Даём понятное сообщение при синтаксической ошибке
            throw new Exception("JSON: ошибка разбора — " + je.Message, je);
        }

        // Отсутствующие секции считаем пустыми
        var accs = root.accounts   ?? new List<BankAccountDto>();
        var cats = root.categories ?? new List<CategoryDto>();
        var ops  = root.operations ?? new List<OperationDto>();

        int a = 0, c = 0, o = 0;

        // Переносим данные в доменную модель через фабрику,
        // чтобы централизовать нормализацию (тримы и т.п.)
        foreach (var x in accs)
        {
            t.Accounts.Add(Factory.MakeAccount(x.id, x.name, x.balance));
            a++;
        }

        foreach (var x in cats)
        {
            t.Categories.Add(Factory.MakeCategory(x.id, Types.ParseOperationType(x.type), x.name));
            c++;
        }

        foreach (var x in ops)
        {
            t.Operations.Add(Factory.MakeOperation(
                x.id,
                Types.ParseOperationType(x.type),
                x.bank_account_id,
                x.category_id,
                x.amount,
                x.date,
                x.description));
            o++;
        }

        return new ImportResult(a, c, o);
    }
}
