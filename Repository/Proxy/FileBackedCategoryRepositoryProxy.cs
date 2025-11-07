using System.Collections.Generic;
using FinanceTracker.Domain;
using FinanceTracker.Repository;
using FinanceTracker.Services;

namespace FinanceTracker.Repository.Proxies;

/// <summary>
/// Прокси над in-memory репозиторием категорий.
/// Чтение делегируется напрямую; после любой модификации вызывается <see cref="ISnapshotWriter.Save"/>.
/// Гарантирует, что состояние сразу сохраняется (на диск или иной носитель).
/// </summary>
public sealed class FileBackedCategoryRepositoryProxy : ICategoryRepository
{
    private readonly InMemory.InMemoryCategoryRepository _inner;
    private readonly ISnapshotWriter _snapshot;

    public FileBackedCategoryRepositoryProxy(
        InMemory.InMemoryCategoryRepository inner,
        ISnapshotWriter snapshot)
    {
        _inner = inner;
        _snapshot = snapshot;
    }

    /// <summary>Добавляет категорию и фиксирует снапшот.</summary>
    public void Add(Category obj)
    {
        _inner.Add(obj);
        _snapshot.Save();
    }

    public bool Exists(long id) => _inner.Exists(id);

    public Category? Get(long id) => _inner.Get(id);

    /// <summary>Обновляет категорию и фиксирует снапшот.</summary>
    public void Update(Category obj)
    {
        _inner.Update(obj);
        _snapshot.Save();
    }

    /// <summary>Удаляет категорию и фиксирует снапшот.</summary>
    public void Remove(long id)
    {
        _inner.Remove(id);
        _snapshot.Save();
    }

    public IReadOnlyList<Category> All() => _inner.All();

    /// <summary>Очищает все категории и фиксирует пустое состояние.</summary>
    public void Clear()
    {
        _inner.Clear();
        _snapshot.Save();
    }
}