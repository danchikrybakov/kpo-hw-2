using System.Diagnostics;
using Spectre.Console;

namespace FinanceTracker.Commands.Decorators;

/// <summary>
/// Декоратор команды, измеряющий время выполнения и печатающий результат в консоль.
/// Реализует паттерн «Декоратор»: не меняя логику исходной команды, добавляет
/// нефункциональное поведение (тайминг и человеко-читаемые сообщения).
/// </summary>
public sealed class TimedCommand : ICommand
{
    private readonly ICommand _inner;
    private readonly string _name;

    /// <param name="inner">Реальная команда, которую нужно выполнить.</param>
    /// <param name="name">Человекочитаемое имя команды для сообщений об ошибках.</param>
    public TimedCommand(ICommand inner, string name)
    {
        _inner = inner;
        _name  = name;
    }

    /// <summary>
    /// Запускает обёрнутую команду, замеряет длительность и печатает итог.
    /// В случае исключения печатается диагностическое сообщение с именем команды,
    /// исключение пробрасывается дальше (верхний уровень выставит ExitCode).
    /// </summary>
    public int Execute()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var code = _inner.Execute();
            sw.Stop();
            // Нейтральная строка об успешном завершении (без привязки к локали интерфейса).
            AnsiConsole.MarkupLine($"[grey]done in {sw.Elapsed.TotalMilliseconds:F3} ms[/]");
            return code;
        }
        catch
        {
            sw.Stop();
            // Сообщение об ошибке: показываем имя команды и длительность до сбоя.
            AnsiConsole.MarkupLine($"[red]Ошибка в '{_name}' (выполнялось {sw.Elapsed.TotalMilliseconds:F3} ms)[/]");
            throw;
        }
    }
}