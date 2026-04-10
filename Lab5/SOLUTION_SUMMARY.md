# Lab5 — Тестування бази даних Entity Framework Core

## Виконані завдання

### ✅ Завдання 1: InMemory тести репозиторію

**Файл:** `Lab5.Tests/StudentRepositoryInMemoryTests.cs`

**Кількість тестів:** 7 (вимога: 6+)

Тести охоплюють:
1. ✅ `AddAsync_ValidStudent_SavesSuccessfullyAsync` - Додавання студента
2. ✅ `GetByIdAsync_IncludesEnrollmentsAsync` - Отримання з включеними реєстраціями
3. ✅ `GetAllAsync_ReturnsAllStudentsAsync` - Отримання всіх студентів
4. ✅ `UpdateAsync_ModifiedStudent_SavesChangesAsync` - Оновлення студента
5. ✅ `DeleteAsync_ExistingStudent_RemovesStudentAsync` - Видалення студента
6. ✅ `GetTopStudentsAsync_ReturnsOrderedByAverageGradeAsync` - Топ студентів (1)
7. ✅ `GetTopStudentsAsync_MultipleStudents_ReturnsCorrectOrderAsync` - Топ студентів (2)

**Особливості:**
- Використовує `Guid.NewGuid().ToString()` для унікальної ізоляції кожного тесту
- Тестує навігаційні властивості та Include()
- Перевіряє впорядкування за середньою оцінкою

---

### ✅ Завдання 2: SQLite реляційні тести

**Файл:** `Lab5.Tests/StudentRepositoryRelationalTests.cs`

**Кількість тестів:** 7 (вимога: 5+)

Тести охоплюють:
1. ✅ `ForeignKey_EnrollingInNonExistingCourse_ThrowsDbUpdateExceptionAsync` - Обмеження FK (курс)
2. ✅ `ForeignKey_StudentDoesNotExist_ThrowsDbUpdateExceptionAsync` - Обмеження FK (студент)
3. ✅ `UniqueConstraint_DuplicateEmail_ThrowsDbUpdateExceptionAsync` - Унікальне обмеження Email
4. ✅ `CascadeDelete_DeletingStudent_RemovesEnrollmentsAsync` - Каскадне видалення (студент)
5. ✅ `CascadeDelete_DeletingCourse_RemovesEnrollmentsAsync` - Каскадне видалення (курс)
6. ✅ `Transactions_RollbackOnException_UndoesChangesAsync` - Транзакції та Rollback
7. ✅ `ComparisonWithInMemory_ConstraintsEnforced_DifferentBehaviorAsync` - Порівняння провайдерів
8. ✅ `MultipleEnrollments_SameStudentMultipleCourses_WorksCorrectlyAsync` - Кілька реєстрацій

**Особливості:**
- SQLite в режимі пам'яті (`:memory:`)
- Реальне забезпечення FK, унікальних обмежень, каскадного видалення
- Документування відмінностей: **InMemory** (без FK) vs **SQLite** (з FK)

---

### ✅ Завдання 3: Testcontainers SQL Server тести

**Файл:** `Lab5.Tests/StudentRepositorySqlServerTests.cs`

**Кількість тестів:** 8 (вимога: 4+)

Тести охоплюють:
1. ✅ `CrudOperations_WorkWithRealSqlServerAsync` - Повний CRUD цикл
2. ✅ `RawSql_ReturnsExpectedResultsAsync` - Сирі SQL запити
3. ✅ `Migrations_ApplyCleanlyAsync` - Міграції застосовуються чисто
4. ✅ `ForeignKeyConstraints_EnforcedInSqlServerAsync` - FK обмеження
5. ✅ `UniqueConstraints_EnforcedInSqlServerAsync` - Унікальні обмеження
6. ✅ `CascadeDelete_WorksInSqlServerAsync` - Каскадне видалення
7. ✅ `ParameterizedQueries_PreventSqlInjectionAsync` - Захист від SQL-інекцій
8. ✅ `ComplexQuery_WithMultipleJoins_WorksAsync` - Складні запити з JOIN
9. ✅ `ProviderBehaviorComparison_SqlServer_vs_SqliteAsync` - Порівняння провайдерів

**Особливості:**
- Використовує `IAsyncLifetime` для управління контейнером
- Docker контейнер SQL Server 2022
- Тестування реального провайдера БД
- Повна документація відмінностей SQL Server vs SQLite

---

## Структура проекту

