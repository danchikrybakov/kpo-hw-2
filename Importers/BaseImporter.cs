using FinanceTracker.Domain;

namespace FinanceTracker.Importers;

/// <summary>
/// Результат импорта: сколько сущностей каждого типа считано/загружено.
/// </summary>
public readonly record struct ImportResult(int Accounts, int Categories, int Operations);

/// <summary>
/// Базовый импортёр. Реализует паттерн «Шаблонный метод»:
/// публичный <see cref="Import"/> фиксирует протокол вызова,
/// а конкретный парсинг делегируется в <see cref="DoImport"/> у наследников.
/// </summary>
public abstract class BaseImporter
{
    /// <summary>
    /// Централизованное создание доменных объектов (единообразная нормализация значений).
    /// </summary>
    protected readonly DomainFactory Factory = new();

    /// <summary>
    /// Точка входа импорта: принимает путь к источнику и «мишень» для загрузки.
    /// </summary>
    /// <param name="path">Файл (json/yaml/единый csv) или папка (csv каталога).</param>
    /// <param name="t">Адаптер-мишень, предоставляющий репозитории для загрузки.</param>
    /// <returns>Сводка по импортированным сущностям.</returns>
    public ImportResult Import(string path, ImportTarget t)
        => DoImport(path, t);

    /// <summary>
    /// Реальная логика конкретного импортёра (CSV/JSON/YAML).
    /// Наследники обязаны вернуть корректные счётчики.
    /// </summary>
    protected abstract ImportResult DoImport(string path, ImportTarget t);
}