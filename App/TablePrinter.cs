using Spectre.Console;
using FinanceTracker.Domain;
using FinanceTracker.Facade;

namespace FinanceTracker.App;

/// <summary>
/// Печать табличных представлений доменных данных в консоли (Spectre.Console).
/// Таблицы — чисто презентационный слой; исходные коллекции не модифицируются.
/// </summary>
public sealed class TablePrinter
{
    /// <summary>
    /// Счета: выводим в порядке возрастания Id.
    /// Баланс форматируется с двумя знаками после запятой (зависит от текущей культуры ОС).
    /// </summary>
    public void Accounts(IEnumerable<BankAccount> v)
    {
        var t = new Table().Border(TableBorder.Rounded);
        t.AddColumns("id","name","balance");
        foreach (var a in v.OrderBy(x => x.Id))
            t.AddRow(a.Id.ToString(), a.Name, a.Balance.ToString("F2"));
        AnsiConsole.Write(t);
    }

    /// <summary>
    /// Категории: сортировка по Id; тип приводим к нижнему регистру для единообразия.
    /// </summary>
    public void Categories(IEnumerable<Category> v)
    {
        var t = new Table().Border(TableBorder.Rounded);
        t.AddColumns("id","type","name");
        foreach (var c in v.OrderBy(x => x.Id))
            t.AddRow(c.Id.ToString(), c.Type.ToString().ToLower(), c.Name);
        AnsiConsole.Write(t);
    }

    /// <summary>
    /// Операции: сортировка по Id; описание может быть пустым.
    /// Сумма форматируется с двумя знаками, дата — как есть (YYYY-MM-DD по входным данным).
    /// </summary>
    public void Operations(IEnumerable<Operation> v)
    {
        var t = new Table().Border(TableBorder.Rounded);
        t.AddColumns("id","type","acc","cat","amount","date","description");
        foreach (var o in v.OrderBy(x=>x.Id))
            t.AddRow(o.Id.ToString(), o.Type.ToString().ToLower(),
                     o.BankAccountId.ToString(), o.CategoryId.ToString(),
                     o.Amount.ToString("F2"), o.Date, o.Description ?? "");
        AnsiConsole.Write(t);
    }

    /// <summary>
    /// Сверка балансов: по каждому счёту показываем заявленный/рассчитанный баланс и дельту.
    /// Внизу — краткая сводка по доходам/расходам.
    /// </summary>
    public void Reconcile(ReconcileReport rep)
    {
        var t = new Table().Border(TableBorder.Rounded);
        t.AddColumns("acc","name","declared","computed","delta","income","expense");
        foreach (var r in rep.Rows.OrderBy(x=>x.AccountId))
            t.AddRow(r.AccountId.ToString(), r.Name,
                r.Declared.ToString("F2"), r.Computed.ToString("F2"),
                r.Delta.ToString("F2"), r.Income.ToString("F2"), r.Expense.ToString("F2"));
        AnsiConsole.Write(t);
        // Итоговая строка — не в таблице, чтобы визуально отделить сводку.
        AnsiConsole.MarkupLine($"[grey]Итого:[/] доход {rep.TotalIncome:F2}, расход {rep.TotalExpense:F2}");
    }

    /// <summary>
    /// Аггрегация по категориям: выводим доход/расход по каждой, сортировка по CategoryId.
    /// </summary>
    public void ByCategory(IEnumerable<CategoryTotal> v)
    {
        var t = new Table().Border(TableBorder.Rounded);
        t.AddColumns("cat","name","type","income","expense");
        foreach (var r in v.OrderBy(x=>x.CategoryId))
            t.AddRow(r.CategoryId.ToString(), r.Name, r.Type.ToString().ToLower(),
                     r.Income.ToString("F2"), r.Expense.ToString("F2"));
        AnsiConsole.Write(t);
    }

    /// <summary>
    /// Помесячная сводка: доход, расход, чистый результат (net = income - expense).
    /// Месяц выводится как ключ агрегирования (например, "2025-11").
    /// </summary>
    public void ByMonth(IEnumerable<MonthRow> v)
    {
        var t = new Table().Border(TableBorder.Rounded);
        t.AddColumns("month","income","expense","net");
        foreach (var r in v.OrderBy(x=>x.Ym))
            t.AddRow(r.Ym, r.Income.ToString("F2"), r.Expense.ToString("F2"),
                     (r.Income - r.Expense).ToString("F2"));
        AnsiConsole.Write(t);
    }

    /// <summary>
    /// Топ расходов по категориям: предполагается уже отсортированный вход, печатаем как есть.
    /// </summary>
    public void TopExpenses(IEnumerable<TopExpenseRow> v)
    {
        var t = new Table().Border(TableBorder.Rounded);
        t.AddColumns("cat","name","expense");
        foreach (var r in v)
            t.AddRow(r.CategoryId.ToString(), r.Name, r.Expense.ToString("F2"));
        AnsiConsole.Write(t);
    }
}