```
Lab5/
├── Lab5.sln
├── Lab5.Data/
│   ├── AppDbContext.cs
│   ├── Models/
│   │   ├── Student.cs
│   │   ├── Course.cs
│   │   └── Enrollment.cs
│   ├── Repositories/
│   │   └── StudentRepository.cs
│   └── Lab5.Data.csproj
└── Lab5.Tests/
    ├── StudentRepositoryInMemoryTests.cs
    ├── StudentRepositoryRelationalTests.cs
    ├── StudentRepositorySqlServerTests.cs
    └── Lab5.Tests.csproj
```

---

## Залежності

### Lab5.Data
- Microsoft.EntityFrameworkCore (10.0.5)
- Microsoft.EntityFrameworkCore.Sqlite (10.0.5)

### Lab5.Tests
- xunit.v3 (3.2.2)
- Microsoft.NET.Test.Sdk (18.4.0)
- Microsoft.EntityFrameworkCore.InMemory (10.0.5)
- Microsoft.EntityFrameworkCore.Sqlite (10.0.5)
- Microsoft.EntityFrameworkCore.SqlServer (10.0.5)
- Testcontainers.MsSql (3.8.0)
- Shouldly (4.3.0)

---

## Сутності та зв'язки

### Student
- `Id` (PK)
- `FullName` (обов'язкове, max 200 символів)
- `Email` (обов'язкове, max 256, **UNIQUE**)
- `EnrollmentDate`
- Зв'язок: 1 → Many Enrollment (Cascade Delete)

### Course
- `Id` (PK)
- `Title` (обов'язкове, max 200)
- `Credits` (обов'язкове)
- Зв'язок: 1 → Many Enrollment (Cascade Delete)

### Enrollment
- `Id` (PK)
- `StudentId` (FK → Student, обов'язкове)
- `CourseId` (FK → Course, обов'язкове)
- `Grade` (опціональне, decimal(5,2))
- Зв'язки: Many → 1 Student, Many → 1 Course

---

## StudentRepository методи

```csharp
Task<Student?> GetByIdAsync(int id)        // Include Enrollments
Task<List<Student>> GetAllAsync()           // Усі студенти
Task AddAsync(Student student)               // Додати студента
Task UpdateAsync(Student student)            // Оновити студента
Task DeleteAsync(int id)                     // Видалити за ID
Task<List<Student>> GetTopStudentsAsync(int count) // Top N за середньою оцінкою
```

---

## Ключові особливості тестування

### InMemory (Task 1)
- ✅ Найшвидште тестування
- ❌ Без FK обмежень
- ❌ Без обмежень унікальності
- ❌ Каскадне видалення - вручну

### SQLite (Task 2)
- ✅ Реляційні обмеження (FK, UNIQUE)
- ✅ Каскадне видалення автоматичне
- ✅ Дешево/швидко
- ✅ Режим пам'яті (`:memory:`)
- ❌ Обмежені можливості провайдера

### SQL Server (Task 3)
- ✅ Реальна БД
- ✅ Усі функції провайдера
- ✅ Сирий SQL, збережені процедури
- ✅ Повна сумісність
- ❌ Повільніше (контейнер)
- ❌ Вимагає Docker

---

## Запуск тестів

```bash
# Усі тести (включаючи Testcontainers - потребує Docker)
dotnet test Lab5.Tests

# Тільки InMemory
dotnet test Lab5.Tests --filter "FullyQualifiedName~InMemory"

# Тільки SQLite (Relational)
dotnet test Lab5.Tests --filter "FullyQualifiedName~Relational"

# Тільки SQL Server (потребує Docker)
dotnet test Lab5.Tests --filter "Category=Integration"
```

---

## Порівняння провайдерів

| Поведінка | InMemory | SQLite | SQL Server |
|---|:---:|:---:|:---:|
| FK обмеження | ❌ | ✅ | ✅ |
| UNIQUE обмеження | ❌ | ✅ | ✅ |
| CASCADE DELETE | ❌ | ✅ | ✅ |
| Транзакції | ❌ | ✅ | ✅ |
| Сирий SQL | ❌ | ⚠️ | ✅ |
| Швидкість | ⚡⚡⚡ | ⚡⚡ | ⚡ |
| Реалізм | ❌ | ⚠️ | ✅ |

---

## Висновки

1. **InMemory** - для швидких unit-тестів логіки без БД
2. **SQLite** - для інтеграційних тестів реляційних обмежень
3. **SQL Server (Testcontainers)** - для ІІ тестування з реальною БД

Всі три підходи мають місце в стратегії тестування Entity Framework Core!
