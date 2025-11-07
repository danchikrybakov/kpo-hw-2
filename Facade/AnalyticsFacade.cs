using FinanceTracker.Domain;

namespace FinanceTracker.Facade;

/// <summary>Суммы дохода/расхода по каждой категории.</summary>
public record CategoryTotal(long CategoryId, string Name, OperationType Type, double Income, double Expense);

/// <summary>Строка отчёта сверки по одному счёту.</summary>
public record ReconcileRow(long AccountId, string Name, double Declared, double Computed, double Delta, double Income, double Expense);

/// <summary>Итоговый отчёт сверки по всем счетам.</summary>
public record ReconcileReport(IReadOnlyList<ReconcileRow> Rows, double TotalIncome, double TotalExpense);

/// <summary>Агрегированные суммы по месяцу (YYYY-MM).</summary>
public record MonthRow(string Ym, double Income, double Expense);

/// <summary>Строка «топ расходов»: категория и её суммарный расход.</summary>
public record TopExpenseRow(long CategoryId, string Name, double Expense);

/// <summary>Результат применения сверки (сколько записей переписано и общая |правка|).</summary>
public record ReconcileApplyResult(int Updated, double TotalAbsDelta);

/// <summary>
/// Фасад над репозиториями для вычисления отчётов и сверок.
/// Вся логика агрегаций и фильтрации сосредоточена здесь, а хранение — внутри репозиториев.
/// </summary>
public sealed class AnalyticsFacade
{
    private readonly Repository.IBankAccountRepository _acc;
    private readonly Repository.ICategoryRepository    _cat;
    private readonly Repository.IOperationRepository   _op;

    public AnalyticsFacade(Repository.IBankAccountRepository a, Repository.ICategoryRepository c, Repository.IOperationRepository o)
    { _acc = a; _cat = c; _op = o; }

    /// <summary>
    /// Свод по категориям: для каждой категории — сумма доходов и сумма расходов.
    /// Порядок — по возрастанию <see cref="CategoryTotal.CategoryId"/>.
    /// </summary>
    public IReadOnlyList<CategoryTotal> ByCategory()
    {
        // Подготовим карту «категория → агрегаты» (с нулями), чтобы корректно вернуть категории без операций.
        var map = _cat.All().ToDictionary(c => c.Id, c => new CategoryTotal(c.Id, c.Name, c.Type, 0, 0));

        foreach (var o in _op.All())
        {
            if (!map.TryGetValue(o.CategoryId, out var ct)) continue; // защитимся от «битых» ссылок
            ct = o.Type == OperationType.Income
                ? ct with { Income  = ct.Income  + o.Amount }
                : ct with { Expense = ct.Expense + o.Amount };
            map[o.CategoryId] = ct;
        }
        return map.Values.OrderBy(x => x.CategoryId).ToList();
    }

    /// <summary>
    /// Сверка: для каждого счёта считаем «computed = income - expense» по его операциям,
    /// сравниваем с заявленным балансом (<see cref="ReconcileRow.Declared"/>) и вычисляем дельту.
    /// </summary>
    public ReconcileReport Reconcile()
    {
        var rows = new List<ReconcileRow>();
        double tin = 0, tex = 0;

        foreach (var a in _acc.All())
        {
            double inc = 0, exp = 0;

            // Фильтруем операции по счёту. Формат дат уже валидный (YYYY-MM-DD), так что сравнение строк допустимо где нужно.
            foreach (var o in _op.All().Where(x => x.BankAccountId == a.Id))
            {
                if (o.Type == OperationType.Income) inc += o.Amount;
                else                                exp += o.Amount;
            }

            var comp  = inc - exp;
            var delta = a.Balance - comp;

            rows.Add(new ReconcileRow(a.Id, a.Name, a.Balance, comp, delta, inc, exp));
            tin += inc; tex += exp;
        }

        return new ReconcileReport(rows.OrderBy(r => r.AccountId).ToList(), tin, tex);
    }

