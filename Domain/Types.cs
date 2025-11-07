namespace FinanceTracker.Domain;

/// <summary>
/// Тип операции: доход или расход.
/// </summary>
public enum OperationType { Income, Expense }

/// <summary>
/// Утилиты для работы с типами доменной модели.
/// </summary>
public static class Types
{
    /// <summary>
    /// Парсинг строкового представления типа операции.
    /// Ожидаются лексемы <c>"income"</c> или <c>"expense"</c> (без учёта регистра и с обрезкой пробелов).
    /// </summary>
    /// <exception cref="Exception">
    /// Бросается, если передано неизвестное значение (для раннего выявления ошибок входных данных).
    /// </exception>
    public static OperationType ParseOperationType(string s)
    {
        s = s.Trim().ToLowerInvariant();
        return s switch
        {
            "income"  => OperationType.Income,
            "expense" => OperationType.Expense,
            _ => throw new Exception("unknown operation type: " + s)
        };
    }
}