using System.Collections.Generic;

namespace FinanceTracker.Repository;

/// <summary>
/// Унифицированный контракт репозитория для сущностей с целочисленным идентификатором (long).
/// Требования к реализациям:
/// • Add/Update должны бросать исключение при конфликте/отсутствии записи;
/// • All() возвращает детерминированный порядок (в проекте — порядок вставки);
/// • Clear() очищает все данные.
/// </summary>
/// <typeparam name="T">Доменная сущность.</typeparam>
public interface IRepository<T>
{
    /// <summary>Добавляет объект. Должен бросать, если запись с таким id уже существует.</summary>
    void Add(T obj);

    /// <summary>Проверяет существование записи по идентификатору.</summary>
    bool Exists(long id);

    /// <summary>Возвращает запись по идентификатору или null, если не найдена.</summary>
    T? Get(long id);

    /// <summary>Обновляет существующую запись. Должен бросать, если запись не найдена.</summary>
    void Update(T obj);

    /// <summary>Удаляет запись по идентификатору. Поведение при отсутствии — по выбору реализации.</summary>
    void Remove(long id);

    /// <summary>
    /// Возвращает снимок всех записей.
    /// Порядок должен быть детерминированным (в InMemory-реализациях — порядок вставки).
    /// </summary>
    IReadOnlyList<T> All();

    /// <summary>Очищает репозиторий.</summary>
    void Clear();
}