    /// <summary>
    /// Применяет сверку: переписывает <see cref="BankAccount.Balance"/> точным «computed = income - expense».
    /// Порог <paramref name="eps"/> используется, чтобы не трогать записи из-за микроскопических расхождений double.
    /// </summary>
    public ReconcileApplyResult ReconcileApply(double eps = 1e-9)
    {
        // Сначала посчитаем целевые значения по всем счетам.
        var comp = _acc.All().ToDictionary(a => a.Id, _ => 0.0);
        foreach (var o in _op.All())
        {
            comp[o.BankAccountId] = comp.GetValueOrDefault(o.BankAccountId) +
                                    (o.Type == OperationType.Income ? o.Amount : -o.Amount);
        }

        var factory = new Domain.DomainFactory();
        int updated = 0; double sum = 0;

        // Обновляем только те счета, где |delta| > eps.
        foreach (var a in _acc.All())
        {
            var target = comp[a.Id];
            var delta  = a.Balance - target;
            if (Math.Abs(delta) > eps)
            {
                _acc.Update(factory.MakeAccount(a.Id, a.Name, target));
                updated++;
                sum += Math.Abs(delta);
            }
        }
        return new ReconcileApplyResult(updated, sum);
    }

    /// <summary>
    /// Помесячные суммы (YYYY-MM). Фильтры по году, счёту и категории применяются независимо.
    /// </summary>
    public IReadOnlyList<MonthRow> ByMonth(int? year = null, long? accId = null, long? catId = null)
    {
        var map = new Dictionary<string, MonthRow>();

        foreach (var o in _op.All())
        {
            if (accId is { } a && o.BankAccountId != a) continue;
            if (catId is { } c && o.CategoryId    != c) continue;

            if (year is { } y)
            {
                // Дата гарантированно в формате YYYY-MM-DD (см. импортеры).
                if (o.Date.Length < 4 || int.Parse(o.Date[..4]) != y) continue;
            }

            if (o.Date.Length < 7) continue; // на всякий случай: нужен YYYY-MM
            var ym = o.Date[..7];

            map.TryAdd(ym, new MonthRow(ym, 0, 0));
            var cur = map[ym];
            cur = o.Type == OperationType.Income ? cur with { Income  = cur.Income  + o.Amount }
                                                 : cur with { Expense = cur.Expense + o.Amount };
            map[ym] = cur;
        }

        return map.Values.OrderBy(x => x.Ym).ToList();
    }

    /// <summary>
    /// Топ категорий по расходам. Можно ограничить периодом <paramref name="from"/>–<paramref name="to"/> (включительно).
    /// </summary>
    public IReadOnlyList<TopExpenseRow> TopExpenses(int n = 10, string? from = null, string? to = null)
    {
        bool InRange(string d)
        {
            // Поскольку формат дат YYYY-MM-DD, лексикографическое сравнение безопасно для порядка.
            if (from is { } f && string.Compare(d, f, StringComparison.Ordinal) < 0) return false;
            if (to   is { } t && string.Compare(d, t, StringComparison.Ordinal) > 0) return false;
            return true;
        }

        // Инициализируем все категории-расходы нулём, чтобы категории без операций тоже учитывались (и отфильтруем позже).
        var exp = new Dictionary<long, double>();
        foreach (var c in _cat.All().Where(x => x.Type == OperationType.Expense))
            exp[c.Id] = 0;

        foreach (var o in _op.All())
        {
            if (o.Type != OperationType.Expense) continue;
            if (!InRange(o.Date)) continue;
            exp[o.CategoryId] = exp.GetValueOrDefault(o.CategoryId) + o.Amount;
        }

        var name = _cat.All().ToDictionary(c => c.Id, c => c.Name);

        return exp.Where(kv => kv.Value > 0)
                  .Select(kv => new TopExpenseRow(kv.Key, name.GetValueOrDefault(kv.Key, "?"), kv.Value))
                  .OrderByDescending(x => x.Expense).ThenBy(x => x.CategoryId)
                  .Take(n)
                  .ToList();
    }
}
