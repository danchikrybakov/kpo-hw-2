namespace FinanceTracker.Repository;

using FinanceTracker.Domain;

/// <summary>
/// Репозиторий категорий (<see cref="Category"/>).
/// Контракт наследуется от <see cref="IRepository{T}"/>: Add/Get/Update/Remove/All/Clear.
/// Реализации в проекте: InMemoryCategoryRepository, FileBackedCategoryRepositoryProxy.
/// </summary>
public interface ICategoryRepository : IRepository<Category> { }