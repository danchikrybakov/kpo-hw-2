using FinanceTracker.Domain;

namespace FinanceTracker.Export.Visitors;

/// <summary>
/// Интерфейс визитёра для обхода доменных сущностей перед экспортом.
/// Позволяет экспортёрам (JSON/CSV/YAML) единообразно «принимать» данные,
/// не зная деталей их получения и порядка хранения.
/// </summary>
/// <remarks>
/// Последовательность вызовов: <c>Begin()</c> → множество <c>Visit(...)</c> → <c>End()</c>.
/// Порядок элементов определяется вызывающей стороной (экспортёром).
/// </remarks>
public interface IFinanceVisitor
{
    /// <summary>
    /// Инициализация обхода (подготовка внутренних буферов, запись заголовков и т.п.).
    /// </summary>
    void Begin();

    /// <summary>Обработка одного банковского счёта.</summary>
    void Visit(BankAccount a);

    /// <summary>Обработка одной категории.</summary>
    void Visit(Category c);

    /// <summary>Обработка одной операции.</summary>
    void Visit(Operation o);

    /// <summary>
    /// Завершение обхода (финализация буферов, доп. пост-обработка, сброс в поток).
    /// </summary>
    void End();
}