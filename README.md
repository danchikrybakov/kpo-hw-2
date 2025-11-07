
# FinanceTracker — консольное приложение учёта личных финансов

**Студент:** _Рыбаков Даниил Андреевич_  
**Группа:** _БПИ246_  


---

## Содержание
1. [Назначение и обзор](#назначение-и-обзор)  
2. [Функциональность](#функциональность)  
3. [Сборка и запуск](#сборка-и-запуск)  
4. [Форматы данных (I/O)](#форматы-данных-io)  
5. [Командный интерфейс (REPL)](#командный-интерфейс-repl)  
6. [Архитектура проекта](#архитектура-проекта)  
7. [Паттерны проектирования (GoF)](#паттерны-проектирования-gof)  
8. [SOLID и GRASP — как применены](#solid-и-grasp--как-применены)  
9. [Диаграмма зависимостей по слоям](#диаграмма-зависимостей-по-слоям)  
10. [Кодстайл, локализация и UX](#кодстайл-локализация-и-ux)  
11. [Сценарии проверки (smoke)](#сценарии-проверки-smoke)  
12. [Известные ограничения](#известные-ограничения)

---

## Назначение и обзор

**FinanceTracker** — учебный консольный инструмент для учёта финансовых операций:
- импорт данных из **CSV / JSON / YAML** (в т.ч. единый CSV-файл с секциями `#%table:*` или папка из трёх CSV);
- консольный режим **REPL** на русском: просмотр, фильтрация, отчёты, сверка балансов, CRUD;
- экспорт текущего состояния в **JSON / YAML / CSV** в том же формате, что и импорт;
- измерение времени выполнения команд, аккуратный табличный вывод.

В проекте демонстрируются **8 паттернов GoF** + применение **SOLID/GRASP**. Зависимости внедряются через **Microsoft.Extensions.DependencyInjection** (DI).

---

## Функциональность

- **CRUD** для: `BankAccount`, `Category (income|expense)`, `Operation`.
- **Импорт**: CSV (папка/секционный), JSON, YAML.
- **Экспорт**: JSON, YAML, CSV (в «родном» формате, порядок объектов сохраняется).
- **Отчёты**:
  - «суммы по категориям» (доход/расход);
  - «помесячно» (доход/расход/сальдо) c фильтрами по году/счёту/категории;
  - «топ-расходов» (N категорий/операций за период).
- **Сверка баланса**: `reconcile` + `reconcile --apply` (пересчитать/применить баланс счётов по операциям).
- **REPL**: подсказки `help`, сообщения об ошибках на русском, подтверждения CRUD.
- **Автоснапшот (Proxy)**: любые изменения (create/edit/delete/clear) записываются в `Data/autosave.yaml`.

---

## Сборка и запуск

Требования: **.NET 8**

```bash
# Сборка
# Если не запускается, то ниже везде указать полные пути 
cd FinanceTracker
dotnet build
dotnet run -- repl yaml Data/data.yaml
dotnet run -- repl json Data/data.json
dotnet run -- repl csv  Data/all.csv
dotnet run -- repl csv  Data/csv_dir
```


- Автосейв включён по умолчанию (файл `Data/autosave.yaml`). Путь задаётся в `Program.cs`.

---

## Форматы данных (I/O)

### YAML
```yaml
accounts:
  - id: 1
    name: Основной
    balance: 1000
categories:

  - id: 1
    type: income
    name: Зарплата
  - id: 2
    type: expense
    name: Продукты

operations:

  - id: 1
    type: income
    bank_account_id: 1
    category_id: 1
    amount: 2000
    date: 2025-11-01
    description: Зарплата
```

### JSON
```json
{
  "accounts": [
    { "id": 1, "name": "Основной", "balance": 1000.0 }
  ],
  "categories": [
    { "id": 1, "type": "income",  "name": "Зарплата" },
    { "id": 2, "type": "expense", "name": "Продукты" }
  ],
  "operations": [
    { "id": 1, "type": "income", "bank_account_id": 1, "category_id": 1, "amount": 2000.0, "date": "2025-11-01", "description": "Зарплата" }
  ]
}
```

### CSV (вариант 1: папка с 3 файлами)
`accounts.csv`
```
id;name;balance
1;Основной;1000
```
`categories.csv`
```
id;type;name
1;income;Зарплата
2;expense;Продукты
```
`operations.csv`
```
id;type;bank_account_id;category_id;amount;date;description
1;income;1;1;2000;2025-11-01;Зарплата
```

### CSV (вариант 2: единый файл с секциями)
```
#%table:accounts
id;name;balance
1;Основной;1000
#%table:categories
id;type;name
1;income;Зарплата
2;expense;Продукты
#%table:operations
id;type;bank_account_id;category_id;amount;date;description
1;income;1;1;2000;2025-11-01;Зарплата
```

> Поддерживаются разделители `, ; \t |` и строка `sep=;` в начале секции. Юникод и кавычки по RFC-стилю: `""` внутри `"`.

---

## Командный интерфейс (REPL)

Запуск: `dotnet run -- repl <json|yaml|csv> <путь>`

Команды:
```
help | quit | exit

Просмотр:
  list accounts
  list categories
  list operations [--acc ID] [--cat ID] [--from DATE] [--to DATE] [--max N] [--contains TEXT]

Аналитика:
  report categories
  report monthly [--year YYYY] [--acc ID] [--cat ID]
  report top [--n N] [--from YYYY-MM-DD] [--to YYYY-MM-DD]

Сверка:
  reconcile
  reconcile --apply

Экспорт:
  export json <dir> (from json|csv|yaml <src>)  # конвертация: читать из <src>
  export yaml <dir> (from json|csv|yaml <src>)
  export csv  <dir> (from json|csv|yaml <src>)
  export json <dir>                             # экспорт текущего состояния (без from)
  export yaml <dir>
  export csv  <dir>

Редактирование:
  create account <id> <name> <balance>
  edit   account <id> [--name "Имя"] [--balance 123.45]
  delete account <id>

  create category <id> <income|expense> <name>
  edit   category <id> [--type income|expense] [--name "Имя"]
  delete category <id>

  create operation <id> <income|expense> <acc_id> <cat_id> <amount> <date> [description]
  edit   operation <id> [--type T] [--acc ID] [--cat ID] [--amount X] [--date DATE] [--desc "text"]
  delete operation <id>
```

Примеры:
```
list operations --acc 1 --from 2025-11-01 --to 2025-11-30
report categories
report monthly --year 2025
report top --n 5 --from 2025-11-01 --to 2025-11-30
reconcile
reconcile --apply
export yaml ./out
export json ./out from csv ./Data/all.csv
```

---

## Архитектура проекта

Директории верхнего уровня (ключевые файлы):
```
App/
  Cli.cs                — маршрутизация и REPL
  TablePrinter.cs       — печать таблиц

Commands/               — команды (паттерн Команда)
  *.cs
  Decorators/TimedCommand.cs  — Декоратор: измерение времени выполнения

Domain/
  BankAccount.cs, Category.cs, Operations.cs, Types.cs
  DomainFactory.cs      — Фабрика доменных объектов

Exporters/
  BaseExporter.cs
  JsonExporter.cs, YamlExporter.cs, CsvExporter.cs
  Visitors/             — Посетитель для описания формата вывода
    IFinanceVisitor.cs
    JsonExportVisitor.cs
    YamlExportVisitor.cs
    CsvExportVisitor.cs

Facade/
  AnalyticsFacade.cs    — Фасад аналитики

Importers/
  BaseImporter.cs       — Шаблонный метод
  JsonImporter.cs, YamlImporter.cs, CsvImporter.cs
  ImportTarget.cs
  CsvSources/           — Адаптер источников CSV
    ICsvSource.cs
    FolderCsvSource.cs
    SectionedCsvSource.cs

Repository/
  IRepository.cs, IBankAccountRepository.cs, ICategoryRepository.cs, IOperationRepository.cs
  InMemory/             — In-memory реализации
    InMemoryBankAccountRepository.cs
    InMemoryCategoryRepository.cs
    InMemoryOperationRepository.cs
  Proxy/                — Прокси с автоснапшотом
    FileBackedBankAccountRepositoryProxy.cs
    FileBackedCategoryRepositoryProxy.cs
    FileBackedOperationRepositoryProxy.cs

Services/
  ISnapshotWriter.cs, FileSnapshotWriter.cs

Program.cs              — DI-контейнер и запуск
```

Связи по слоям:
- **Domain** ни от кого не зависит.
- **Repository** зависит от **Domain**.
- **Importers/Exporters/Facade/Commands** зависят от **Repository** и **Domain**.
- **App/Program** связывает всё через **DI**.

---

## Паттерны проектирования (GoF)

1) **Фасад (Facade)** — `Facade/AnalyticsFacade.cs`  
   Единая точка для аналитических расчётов (агрегация доходов/расходов, помесячные отчёты, сводки). Скрывает детали репозиториев и доменных операций от слоя команд.

2) **Команда (Command)** — `Commands/*`, регистрация/маршрутизация в `App/Cli.cs`  
   Каждая CLI-команда оформлена как объект, имеет единый интерфейс выполнения. Упрощает добавление новых сценариев и декорирование.

3) **Декоратор (Decorator)** — `Commands/Decorators/TimedCommand.cs`  
   Прозрачно измеряет и выводит время выполнения любой команды, не меняя её код.

4) **Шаблонный метод (Template Method)** — `Importers/BaseImporter.cs` + `Json/Yaml/CsvImporter.cs`  
   В базовом классе зафиксирован «каркас» импорта (создание `ImportTarget`, пост-обработка), в наследниках меняется только парсинг формата.

5) **Посетитель (Visitor)** — `Exporters/Visitors/*` + конкретные экспортёры  
   Разделяет структуру модели и формат вывода. Легко добавить новый формат: написать Visitor + тонкий Exporter.

6) **Фабрика (Factory)** — `Domain/DomainFactory.cs`  
   Централизованное создание доменных объектов с валидацией и нормализацией входных данных.

7) **Прокси (Proxy)** — `Repository/Proxy/*` + `Services/FileSnapshotWriter.cs`  
   Оборачивает in-memory репозитории: после любой мутации вызывается `Save()` → актуальное состояние пишется в файл (YAML). Даёт «квази-персистентность» без изменения кода клиентов.

8) **Адаптер (Adapter)** — `Importers/CsvSources/{ICsvSource,FolderCsvSource,SectionedCsvSource}.cs`  
   Приводит *разные источники CSV* (папка/секции) к единому виду «три текстовых блока + разделитель». `CsvImporter` больше не знает, откуда пришли данные — только как их парсить.

---

## SOLID и GRASP — как применены

- **S (Single Responsibility)**: импортеры/экспортеры разделены по форматам; `AnalyticsFacade` отделён от команд и репозиториев; таблицы печатает `TablePrinter`.
- **O (Open-Closed)**: добавить новый формат — реализовать новый `Importer/Exporter` (или Visitor) без правок существующего кода.
- **L (Liskov)**: `IRepository<T>` и его реализации (InMemory/Proxy) взаимозаменяемы.
- **I (Interface Segregation)**: узкие интерфейсы (`IBankAccountRepository`, etc.) вместо «толстых».
- **D (Dependency Inversion)**: зависимости внедряются через DI в `Program.cs`.

GRASP:  
- **Controller** — `Cli` обрабатывает команды пользователя;  
- **Low Coupling / High Cohesion** — разделение на слои и роли;  
- **Creator** — `DomainFactory` владеет созданием доменных сущностей.

---

## Диаграмма зависимостей по слоям

```
[App/Program, Cli] 
      │
      ▼
[Commands] ──▶ [Facade] ──▶ [Repository (Interfaces)]
      │                        │
      │                        ├──▶ [InMemory]
      │                        └──▶ [Proxies] ──▶ [Services/Snapshot]
      │
      ├──▶ [Importers] ──▶ [Domain]
      └──▶ [Exporters] ──▶ [Domain]
```

---

## Кодстайл, локализация и UX

- Сообщения об ошибках и `help` — **на русском**, понятные и единообразные.
- Табличный вывод — через `TablePrinter` (равные столбцы, выравнивания, формат чисел/дат).
- Экспорт **сохраняет порядок** вставки (как на входе).  
- JSON/YAML — **snake_case**, строки без лишних кавычек (plain scalars, где безопасно).

---

## Сценарии проверки (smoke)

1) **Импорт → просмотр → отчёт → экспорт**  
   ```bash
   dotnet run -- repl yaml ./Data/data.yaml
   list accounts
   list operations --from 2025-11-01 --to 2025-11-30
   report categories
   export json ./Out
   ```

2) **CSV (секции) vs CSV (папка)** — идентичный результат  
   ```bash
   dotnet run -- repl csv ./Data/all.csv
   # ... команды list/report ...
   dotnet run -- repl csv ./Data/csv_dir
   # вручную сравнить вывод
   ```

3) **Сверка баланса**  
   ```bash
   reconcile
   reconcile --apply
   export yaml ./Out
   ```

4) **CRUD**  
   ```bash
   create account 3 "Карта" 1200
   edit   account 3 --name "Карта Tinkoff" --balance 1500
   delete account 3
   ```

