using System.Collections.Generic;
using FinanceTracker.Domain;
using FinanceTracker.Repository;
using FinanceTracker.Services;

namespace FinanceTracker.Repository.Proxies;

/// <summary>
/// Прокси над in-memory репозиторием счётов:
/// - Все операции чтения делегируются напрямую.
/// - После любой модификации (Add/Update/Remove/Clear) вызывается <see cref="ISnapshotWriter.Save"/>.
/// Таким образом, текущее состояние всегда сохраняется на диск (или в иной носитель).
/// </summary>
public sealed class FileBackedBankAccountRepositoryProxy : IBankAccountRepository
{
    private readonly InMemory.InMemoryBankAccountRepository _inner;
    private readonly ISnapshotWriter _snapshot;

    public FileBackedBankAccountRepositoryProxy(
        InMemory.InMemoryBankAccountRepository inner,
        ISnapshotWriter snapshot)
    {
        _inner = inner;
        _snapshot = snapshot;
    }

    /// <summary>Добавление счёта с немедленным сохранением снапшота.</summary>
    public void Add(BankAccount obj)
    {
        _inner.Add(obj);
        _snapshot.Save();
    }

    public bool Exists(long id) => _inner.Exists(id);

    public BankAccount? Get(long id) => _inner.Get(id);

    /// <summary>Обновление счёта и сохранение снапшота.</summary>
    public void Update(BankAccount obj)
    {
        _inner.Update(obj);
        _snapshot.Save();
    }

    /// <summary>Удаление счёта и сохранение снапшота.</summary>
    public void Remove(long id)
    {
        _inner.Remove(id);
        _snapshot.Save();
    }

    public IReadOnlyList<BankAccount> All() => _inner.All();

    /// <summary>Полная очистка и сохранение пустого состояния.</summary>
    public void Clear()
    {
        _inner.Clear();
        _snapshot.Save();
    }
}