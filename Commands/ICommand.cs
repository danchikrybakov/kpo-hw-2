namespace FinanceTracker.Commands;

/// Единый интерфейс для консольной команды.
public interface ICommand
{
    int Execute();
}