namespace FinanceTracker.Repository.InMemory;

using FinanceTracker.Domain;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Репозиторий счетов в памяти с сохранением порядка вставки.
/// <list type="bullet">
/// <item><description>Уникальность по <c>Id</c> (дубли — исключение).</description></item>
/// <item><description>Обновление не меняет порядок.</description></item>
/// <item><description><see cref="All"/> возвращает элементы в порядке импорта/создания.</description></item>
/// </list>
/// </summary>
public sealed class InMemoryBankAccountRepository : IBankAccountRepository
{
    // Быстрый доступ по Id
    private readonly Dictionary<long, BankAccount> _data = new();

    // Линейный порядок отображения (insertion order)
    private readonly List<long> _order = new();

    /// <summary>Добавляет счёт; при повторном Id выбрасывается исключение.</summary>
    public void Add(BankAccount obj)
    {
        if (_data.ContainsKey(obj.Id))
            throw new System.Exception("account already exists: " + obj.Id);

        _data[obj.Id] = obj;
        _order.Add(obj.Id);
    }

    /// <summary>Проверяет существование счёта по Id.</summary>
    public bool Exists(long id) => _data.ContainsKey(id);

    /// <summary>Возвращает счёт по Id или <c>null</c>, если не найден.</summary>
    public BankAccount? Get(long id) => _data.TryGetValue(id, out var v) ? v : null;

    /// <summary>Обновляет счёт; порядок вывода не меняется.</summary>
    public void Update(BankAccount obj)
    {
        if (!_data.ContainsKey(obj.Id))
            throw new System.Exception("account not found: " + obj.Id);

        _data[obj.Id] = obj; // порядок не меняем
    }

    /// <summary>Удаляет счёт по Id (если был) и убирает его из порядка.</summary>
    public void Remove(long id)
    {
        if (_data.Remove(id))
            _order.Remove(id);
    }

    /// <summary>
    /// Возвращает все счета в порядке вставки.
    /// Требуемый интерфейсом тип — <see cref="IReadOnlyList{T}"/>.
    /// </summary>
    public IReadOnlyList<BankAccount> All() => _order.Select(id => _data[id]).ToList();

    /// <summary>Полная очистка репозитория.</summary>
    public void Clear()
    {
        _data.Clear();
        _order.Clear();
    }
}
