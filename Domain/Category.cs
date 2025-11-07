namespace FinanceTracker.Domain;

/// <summary>
/// Категория операций (доход/расход).
/// <para>
/// <b>Id</b> — уникальный идентификатор (long).<br/>
/// <b>Type</b> — тип операции: <see cref="OperationType.Income"/> или <see cref="OperationType.Expense"/>.<br/>
/// <b>Name</b> — человекочитаемое название (например, «Зарплата», «Продукты»).
/// </para>
/// </summary>
/// <remarks>
/// <c>record</c> удобен для неизменяемых DTO и «with»-копирования в командах редактирования.
/// Валидация согласованности (совпадение типа категории и операции) выполняется на уровне импортёров/аналитики.
/// </remarks>
public record Category(long Id, OperationType Type, string Name);