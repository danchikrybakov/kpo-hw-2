namespace FinanceTracker.Domain;

/// <summary>
/// Фабрика доменных объектов. 
/// Единая точка создания сущностей (BankAccount, Category, Operation),
/// чтобы централизованно применять минимальную нормализацию данных
/// (обрезка пробелов, базовая унификация строковых полей).
/// </summary>
public sealed class DomainFactory
{
    /// <summary>
    /// Создаёт банковский счёт. Имя нормализуется (Trim).
    /// </summary>
    public BankAccount MakeAccount(long id, string name, double balance)
        => new(id, name.Trim(), balance);

    /// <summary>
    /// Создаёт категорию. Имя нормализуется (Trim).
    /// </summary>
    public Category MakeCategory(long id, OperationType type, string name)
        => new(id, type, name.Trim());

    /// <summary>
    /// Создаёт операцию. Дата и описание нормализуются (Trim). 
    /// Согласованность типа операции и категории проверяется выше по слою (импорт/аналитика).
    /// </summary>
    public Operation MakeOperation(long id, OperationType type, long acc, long cat, double amount, string date, string? desc)
        => new(id, type, acc, cat, amount, date.Trim(), desc?.Trim());
}