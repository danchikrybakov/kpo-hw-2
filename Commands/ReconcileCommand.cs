using FinanceTracker.App;
using Spectre.Console;
using FinanceTracker.Facade;

namespace FinanceTracker.Commands;

/// <summary>
/// Сверка счётов: считает фактические балансы по операциям и сравнивает их
/// с «заявленными» (stored balance). Опционально может применить правки.
/// </summary>
public sealed class ReconcileCommand : ICommand
{
    private readonly AnalyticsFacade _analytics;
    private readonly TablePrinter _printer;
    private readonly bool _apply;

    /// <param name="analytics">
    /// Фасад аналитики: предоставляет расчёт отчёта и (по запросу) применение правок.
    /// </param>
    /// <param name="printer">Вывод табличного отчёта в консоль.</param>
    /// <param name="apply">
    /// Если true — сначала «подтягиваем» заявленные балансы к рассчитанным (побочный эффект).
    /// Если false — только показываем отчёт без изменений данных.
    /// </param>
    public ReconcileCommand(AnalyticsFacade analytics, TablePrinter printer, bool apply)
    {
        _analytics = analytics;
        _printer = printer;
        _apply = apply;
    }

    /// <summary>
    /// При необходимости применяет правки (update балансов), затем печатает отчёт по всем счетам:
    /// заявленный / рассчитанный / дельта / доход / расход и итоговую строку.
    /// </summary>
    public int Execute()
    {
        if (_apply)
        {
            var r = _analytics.ReconcileApply();
            // Краткая сводка о факте применения и суммарной «модуле» правки.
            AnsiConsole.MarkupLine("[yellow]Применено:[/] {0} счёта(ов); |правка| = {1:F2}", r.Updated, r.TotalAbsDelta);
        }

        // Печать актуального отчёта (после применения правок или без них).
        _printer.Reconcile(_analytics.Reconcile());
        return 0;
    }
}