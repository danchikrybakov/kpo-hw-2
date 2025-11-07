namespace FinanceTracker.Repository.InMemory;

using FinanceTracker.Domain;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Репозиторий категорий в памяти с сохранением порядка вставки.
/// <list type="bullet">
/// <item><description>Уникальность по <c>Id</c> (дубли — исключение).</description></item>
/// <item><description>Обновление не меняет порядок.</description></item>
/// <item><description><see cref="All"/> возвращает элементы в порядке импорта/создания.</description></item>
/// </list>
/// </summary>
public sealed class InMemoryCategoryRepository : ICategoryRepository
{
    private readonly Dictionary<long, Category> _data = new(); // быстрый доступ по Id
    private readonly List<long> _order = new();                // порядок отображения (insertion order)

    /// <summary>Добавляет категорию; при повторном Id выбрасывается исключение.</summary>
    public void Add(Category obj)
    {
        if (_data.ContainsKey(obj.Id))
            throw new System.Exception("category already exists: " + obj.Id);

        _data[obj.Id] = obj;
        _order.Add(obj.Id);
    }

    /// <summary>Проверяет существование категории по Id.</summary>
    public bool Exists(long id) => _data.ContainsKey(id);

    /// <summary>Возвращает категорию по Id или <c>null</c>, если не найдена.</summary>
    public Category? Get(long id) => _data.TryGetValue(id, out var v) ? v : null;

    /// <summary>Обновляет категорию; порядок вывода не меняется.</summary>
    public void Update(Category obj)
    {
        if (!_data.ContainsKey(obj.Id))
            throw new System.Exception("category not found: " + obj.Id);

        _data[obj.Id] = obj;
    }

    /// <summary>Удаляет категорию по Id (если была) и убирает её из порядка.</summary>
    public void Remove(long id)
    {
        if (_data.Remove(id))
            _order.Remove(id);
    }

    /// <summary>Все категории в порядке вставки (как требует UI/экспорт).</summary>
    public IReadOnlyList<Category> All() => _order.Select(id => _data[id]).ToList();

    /// <summary>Полная очистка репозитория.</summary>
    public void Clear()
    {
        _data.Clear();
        _order.Clear();
    }
}
