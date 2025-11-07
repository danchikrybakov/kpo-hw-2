using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using FinanceTracker.Repository;
using System.Linq;

namespace FinanceTracker.Exporters;

/// <summary>
/// Экспорт в JSON в формате, совместимом с импортом:
/// { "accounts":[...], "categories":[...], "operations":[...] } со snake_case полями.
/// Порядок элементов берётся из репозиториев (insertion order): это сохраняет
/// порядок «как на входе», а новые записи идут в конец.
/// </summary>
public sealed class JsonExporter : BaseExporter
{
    /// <summary>
    /// Сериализует текущее состояние в JSON и записывает по пути <paramref name="path"/>.
    /// Для кириллицы отключаем экранирование (\uXXXX), чтобы файл читался человеком.
    /// </summary>
    public override void Export(
        string path,
        IBankAccountRepository acc,
        ICategoryRepository    cat,
        IOperationRepository   op)
    {
        // Проецируем доменные сущности в анонимные объекты со snake_case ключами.
        var payload = new
        {
            accounts = acc.All().Select(a => new
            {
                id      = a.Id,
                name    = a.Name,
                balance = a.Balance
            }),
            categories = cat.All().Select(c => new
            {
                id   = c.Id,
                type = c.Type.ToString().ToLowerInvariant(), // "income" / "expense"
                name = c.Name
            }),
            operations = op.All().Select(o => new
            {
                id              = o.Id,
                type            = o.Type.ToString().ToLowerInvariant(),
                bank_account_id = o.BankAccountId,
                category_id     = o.CategoryId,
                amount          = o.Amount,
                date            = o.Date,          // формат YYYY-MM-DD уже обеспечен на входе
                description     = o.Description    // null -> отсутствует поле? нет, будет "description": null (это ок)
            })
        };

        // Индентированный JSON + явный Encoder для латиницы/кириллицы без \uXXXX.
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
        };

        var json = JsonSerializer.Serialize(payload, options);

        // Создаём родительский каталог при необходимости и пишем UTF-8 без BOM.
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, json);
    }
}
