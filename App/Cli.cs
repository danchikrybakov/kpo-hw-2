using Spectre.Console;
using FinanceTracker.Repository;
using FinanceTracker.Importers;
using FinanceTracker.Exporters;
using FinanceTracker.Facade;
using FinanceTracker.Domain;
using FinanceTracker.Commands;
using FinanceTracker.Commands.Decorators;

namespace FinanceTracker.App;

/// <summary>
/// Консольный интерфейс: маршрутизация одноразовых команд и REPL.
/// Не хранит бизнес-состояния; работает поверх репозиториев и фасада аналитики.
/// </summary>
public sealed class Cli
{
    private readonly IBankAccountRepository _acc;
    private readonly ICategoryRepository _cat;
    private readonly IOperationRepository _op;
    private readonly AnalyticsFacade _analytics;
    private readonly TablePrinter _print;

    /// <summary>
    /// Все зависимости приходят через DI: репозитории, фасад, принтер таблиц.
    /// </summary>
    public Cli(IBankAccountRepository acc, ICategoryRepository cat, IOperationRepository op,
               AnalyticsFacade analytics, TablePrinter print)
    {
        _acc = acc; _cat = cat; _op = op; _analytics = analytics; _print = print;
    }

    /// <summary>
    /// Точка входа для одноразового запуска команды.
    /// При отсутствии аргументов показывает справку и завершает работу.
    /// Исключения форматируются как сообщение об ошибке.
    /// </summary>
    public Task RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return Task.CompletedTask;
        }

        try
        {
            Execute(args);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] " + ex.Message);
            return Task.FromException(ex);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Разбор первой лексемы как имени команды и передача управления обработчику.
    /// Бросает исключение при неизвестной команде или «лишних» аргументах.
    /// </summary>
    private void Execute(string[] args)
    {
        var a = new ArgReader(args);

        var cmd = a.PopTokenOrThrow("command");
        switch (cmd)
        {
            case "help": PrintHelp(); break;

            case "import":    CmdImport(a); break;
            case "export":    CmdExport(a); break;

            case "list":      CmdList(a); break;

            case "reconcile": CmdReconcile(a); break;

            case "report":    CmdReport(a); break;

            case "create":    CmdCreate(a); break;
            case "edit":      CmdEdit(a);   break;
            case "delete":    CmdDelete(a); break;

            case "repl":      CmdRepl(a);   break;

            default: throw new Exception("unknown command: " + cmd);
        }
        a.ExpectEnd();
    }

    /// <summary>
    /// Запускает выбранную команду через декоратор замера времени.
    /// </summary>
    private void Run(string name, FinanceTracker.Commands.ICommand cmd)
        => new TimedCommand(cmd, name).Execute();

    /// <summary>
    /// import &lt;csv|json|yaml&gt; &lt;path&gt;
    /// </summary>
    private void CmdImport(ArgReader a)
    {
        var fmt  = a.PopTokenOr("format", "csv");
        var path = a.PopTokenOrThrow("path");
        Run("import", new ImportCommand(MakeImporter(fmt), _acc, _cat, _op, path));
    }

    /// <summary>
    /// export &lt;json|csv|yaml&gt; &lt;dest&gt; [from &lt;csv|json|yaml&gt; &lt;src&gt;]
    /// Без блока <c>from</c> экспортируется текущее состояние в памяти.
    /// </summary>
    private void CmdExport(ArgReader a)
    {
        var fmtOut = a.PopTokenOrThrow("fmtOut");
        var dest   = a.PopTokenOrThrow("dest");

        if (!a.HasMore)
        {
            Run("export", new ExportCommand(MakeExporter(fmtOut), _acc, _cat, _op, fmtOut, dest));
            return;
        }

        var kw = a.PopToken();
        if (!kw.Equals("from", StringComparison.OrdinalIgnoreCase))
            throw new Exception("ожидалось ключевое слово 'from' или окончание команды");

        var fmtIn = a.PopTokenOrThrow("fmtIn");
        var src   = a.PopTokenOrThrow("src");

        Run("export", new ExportCommand(MakeExporter(fmtOut), _acc, _cat, _op, fmtOut, dest, MakeImporter(fmtIn), src));
    }

    // Зарезервировано на случай добавления автоматического создания директорий назначения.
    private static string PrepareDest(string fmtOut, string dest)
    {
        var f = fmtOut.ToLowerInvariant();
        if (f == "csv")
        {
            // Для CSV — каталог; если передали путь к файлу, трактуем как имя каталога.
            Directory.CreateDirectory(dest);
            return dest;
        }
        var dir = Path.GetDirectoryName(dest);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        return dest;
    }

    /// <summary>
    /// list accounts | list categories | list operations [фильтры]
    /// </summary>
    private void CmdList(ArgReader a)
    {
        var sub = a.PopTokenOrThrow("list-subcommand");
        switch (sub)
        {
            case "accounts":   Run("list accounts",   new ListAccountsCommand(_acc, _print)); break;
            case "categories": Run("list categories", new ListCategoriesCommand(_cat, _print)); break;
            case "operations":
            {
                long? accId = null, catId = null; string? from = null, to = null, contains = null; int? max = null;
                while (a.HasMore)
                {
                    var t = a.PopToken();
                    switch (t)
                    {
                        case "--acc":      accId = a.PopLong("acc id");      break;
                        case "--cat":      catId = a.PopLong("cat id");      break;
                        case "--from":     from  = a.PopTokenOrThrow("date"); break;
                        case "--to":       to    = a.PopTokenOrThrow("date"); break;
                        case "--max":      max   = (int)a.PopLong("N");       break;
                        case "--contains": contains = a.PopTokenOrThrow("text"); break;
                        default: throw new Exception("unknown option: " + t);
                    }
                }
                Run("list operations", new ListOperationsCommand(_op, _print, accId, catId, from, to, contains, max));
                break;
            }
            default: throw new Exception("unknown list subcommand: " + sub);
        }
    }

    /// <summary>
    /// reconcile [--apply] — сверка расчётных балансов с операциями; с флагом применяется к данным.
    /// </summary>
    private void CmdReconcile(ArgReader a)
    {
        bool apply = false;
        while (a.HasMore)
        {
            var t = a.PopToken();
            if (t == "--apply") apply = true;
            else throw new Exception("unknown option: " + t);
        }
        Run("reconcile", new ReconcileCommand(_analytics, _print, apply));
    }

    /// <summary>
    /// report categories | report monthly [--year ...] [--acc ...] [--cat ...] | report top [--n ...] [--from ...] [--to ...]
    /// </summary>
    private void CmdReport(ArgReader a)
    {
        var sub = a.PopTokenOrThrow("report-subcommand");
        switch (sub)
        {
            case "categories":
                Run("report categories", new ReportCategoriesCommand(_analytics, _print));
                break;

            case "monthly":
            {
                int? year = null; long? acc=null, cat=null;
                while (a.HasMore)
                {
                    var t = a.PopToken();
                    switch (t)
                    {
                        case "--year": year = (int)a.PopLong("year"); break;
                        case "--acc":  acc  = a.PopLong("acc id");    break;
                        case "--cat":  cat  = a.PopLong("cat id");    break;
                        default: throw new Exception("unknown option: " + t);
                    }
                }
                Run("report monthly", new ReportMonthlyCommand(_analytics, _print, year, acc, cat));
                break;
            }

            case "top":
            {
                int n = 10; string? from=null, to=null;
                while (a.HasMore)
                {
                    var t = a.PopToken();
                    switch (t)
                    {
                        case "--n":    n    = (int)a.PopLong("N");            break;
                        case "--from": from = a.PopTokenOrThrow("date");      break;
                        case "--to":   to   = a.PopTokenOrThrow("date");      break;
                        default: throw new Exception("unknown option: " + t);
                    }
                }
                Run("report top", new ReportTopCommand(_analytics, _print, n, from, to));
                break;
            }
            default: throw new Exception("unknown report subcommand: " + sub);
        }
    }

    /// <summary>
    /// create account|category|operation ...
    /// </summary>
    private void CmdCreate(ArgReader a)
    {
        var sub = a.PopTokenOrThrow("entity");
        switch (sub)
        {
            case "account":
            {
                var id=a.PopLong("id"); var name=a.PopTokenOrThrow("name"); var bal=a.PopDouble("balance");
                Run("create account", new CreateAccountCommand(_acc, id, name, bal)); break;
            }
            case "category":
            {
                var id=a.PopLong("id"); var type=Types.ParseOperationType(a.PopTokenOrThrow("type")); var name=a.PopTokenOrThrow("name");
                Run("create category", new CreateCategoryCommand(_cat, id, type, name)); break;
            }
            case "operation":
            {
                var id=a.PopLong("id"); var type=Types.ParseOperationType(a.PopTokenOrThrow("type"));
                var acc=a.PopLong("acc_id"); var cat=a.PopLong("cat_id"); var amt=a.PopDouble("amount"); var date=a.PopTokenOrThrow("date");
                var desc=a.HasMore ? a.PopRest() : null;
                Run("create operation", new CreateOperationCommand(_op, id, type, acc, cat, amt, date, desc)); break;
            }
            default: throw new Exception("unknown entity: " + sub);
        }
    }

    /// <summary>
    /// edit account|category|operation ...
    /// </summary>
    private void CmdEdit(ArgReader a)
    {
        var sub = a.PopTokenOrThrow("entity");
        switch (sub)
        {
            case "account":
            {
                var id=a.PopLong("id"); string? name=null; double? bal=null;
                while (a.HasMore){ var t=a.PopToken(); if (t=="--name") name=a.PopTokenOrThrow("name"); else if (t=="--balance") bal=a.PopDouble("balance"); else throw new Exception("unknown option: "+t); }
                Run("edit account", new EditAccountCommand(_acc, id, name, bal)); break;
            }
            case "category":
            {
                var id=a.PopLong("id"); string? tStr=null, name=null;
                while (a.HasMore){ var t=a.PopToken(); if (t=="--type") tStr=a.PopTokenOrThrow("type"); else if (t=="--name") name=a.PopTokenOrThrow("name"); else throw new Exception("unknown option: "+t); }
                Run("edit category", new EditCategoryCommand(_cat, id, tStr, name)); break;
            }
            case "operation":
            {
                var id=a.PopLong("id"); string? type=null,date=null,desc=null; long? acc=null,cat=null; double? amt=null;
                while (a.HasMore)
                {
                    var x=a.PopToken();
                    switch(x){
                        case "--type":   type=a.PopTokenOrThrow("type"); break;
                        case "--acc":    acc=a.PopLong("acc id");        break;
                        case "--cat":    cat=a.PopLong("cat id");        break;
                        case "--amount": amt=a.PopDouble("amount");      break;
                        case "--date":   date=a.PopTokenOrThrow("date"); break;
                        case "--desc":   desc=a.PopRest();               break;
                        default: throw new Exception("unknown option: "+x);
                    }
                }
                Run("edit operation", new EditOperationCommand(_op, id, type, acc, cat, amt, date, desc)); break;
            }
            default: throw new Exception("unknown entity: " + sub);
        }
    }

    /// <summary>
    /// delete account|category|operation &lt;id&gt;
    /// </summary>
    private void CmdDelete(ArgReader a)
    {
        var sub = a.PopTokenOrThrow("entity"); var id = a.PopLong("id");
        switch (sub)
        {
            case "account":   Run("delete account",   new DeleteAccountCommand(_acc, id)); break;
            case "category":  Run("delete category",  new DeleteCategoryCommand(_cat, id)); break;
            case "operation": Run("delete operation", new DeleteOperationCommand(_op, id)); break;
            default: throw new Exception("unknown entity: " + sub);
        }
    }

    /// <summary>
    /// REPL: первая строка загружает данные из указанного формата/пути,
    /// далее поддерживаются команды до "quit"/"exit". Команда "help" печатает справку.
    /// </summary>
    private void CmdRepl(ArgReader a)
    {
        var fmt  = a.PopTokenOr("format", "csv");
        var path = a.PopTokenOrThrow("path");

        ClearAll();
        MakeImporter(fmt).Import(path, new ImportTarget(_acc, _cat, _op));
        
        PrintHelp();

        while (true)
        {
            var input = AnsiConsole.Ask<string>("[grey]ft>[/] ");
            var line = input.Trim();
            if (line.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
                line.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;
            if (line.Equals("help", StringComparison.OrdinalIgnoreCase))
            { PrintHelp(); continue; }
            if (string.IsNullOrWhiteSpace(line)) continue;

            try { Execute(SplitArgs(line).ToArray()); }
            catch (Exception ex) { AnsiConsole.MarkupLine("[red]Error:[/] " + ex.Message); }
        }
    }

    /// <summary>
    /// Печать краткой справки по доступным командам.
    /// </summary>
    private void PrintHelp()
    {
        AnsiConsole.MarkupLine("[bold]Команды:[/]");
        AnsiConsole.WriteLine("  help | quit | exit");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Просмотр:[/]");
        AnsiConsole.WriteLine("  list accounts");
        AnsiConsole.WriteLine("  list categories");
        AnsiConsole.WriteLine("  list operations [--acc ID] [--cat ID] [--from DATE] [--to DATE] [--max N] [--contains TEXT]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Аналитика:[/]");
        AnsiConsole.WriteLine("  reconcile [--apply]");
        AnsiConsole.WriteLine("  report categories");
        AnsiConsole.WriteLine("  report monthly [--year YYYY] [--acc ID] [--cat ID]");
        AnsiConsole.WriteLine("  report top [--n N] [--from YYYY-MM-DD] [--to YYYY-MM-DD]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Импорт/экспорт:[/]");
        AnsiConsole.WriteLine("  import <csv|json|yaml> <path>");
        AnsiConsole.WriteLine("  export <json|csv|yaml> <dest>");
        AnsiConsole.WriteLine("  export <json|csv|yaml> <dest> from <csv|json|yaml> <src>");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]CRUD:[/]");
        AnsiConsole.WriteLine("  create account <id> <name> <balance>");
        AnsiConsole.WriteLine("  edit   account <id> [--name \"New\"] [--balance 123.45]");
        AnsiConsole.WriteLine("  delete account <id>");
        AnsiConsole.WriteLine("  create category <id> <income|expense> <name>");
        AnsiConsole.WriteLine("  edit   category <id> [--type income|expense] [--name \"New\"]");
        AnsiConsole.WriteLine("  delete category <id>");
        AnsiConsole.WriteLine("  create operation <id> <income|expense> <acc_id> <cat_id> <amount> <date> [description]");
        AnsiConsole.WriteLine("  edit   operation <id> [--type T] [--acc ID] [--cat ID] [--amount X] [--date DATE] [--desc \"text\"]");
        AnsiConsole.WriteLine("  delete operation <id>");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]REPL:[/]");
        AnsiConsole.WriteLine("  repl <csv|json|yaml> <path>");
    }

    private void ClearAll() { _acc.Clear(); _cat.Clear(); _op.Clear(); }

    /// <summary>
    /// Преобразование введённой строки в массив аргументов:
    /// поддерживаются двойные кавычки для аргументов с пробелами.
    /// </summary>
    private static List<string> SplitArgs(string s)
    {
        var r = new List<string>(); var cur = "";
        bool inQ = false;
        for (int i=0;i<s.Length;i++){
            var ch = s[i];
            if (ch=='"'){ inQ=!inQ; continue; }
            if (char.IsWhiteSpace(ch) && !inQ){ if (cur.Length>0){ r.Add(cur); cur=""; } }
            else cur+=ch;
        }
        if (cur.Length>0) r.Add(cur);
        return r;
    }

    /// <summary>
    /// Фабрика импортёров: маппинг имени формата на реализацию.
    /// </summary>
    private static BaseImporter MakeImporter(string fmt) => fmt.ToLowerInvariant() switch
    {
        "csv"  => new CsvImporter(),
        "json" => new JsonImporter(),
        "yaml" or "yml" => new YamlImporter(),
        _ => throw new Exception("unknown import format: " + fmt)
    };

    /// <summary>
    /// Фабрика экспортёров: маппинг имени формата на реализацию.
    /// </summary>
    private static BaseExporter MakeExporter(string fmt) => fmt.ToLowerInvariant() switch
    {
        "json" => new JsonExporter(),
        "csv"  => new CsvExporter(),
        "yaml" or "yml" => new YamlExporter(),
        _ => throw new Exception("unknown export format: " + fmt)
    };

    /// <summary>
    /// Небольшой reader для аргументов: упрощает валидацию и сообщения об ошибках.
    /// </summary>
    private sealed class ArgReader
    {
        private readonly List<string> _t;
        private int _i;

        public ArgReader(IEnumerable<string> args){ _t = args.ToList(); _i = 0; }

        public bool HasMore => _i < _t.Count;

        public string PopToken() => HasMore ? _t[_i++] : "";

        /// <summary>Снять следующий токен или бросить исключение с понятным названием параметра.</summary>
        public string PopTokenOrThrow(string name)
        {
            if (!HasMore) throw new Exception($"missing {name}");
            return _t[_i++];
        }

        /// <summary>Снять следующий токен или вернуть значение по умолчанию.</summary>
        public string PopTokenOr(string name, string def) => HasMore ? _t[_i++] : def;

        /// <summary>Снять целое; сообщение об ошибке привязано к логическому имени параметра.</summary>
        public long PopLong(string name)
        {
            var s = PopTokenOrThrow(name);
            if (!long.TryParse(s, out var v)) throw new Exception($"{name} must be integer");
            return v;
        }

        /// <summary>Снять число с плавающей точкой (инвариантная культура).</summary>
        public double PopDouble(string name)
        {
            var s = PopTokenOrThrow(name);
            if (!double.TryParse(s, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var v))
                throw new Exception($"{name} must be number");
            return v;
        }

        /// <summary>Вернуть остаток строки как одно поле (для описаний).</summary>
        public string? PopRest()
        {
            if (!HasMore) return null;
            var rest = string.Join(' ', _t.Skip(_i));
            _i = _t.Count;
            return rest;
        }

        /// <summary>Проверка, что аргументы не остались; иначе — человекочитаемая ошибка.</summary>
        public void ExpectEnd()
        {
            if (HasMore) throw new Exception("too many arguments: " + string.Join(' ', _t.Skip(_i)));
        }
    }
}
