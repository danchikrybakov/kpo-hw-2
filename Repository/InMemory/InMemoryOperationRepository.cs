namespace FinanceTracker.Repository.InMemory;

using FinanceTracker.Domain;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Репозиторий операций в памяти с сохранением порядка вставки.
/// Особенности:
/// - Уникальность по <c>Id</c> (дубли вызывают исключение).
/// - <see cref="Update"/> не меняет порядок отображения.
/// - <see cref="All"/> возвращает операции в порядке импорта/создания (для стабильного UI/экспорта).
/// </summary>
public sealed class InMemoryOperationRepository : IOperationRepository
{
    // Хранилище по ключу Id и список порядка для стабильного вывода
    private readonly Dictionary<long, Operation> _data = new();
    private readonly List<long> _order = new();

    /// <summary>Добавляет операцию; при существующем Id — исключение.</summary>
    public void Add(Operation obj)
    {
        if (_data.ContainsKey(obj.Id))
            throw new System.Exception("operation already exists: " + obj.Id);

        _data[obj.Id] = obj;
        _order.Add(obj.Id);
    }

    /// <summary>Проверка наличия по Id.</summary>
    public bool Exists(long id) => _data.ContainsKey(id);

    /// <summary>Возвращает операцию по Id или <c>null</c>, если не найдена.</summary>
    public Operation? Get(long id) => _data.TryGetValue(id, out var v) ? v : null;

    /// <summary>Обновляет операцию по Id; порядок вставки не меняется.</summary>
    public void Update(Operation obj)
    {
        if (!_data.ContainsKey(obj.Id))
            throw new System.Exception("operation not found: " + obj.Id);

        _data[obj.Id] = obj;
    }

    /// <summary>Удаляет операцию по Id (если была) и убирает её из порядка.</summary>
    public void Remove(long id)
    {
        if (_data.Remove(id))
            _order.Remove(id);
    }

    /// <summary>Все операции в порядке вставки.</summary>
    public IReadOnlyList<Operation> All() => _order.Select(id => _data[id]).ToList();

    /// <summary>Полная очистка репозитория.</summary>
    public void Clear()
    {
        _data.Clear();
        _order.Clear();
    }
}
