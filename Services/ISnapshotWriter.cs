namespace FinanceTracker.Services;

/// <summary>
/// Абстракция сервиса «снимка состояния»: сохраняет текущее состояние приложения
/// во внешнее хранилище (файл/каталог). Конкретный формат задаётся реализацией.
/// </summary>
public interface ISnapshotWriter
{
    /// <summary>Выполняет сохранение текущего состояния.</summary>
    void Save();
}