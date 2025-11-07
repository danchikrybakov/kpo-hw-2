using Spectre.Console;
using FinanceTracker.Repository;
using FinanceTracker.Domain;

namespace FinanceTracker.Commands;

/// <summary>
/// Команда создания счёта (CRUD: Create).
/// Использует репозиторий счетов и печатает короткое подтверждение.
/// </summary>
public sealed class CreateAccountCommand : ICommand
{
    private readonly IBankAccountRepository _acc; 
    private readonly long _id; 
    private readonly string _name; 
    private readonly double _bal;

    /// <param name="acc">Репозиторий счетов.</param>
    /// <param name="id">Идентификатор нового счёта (ожидается уникальность).</param>
    /// <param name="name">Название счёта.</param>
    /// <param name="balance">Начальный баланс.</param>
    public CreateAccountCommand(IBankAccountRepository acc, long id, string name, double balance)
    { 
        _acc = acc; _id = id; _name = name; _bal = balance; 
    }

    /// <summary>Добавляет счёт в хранилище и печатает подтверждение.</summary>
    public int Execute()
    { 
        _acc.Add(new BankAccount(_id, _name, _bal)); 
        AnsiConsole.MarkupLine($"[green]Создан счёт:[/] id={_id}, name=\"{_name}\", balance={_bal:F2}"); 
        return 0; 
    }
}

/// <summary>
/// Команда редактирования счёта (CRUD: Update).
/// Поддерживает частичное обновление: если параметр не задан, сохраняется текущее значение.
/// </summary>
public sealed class EditAccountCommand : ICommand
{
    private readonly IBankAccountRepository _acc; 
    private readonly long _id; 
    private readonly string? _name; 
    private readonly double? _bal;

    /// <param name="acc">Репозиторий счетов.</param>
    /// <param name="id">Идентификатор редактируемого счёта.</param>
    /// <param name="name">Новое имя (необязательное).</param>
    /// <param name="balance">Новый баланс (необязательный).</param>
    public EditAccountCommand(IBankAccountRepository acc, long id, string? name, double? balance)
    { 
        _acc = acc; _id = id; _name = name; _bal = balance; 
    }

    /// <summary>
    /// Обновляет только заданные поля; печатает сводку изменений.
    /// Бросает исключение, если счёт не найден.
    /// </summary>
    public int Execute()
    {
        var cur = _acc.Get(_id) ?? throw new Exception("account not found");

        // Частичное обновление: неуказанные поля остаются прежними.
        var up = cur with { Name = _name ?? cur.Name, Balance = _bal ?? cur.Balance };
        _acc.Update(up);

        // Формируем человеко-читаемую сводку: показываем только реально изменённые поля.
        var info = new List<string>();
        if (up.Name != cur.Name) info.Add($"name: \"{cur.Name}\" → \"{up.Name}\"");
        if (Math.Abs(up.Balance - cur.Balance) > 1e-9) info.Add($"balance: {cur.Balance:F2} → {up.Balance:F2}");

        AnsiConsole.MarkupLine($"[yellow]Изменён счёт {_id}:[/] {(info.Count > 0 ? string.Join(", ", info) : "без изменений")}");
        return 0;
    }
}

/// <summary>
/// Команда удаления счёта (CRUD: Delete).
/// </summary>
public sealed class DeleteAccountCommand : ICommand
{
    private readonly IBankAccountRepository _acc; 
    private readonly long _id;

    /// <param name="acc">Репозиторий счетов.</param>
    /// <param name="id">Идентификатор удаляемого счёта.</param>
    public DeleteAccountCommand(IBankAccountRepository acc, long id)
    { 
        _acc = acc; _id = id; 
    }

    /// <summary>Удаляет счёт и печатает подтверждение.</summary>
    public int Execute()
    { 
        _acc.Remove(_id); 
        AnsiConsole.MarkupLine($"[red]Удалён счёт:[/] id={_id}"); 
        return 0; 
    }
}
