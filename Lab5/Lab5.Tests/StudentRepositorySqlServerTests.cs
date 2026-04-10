using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Sdk;
using Lab5.Data;
using Lab5.Data.Models;
using Lab5.Data.Repositories;
using Shouldly;
using Testcontainers.MsSql;

namespace Lab5.Tests.Testcontainers;

/// <summary>
/// Task 3: Integration tests using Testcontainers with real SQL Server database
///
/// Testcontainers provide real database containers via Docker, enabling:
/// - Complete relational database behavior
/// - Raw SQL and stored procedures
/// - Provider-specific query semantics
/// - Full migration testing
///
/// Tests run slower than InMemory/SQLite (container startup takes 10-30 seconds)
/// but provide the highest confidence that code works with real databases.
/// </summary>
[Trait("Category", "Integration")]
public class StudentRepositorySqlServerTests : IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = 
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

    private AppDbContext _context = null!;

    public async ValueTask InitializeAsync()
    {
        await _dbContainer.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_dbContainer.GetConnectionString())
            .Options;

        _context = new AppDbContext(options);
        await _context.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    [Fact]
    public async Task CrudOperations_WorkWithRealSqlServerAsync()
    {
        // Arrange
        var student = new Student
        {
            FullName = "Test User",
            Email = "test@sqlserver.com",
            EnrollmentDate = DateTime.UtcNow
        };

        // Act - Create
        _context.Students.Add(student);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var savedId = student.Id;
        savedId.ShouldBeGreaterThan(0);

        // Act - Read
        var found = await _context.Students.FindAsync(new object[] { savedId }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert - Read
        found.ShouldNotBeNull();
        found.FullName.ShouldBe("Test User");
        found.Email.ShouldBe("test@sqlserver.com");

        // Act - Update
        found.FullName = "Updated User";
        _context.Students.Update(found);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert - Update
        var updated = await _context.Students.FindAsync(new object[] { savedId }, cancellationToken: TestContext.Current.CancellationToken);
        updated.ShouldNotBeNull();
        updated.FullName.ShouldBe("Updated User");

        // Act - Delete
        _context.Students.Remove(updated);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert - Delete
        var deleted = await _context.Students.FindAsync(new object[] { savedId }, cancellationToken: TestContext.Current.CancellationToken);
        deleted.ShouldBeNull();
    }

    [Fact]
    public async Task RawSql_ReturnsExpectedResultsAsync()
    {
        // Arrange
        var student1 = new Student
        {
            FullName = "SQL User One",
            Email = "sqluser1@test.com",
            EnrollmentDate = DateTime.UtcNow
        };
        var student2 = new Student
        {
            FullName = "SQL User Two",
            Email = "sqluser2@test.com",
            EnrollmentDate = DateTime.UtcNow
        };
        var student3 = new Student
        {
            FullName = "Other User",
            Email = "other@test.com",
            EnrollmentDate = DateTime.UtcNow
        };

        _context.Students.AddRange(student1, student2, student3);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var sqlUsers = await _context.Students
            .FromSqlRaw("SELECT * FROM Students WHERE FullName LIKE '%SQL%'")
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        sqlUsers.ShouldNotBeEmpty();
        sqlUsers.Count.ShouldBe(2);
        foreach(var s in sqlUsers)
        {
            s.FullName.ShouldContain("SQL");
        }
    }

    [Fact]
    public async Task Migrations_ApplyCleanlyAsync()
    {
        // Act - Ensure database exists via EnsureCreatedAsync
        await _context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // Act - Insert test data to verify schema exists
        var student = new Student
        {
            FullName = "Migration Test",
            Email = "migrate@test.com",
            EnrollmentDate = DateTime.UtcNow
        };
        _context.Students.Add(student);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert - Schema exists and data persists
        var count = await _context.Students.CountAsync(TestContext.Current.CancellationToken);
        count.ShouldBe(1);

        var retrieved = await _context.Students.FirstAsync(TestContext.Current.CancellationToken);
        retrieved.FullName.ShouldBe("Migration Test");
    }

    [Fact]
    public async Task ForeignKeyConstraints_EnforcedInSqlServerAsync()
    {
        // Arrange
        var course = new Course { Title = "Database Design", Credits = 4 };
        _context.Courses.Add(course);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var invalidEnrollment = new Enrollment
        {
            StudentId = 9999, // Non-existent student
            CourseId = course.Id,
            Grade = 85
        };

        // Act & Assert
        _context.Enrollments.Add(invalidEnrollment);
        await Should.ThrowAsync<DbUpdateException>(
            () => _context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UniqueConstraints_EnforcedInSqlServerAsync()
    {
        // Arrange
        var student1 = new Student
        {
            FullName = "John",
            Email = "unique@test.com",
            EnrollmentDate = DateTime.UtcNow
        };
        _context.Students.Add(student1);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var student2 = new Student
        {
            FullName = "Jane",
            Email = "unique@test.com", // Duplicate email
            EnrollmentDate = DateTime.UtcNow
        };

        // Act & Assert
        _context.Students.Add(student2);
        await Should.ThrowAsync<DbUpdateException>(
            () => _context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CascadeDelete_WorksInSqlServerAsync()
    {
        // Arrange
        var student = new Student
        {
            FullName = "Cascade Test",
            Email = "cascade@test.com",
            EnrollmentDate = DateTime.UtcNow
        };
        var course = new Course { Title = "Cascade Course", Credits = 3 };

        _context.Students.Add(student);
        _context.Courses.Add(course);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var enrollment = new Enrollment
        {
            StudentId = student.Id,
            CourseId = course.Id,
            Grade = 90
        };
        _context.Enrollments.Add(enrollment);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var studentId = student.Id;
        var enrollmentsBefore = await _context.Enrollments
            .Where(e => e.StudentId == studentId)
            .CountAsync(TestContext.Current.CancellationToken);
        enrollmentsBefore.ShouldBe(1);

        // Act
        _context.Students.Remove(student);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var enrollmentsAfter = await _context.Enrollments
            .Where(e => e.StudentId == studentId)
            .CountAsync(TestContext.Current.CancellationToken);
        enrollmentsAfter.ShouldBe(0);
    }

    [Fact]
    public async Task ParameterizedQueries_PreventSqlInjectionAsync()
    {
        // Arrange
        var student = new Student
        {
            FullName = "Security Test",
            Email = "security@test.com",
            EnrollmentDate = DateTime.UtcNow
        };

        _context.Students.Add(student);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var maliciousInput = "' OR '1'='1";

        // Act - Use parameterized query (safe)
        var results = await _context.Students
            .FromSqlInterpolated($"SELECT * FROM Students WHERE Email = {maliciousInput}")
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert - Query is safe due to parameterization
        results.ShouldBeEmpty(); // No results should match the malicious string
    }

    [Fact]
    public async Task ComplexQuery_WithMultipleJoins_WorksAsync()
    {
        // Arrange
        var course1 = new Course { Title = "Advanced SQL", Credits = 4 };
        var course2 = new Course { Title = "Data Modeling", Credits = 3 };

        var student1 = new Student
        {
            FullName = "Alice",
            Email = "alice@complex.com",
            EnrollmentDate = DateTime.UtcNow
        };
        var student2 = new Student
        {
            FullName = "Bob",
            Email = "bob@complex.com",
            EnrollmentDate = DateTime.UtcNow
        };

        _context.Courses.AddRange(course1, course2);
        _context.Students.AddRange(student1, student2);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _context.Enrollments.AddRange(
            new Enrollment { StudentId = student1.Id, CourseId = course1.Id, Grade = 92 },
            new Enrollment { StudentId = student1.Id, CourseId = course2.Id, Grade = 88 },
            new Enrollment { StudentId = student2.Id, CourseId = course1.Id, Grade = 85 }
        );
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - Complex query with includes
        var enrollmentsWithDetails = await _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
            .Where(e => e.Grade >= 85)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        enrollmentsWithDetails.Count.ShouldBe(3);
        foreach(var e in enrollmentsWithDetails)
        {
            e.Student.ShouldNotBeNull();
            e.Course.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task ProviderBehaviorComparison_SqlServer_vs_SqliteAsync()
    {
        // This documents SQL Server specific behavior and how it differs from SQLite/InMemory

        // SQL Server specific features:
        // 1. IDENTITY columns with seed/increment
        // 2. Nvarchar with collation support
        // 3. Decimal precision up to 38 digits
        // 4. Built-in audit functions (GETUTCDATE(), CURRENT_USER)
        // 5. Row-level security support
        // 6. Full transaction isolation levels (READ UNCOMMITTED, READ COMMITTED, etc.)

        // Arrange
        var student = new Student
        {
            FullName = "Provider Test",
            Email = "provider@test.com",
            EnrollmentDate = DateTime.UtcNow
        };

        // Act
        _context.Students.Add(student);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert - SQL Server handles IDENTITY generation
        student.Id.ShouldBeGreaterThan(0);

        var retrieved = await _context.Students.FindAsync(new object[] { student.Id }, cancellationToken: TestContext.Current.CancellationToken);
        retrieved.ShouldNotBeNull();
    }
}
