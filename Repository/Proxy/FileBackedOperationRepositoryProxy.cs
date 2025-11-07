using FinanceTracker.Domain;
using FinanceTracker.Repository;
using FinanceTracker.Services;

namespace FinanceTracker.Repository.Proxies;

public sealed class FileBackedOperationRepositoryProxy : IOperationRepository
{
    private readonly InMemory.InMemoryOperationRepository _inner;
    private readonly ISnapshotWriter _snapshot;

    public FileBackedOperationRepositoryProxy(
        InMemory.InMemoryOperationRepository inner,
        ISnapshotWriter snapshot)
    {
        _inner = inner;
        _snapshot = snapshot;
    }

    public void Add(Operation obj) { _inner.Add(obj); _snapshot.Save(); }
    public bool Exists(long id) => _inner.Exists(id);
    public Operation? Get(long id) => _inner.Get(id);
    public void Update(Operation obj) { _inner.Update(obj); _snapshot.Save(); }
    public void Remove(long id) { _inner.Remove(id); _snapshot.Save(); }
    public IReadOnlyList<Operation> All() => _inner.All();
    public void Clear() { _inner.Clear(); _snapshot.Save(); }
}