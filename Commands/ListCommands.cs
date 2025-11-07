using FinanceTracker.App;
using FinanceTracker.Repository;
using FinanceTracker.Domain;

namespace FinanceTracker.Commands;

/// <summary>
/// Команда вывода списка счетов.
/// </summary>
public sealed class ListAccountsCommand : ICommand
{
    private readonly IBankAccountRepository _acc; 
    private readonly TablePrinter _p;

    /// <param name="acc">Репозиторий счетов.</param>
    /// <param name="p">Печать таблиц в консоль.</param>
    public ListAccountsCommand(IBankAccountRepository acc, TablePrinter p)
    { 
        _acc = acc; _p = p; 
    }

    /// <summary>Печатает таблицу со счетами.</summary>
    public int Execute()
    { 
        _p.Accounts(_acc.All()); 
        return 0; 
    }
}

/// <summary>
/// Команда вывода списка категорий.
/// </summary>
public sealed class ListCategoriesCommand : ICommand
{
    private readonly ICategoryRepository _cat; 
    private readonly TablePrinter _p;

    /// <param name="cat">Репозиторий категорий.</param>
    /// <param name="p">Печать таблиц в консоль.</param>
    public ListCategoriesCommand(ICategoryRepository cat, TablePrinter p)
    { 
        _cat = cat; _p = p; 
    }

    /// <summary>Печатает таблицу с категориями.</summary>
    public int Execute()
    { 
        _p.Categories(_cat.All()); 
        return 0; 
    }
}

/// <summary>
/// Команда вывода списка операций с фильтрами по счёту, категории, диапазону дат, подстроке в описании и ограничению количества.
/// </summary>
public sealed class ListOperationsCommand : ICommand
{
    private readonly IOperationRepository _op; 
    private readonly TablePrinter _p;

    // Фильтры: все параметры необязательные.
    private readonly long? _accId, _catId; 
    private readonly string? _from, _to, _contains; 
    private readonly int? _max;

    /// <param name="op">Репозиторий операций.</param>
    /// <param name="p">Печать таблиц в консоль.</param>
    /// <param name="accId">Фильтр по счёту.</param>
    /// <param name="catId">Фильтр по категории.</param>
    /// <param name="from">Нижняя граница даты включительно (формат YYYY-MM-DD).</param>
    /// <param name="to">Верхняя граница даты включительно (формат YYYY-MM-DD).</param>
    /// <param name="contains">Подстрока в описании (регистронезависимо).</param>
    /// <param name="max">Максимальное число записей.</param>
    public ListOperationsCommand(IOperationRepository op, TablePrinter p,
        long? accId, long? catId, string? from, string? to, string? contains, int? max)
    { 
        _op = op; _p = p; 
        _accId = accId; _catId = catId; _from = from; _to = to; _contains = contains; _max = max; 
    }

    /// <summary>
    /// Применяет фильтры к операциям и печатает результат. 
    /// Сравнение дат выполняется лексикографически, т.к. формат YYYY-MM-DD упорядочивается строкой.
    /// Порядок фильтров не влияет на итог.
    /// </summary>
    public int Execute()
    {
        IEnumerable<Operation> q = _op.All();
        if (_accId    is { } a) q = q.Where(x => x.BankAccountId == a);
        if (_catId    is { } c) q = q.Where(x => x.CategoryId == c);
        if (_from     is { } f) q = q.Where(x => string.Compare(x.Date, f, StringComparison.Ordinal) >= 0);
        if (_to       is { } t) q = q.Where(x => string.Compare(x.Date, t, StringComparison.Ordinal) <= 0);
        if (_contains is { } s) q = q.Where(x => (x.Description ?? "").Contains(s, StringComparison.OrdinalIgnoreCase));
        if (_max      is { } m) q = q.Take(m);

        _p.Operations(q);
        return 0;
    }
}
