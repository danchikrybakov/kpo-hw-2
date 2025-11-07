using FinanceTracker.App;
using FinanceTracker.Facade;

namespace FinanceTracker.Commands;

/// <summary>
/// Отчёт по категориям: агрегирует доход/расход для каждой категории и печатает таблицу.
/// </summary>
public sealed class ReportCategoriesCommand : ICommand
{
    private readonly AnalyticsFacade _a; 
    private readonly TablePrinter _p;

    /// <param name="a">Фасад аналитики (расчёты).</param>
    /// <param name="p">Печать таблиц в консоль.</param>
    public ReportCategoriesCommand(AnalyticsFacade a, TablePrinter p)
    { 
        _a = a; _p = p; 
    }

    /// <summary>Строит свод по категориям и печатает результат.</summary>
    public int Execute()
    { 
        _p.ByCategory(_a.ByCategory()); 
        return 0; 
    }
}

/// <summary>
/// Помесячный отчёт: для заданных фильтров (год/счёт/категория) выводит доход, расход и сальдо по месяцам.
/// </summary>
public sealed class ReportMonthlyCommand : ICommand
{
    private readonly AnalyticsFacade _a; 
    private readonly TablePrinter _p;
    private readonly int? _year; 
    private readonly long? _acc, _cat;

    /// <param name="a">Фасад аналитики.</param>
    /// <param name="p">Печать таблиц.</param>
    /// <param name="year">Год (необязательно).</param>
    /// <param name="acc">Id счёта (необязательно).</param>
    /// <param name="cat">Id категории (необязательно).</param>
    public ReportMonthlyCommand(AnalyticsFacade a, TablePrinter p, int? year, long? acc, long? cat)
    { 
        _a = a; _p = p; _year = year; _acc = acc; _cat = cat; 
    }

    /// <summary>Строит помесячную сводку и печатает результат.</summary>
    public int Execute()
    { 
        _p.ByMonth(_a.ByMonth(_year, _acc, _cat)); 
        return 0; 
    }
}

/// <summary>
/// Топ расходов по категориям за период: по умолчанию N=10; поддерживаются from/to (YYYY-MM-DD).
/// </summary>
public sealed class ReportTopCommand : ICommand
{
    private readonly AnalyticsFacade _a; 
    private readonly TablePrinter _p;
    private readonly int _n; 
    private readonly string? _from, _to;

    /// <param name="a">Фасад аналитики.</param>
    /// <param name="p">Печать таблиц.</param>
    /// <param name="n">Размер топа.</param>
    /// <param name="from">Начальная дата включительно (необязательно).</param>
    /// <param name="to">Конечная дата включительно (необязательно).</param>
    public ReportTopCommand(AnalyticsFacade a, TablePrinter p, int n, string? from, string? to)
    { 
        _a = a; _p = p; _n = n; _from = from; _to = to; 
    }

    /// <summary>Строит топ расходов и печатает результат.</summary>
    public int Execute()
    { 
        _p.TopExpenses(_a.TopExpenses(_n, _from, _to)); 
        return 0; 
    }
}
