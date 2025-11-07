namespace FinanceTracker.Domain;

/// <summary>
/// Финансовая операция.
/// </summary>
/// <remarks>
/// <para>
/// <b>Type</b> задаёт смысл суммы (<see cref="OperationType.Income"/> или <see cref="OperationType.Expense"/>).
/// <b>Amount</b> хранится как абсолютная величина (без знака); знак выводится из <c>Type</c>.
/// </para>
/// <para>
/// <b>Date</b> — строка в формате <c>YYYY-MM-DD</c> (лексикографически сравнимая и сортируемая).
/// </para>
/// </remarks>
public record Operation(
    long Id,
    OperationType Type,
    long BankAccountId,
    long CategoryId,
    double Amount,
    string Date,         // формат: YYYY-MM-DD
    string? Description  // необязательное поле; null/пустая строка — отсутствие описания
);