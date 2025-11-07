using Spectre.Console;
using FinanceTracker.Repository;
using FinanceTracker.Domain;

namespace FinanceTracker.Commands;

/// <summary>
/// Команда создания категории (CRUD: Create).
/// </summary>
public sealed class CreateCategoryCommand : ICommand
{
    private readonly ICategoryRepository _cat; 
    private readonly long _id; 
    private readonly OperationType _type; 
    private readonly string _name;

    /// <param name="cat">Репозиторий категорий.</param>
    /// <param name="id">Идентификатор новой категории.</param>
    /// <param name="type">Тип операций категории (income/expense).</param>
    /// <param name="name">Название категории.</param>
    public CreateCategoryCommand(ICategoryRepository cat, long id, OperationType type, string name)
    { 
        _cat = cat; _id = id; _type = type; _name = name; 
    }

    /// <summary>Добавляет категорию и печатает подтверждение.</summary>
    public int Execute()
    { 
        _cat.Add(new Category(_id, _type, _name)); 
        AnsiConsole.MarkupLine($"[green]Создана категория:[/] id={_id}, type={_type}, name=\"{_name}\""); 
        return 0; 
    }
}

/// <summary>
/// Команда редактирования категории (CRUD: Update).
/// Поддерживает частичное обновление: незаданные поля не меняются.
/// </summary>
public sealed class EditCategoryCommand : ICommand
{
    private readonly ICategoryRepository _cat; 
    private readonly long _id; 
    private readonly string? _type; 
    private readonly string? _name;

    /// <param name="cat">Репозиторий категорий.</param>
    /// <param name="id">Идентификатор редактируемой категории.</param>
    /// <param name="type">Новый тип (строка "income"/"expense", необязательно).</param>
    /// <param name="name">Новое имя (необязательно).</param>
    public EditCategoryCommand(ICategoryRepository cat, long id, string? type, string? name)
    { 
        _cat = cat; _id = id; _type = type; _name = name; 
    }

    /// <summary>
    /// Обновляет только переданные поля; печатает сводку изменений.
    /// Бросает исключение, если категория не найдена.
    /// </summary>
    public int Execute()
    {
        var cur = _cat.Get(_id) ?? throw new Exception("category not found");
        var up = cur with
        {
            // Если тип не передан — оставляем прежний; иначе парсим строковое значение.
            Type = _type is null ? cur.Type : Types.ParseOperationType(_type),
            Name = _name ?? cur.Name
        };
        _cat.Update(up);

        var info = new List<string>();
        if (up.Type != cur.Type) info.Add($"type: {cur.Type} → {up.Type}");
        if (up.Name != cur.Name) info.Add($"name: \"{cur.Name}\" → \"{up.Name}\"");

        AnsiConsole.MarkupLine($"[yellow]Изменена категория {_id}:[/] {(info.Count > 0 ? string.Join(", ", info) : "без изменений")}");
        return 0;
    }
}

/// <summary>
/// Команда удаления категории (CRUD: Delete).
/// </summary>
public sealed class DeleteCategoryCommand : ICommand
{
    private readonly ICategoryRepository _cat; 
    private readonly long _id;

    /// <param name="cat">Репозиторий категорий.</param>
    /// <param name="id">Идентификатор удаляемой категории.</param>
    public DeleteCategoryCommand(ICategoryRepository cat, long id)
    { 
        _cat = cat; _id = id; 
    }

    /// <summary>Удаляет категорию и печатает подтверждение.</summary>
    public int Execute()
    { 
        _cat.Remove(_id); 
        AnsiConsole.MarkupLine($"[red]Удалена категория:[/] id={_id}"); 
        return 0; 
    }
}
