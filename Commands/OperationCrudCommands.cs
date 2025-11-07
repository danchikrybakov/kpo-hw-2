using Spectre.Console;
using FinanceTracker.Repository;
using FinanceTracker.Domain;

namespace FinanceTracker.Commands;

/// <summary>
/// Создание операции (CRUD: Create).
/// </summary>
public sealed class CreateOperationCommand : ICommand
{
    private readonly IOperationRepository _op; 
    private readonly long _id; 
    private readonly OperationType _type;
    private readonly long _acc, _cat; 
    private readonly double _amt; 
    private readonly string _date; 
    private readonly string? _desc;

    /// <param name="op">Репозиторий операций.</param>
    /// <param name="id">Идентификатор операции.</param>
    /// <param name="type">Тип (income/expense).</param>
    /// <param name="acc">Идентификатор счёта.</param>
    /// <param name="cat">Идентификатор категории.</param>
    /// <param name="amt">Сумма операции.</param>
    /// <param name="date">Дата в формате YYYY-MM-DD.</param>
    /// <param name="desc">Описание (необязательно).</param>
    public CreateOperationCommand(IOperationRepository op, long id, OperationType type, long acc, long cat, double amt, string date, string? desc)
    { 
        _op = op; _id = id; _type = type; _acc = acc; _cat = cat; _amt = amt; _date = date; _desc = desc; 
    }

    /// <summary>Добавляет операцию и печатает краткое подтверждение.</summary>
    public int Execute()
    { 
        _op.Add(new Operation(_id, _type, _acc, _cat, _amt, _date, _desc)); 
        AnsiConsole.MarkupLine($"[green]Создана операция:[/] id={_id}, type={_type}, acc={_acc}, cat={_cat}, amount={_amt:F2}, date={_date}");
        return 0; 
    }
}

/// <summary>
/// Редактирование операции (CRUD: Update).
/// Частичное обновление: меняем только заданные поля, остальные — без изменений.
/// </summary>
public sealed class EditOperationCommand : ICommand
{
    private readonly IOperationRepository _op; 
    private readonly long _id;

    // Все поля — опциональные, кроме идентификатора операции.
    private readonly string? _type, _date, _desc; 
    private readonly long? _acc, _cat; 
    private readonly double? _amt;

    /// <param name="op">Репозиторий операций.</param>
    /// <param name="id">Идентификатор операции.</param>
    /// <param name="type">Новый тип (строка "income"/"expense", необязательно).</param>
    /// <param name="acc">Новый счёт (необязательно).</param>
    /// <param name="cat">Новая категория (необязательно).</param>
    /// <param name="amt">Новая сумма (необязательно).</param>
    /// <param name="date">Новая дата (YYYY-MM-DD, необязательно).</param>
    /// <param name="desc">Новое описание (необязательно).</param>
    public EditOperationCommand(IOperationRepository op, long id, string? type, long? acc, long? cat, double? amt, string? date, string? desc)
    { 
        _op = op; _id = id; _type = type; _acc = acc; _cat = cat; _amt = amt; _date = date; _desc = desc; 
    }

    /// <summary>
    /// Обновляет указанные поля; печатает сводку реально изменённых значений.
    /// Бросает исключение, если операция не найдена.
    /// </summary>
    public int Execute()
    {
        var cur = _op.Get(_id) ?? throw new Exception("operation not found");

        var up = cur with
        {
            Type          = _type is null ? cur.Type : Types.ParseOperationType(_type),
            BankAccountId = _acc ?? cur.BankAccountId,
            CategoryId    = _cat ?? cur.CategoryId,
            Amount        = _amt ?? cur.Amount,
            Date          = _date ?? cur.Date,
            Description   = _desc ?? cur.Description
        };

        _op.Update(up);

        // Человекочитаемая сводка только по тем полям, что действительно изменились.
        var info = new List<string>();
        if (up.Type != cur.Type) info.Add($"type: {cur.Type} → {up.Type}");
        if (up.BankAccountId != cur.BankAccountId) info.Add($"acc: {cur.BankAccountId} → {up.BankAccountId}");
        if (up.CategoryId != cur.CategoryId) info.Add($"cat: {cur.CategoryId} → {up.CategoryId}");
        if (Math.Abs(up.Amount - cur.Amount) > 1e-9) info.Add($"amount: {cur.Amount:F2} → {up.Amount:F2}");
        if (up.Date != cur.Date) info.Add($"date: {cur.Date} → {up.Date}");
        if ((up.Description ?? "") != (cur.Description ?? "")) info.Add("desc: изменено");

        AnsiConsole.MarkupLine($"[yellow]Изменена операция {_id}:[/] {(info.Count > 0 ? string.Join(", ", info) : "без изменений")}");
        return 0;
    }
}

/// <summary>
/// Удаление операции (CRUD: Delete).
/// </summary>
public sealed class DeleteOperationCommand : ICommand
{
    private readonly IOperationRepository _op; 
    private readonly long _id;

    /// <param name="op">Репозиторий операций.</param>
    /// <param name="id">Идентификатор операции.</param>
    public DeleteOperationCommand(IOperationRepository op, long id)
    { 
        _op = op; _id = id; 
    }

    /// <summary>Удаляет операцию и печатает подтверждение.</summary>
    public int Execute()
    { 
        _op.Remove(_id); 
        AnsiConsole.MarkupLine($"[red]Удалена операция:[/] id={_id}"); 
        return 0; 
    }
}