---

## Известные ограничения

- YAML-писатель старается избегать кавычек, но YAML-спецификация требует их для строк с особыми символами (например, двоеточие в конце, ведущие/хвостовые пробелы и т.п.). Это корректное поведение.  
- Валидаторы CSV минимальны (обязательные колонки, парсинг чисел/дат). При необходимости легко усилить проверку.

---

## DI-Регистрация (отрывок из Program.cs)

- InMemory-репозитории регистрируются как `Singleton`.
- Прокси-репозитории для интерфейсов (`IBankAccountRepository` и т.д.) оборачивают InMemory и вызывают `ISnapshotWriter.Save()` после мутаций.
- Экспортер по умолчанию — `YamlExporter` (для снапшотов).

```csharp
services.AddSingleton<InMemoryBankAccountRepository>();
services.AddSingleton<InMemoryCategoryRepository>();
services.AddSingleton<InMemoryOperationRepository>();

services.AddSingleton<BaseExporter, YamlExporter>();
services.AddSingleton<ISnapshotWriter>(sp =>
    new FileSnapshotWriter("Data/autosave.yaml",
        sp.GetRequiredService<InMemoryBankAccountRepository>(),
        sp.GetRequiredService<InMemoryCategoryRepository>(),
        sp.GetRequiredService<InMemoryOperationRepository>(),
        sp.GetRequiredService<BaseExporter>()));

services.AddSingleton<IBankAccountRepository>(sp =>
    new FileBackedBankAccountRepositoryProxy(
        sp.GetRequiredService<InMemoryBankAccountRepository>(),
        sp.GetRequiredService<ISnapshotWriter>()));
// ... аналогично для ICategoryRepository / IOperationRepository

services.AddSingleton<AnalyticsFacade>();
services.AddSingleton<TablePrinter>();
services.AddSingleton<Cli>();
```
