namespace FinanceTracker.Repository;

using FinanceTracker.Domain;

/// <summary>
/// Репозиторий для работы со счётами (<see cref="BankAccount"/>).
/// Наследует контракт из <see cref="IRepository{T}"/> (Add/Get/Update/Remove/All/Clear).
/// Типичные реализации в проекте: InMemoryBankAccountRepository и FileBackedBankAccountRepositoryProxy.
/// </summary>
public interface IBankAccountRepository : IRepository<BankAccount> { }