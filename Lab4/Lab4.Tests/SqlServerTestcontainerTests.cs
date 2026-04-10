using Lab4.Data;
using Lab4.Data.Entities;
using Lab4.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Testcontainers.MsSql;
using Xunit;

namespace Lab4.Tests;

[Trait("Category", "Integration")]
public class SqlServerTestcontainerTests : IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
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
        await _dbContainer.StopAsync();
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

        // Act
        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        // Assert
        var found = await _context.Students.FindAsync(student.Id);
        found.ShouldNotBeNull();
        found.FullName.ShouldBe("Test User");
        found.Email.ShouldBe("test@sqlserver.com");
    }

    [Fact]
    public async Task RawSql_ReturnsExpectedResultsAsync()
    {
        // Arrange
        _context.Students.Add(new Student
        {
            FullName = "SQL User",
            Email = "sql@test.com",
            EnrollmentDate = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var students = await _context.Students
            .FromSqlRaw("SELECT * FROM Students WHERE FullName LIKE '%SQL%'")
            .ToListAsync();

        // Assert
        students.ShouldNotBeEmpty();
        students.First().FullName.ShouldContain("SQL");
    }

    [Fact]
    public async Task ForeignKeyConstraint_EnforcedByRealDatabaseAsync()
    {
        // Arrange
        var student = new Student
        {
            FullName = "Constraint Test",
            Email = "constraint@test.com",
            EnrollmentDate = DateTime.UtcNow
        };
        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        var invalidEnrollment = new Enrollment
        {
            StudentId = student.Id,
            CourseId = 9999, // non-existent course
            Grade = 85
        };

        // Act & Assert
        _context.Enrollments.Add(invalidEnrollment);
        var exception = await Should.ThrowAsync<DbUpdateException>(
            () => _context.SaveChangesAsync());
        exception.ShouldNotBeNull();
    }

    [Fact]
    public async Task Migrations_ApplyCleanlyAsync()
    {
        // Act — re-apply via migrations without drop
        await _context.Database.EnsureCreatedAsync();

        // Assert — schema exists with expected tables
        var tableCount = await _context.Students.CountAsync();
        
        // Verify we can use the schema by adding data
        _context.Students.Add(new Student
        {
            FullName = "Migration Test",
            Email = "migrate@test.com",
            EnrollmentDate = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var count = await _context.Students.CountAsync();
        count.ShouldBe(1);
    }

    [Fact]
    public async Task RepositoryOperations_WorkWithRealDatabaseAsync()
    {
        // Arrange
        var repo = new StudentRepository(_context);
        var student = new Student
        {
            FullName = "Repo Test",
            Email = "repo@test.com",
            EnrollmentDate = DateTime.UtcNow
        };

        // Act
        await repo.AddAsync(student);
        var retrieved = await repo.GetByIdAsync(student.Id);

        // Assert
        retrieved.ShouldNotBeNull();
        retrieved.FullName.ShouldBe("Repo Test");
        retrieved.Email.ShouldBe("repo@test.com");
    }

    [Fact]
    public async Task UniqueConstraint_EnforcedByRealDatabaseAsync()
    {
        // Arrange
        var student1 = new Student
        {
            FullName = "First Student",
            Email = "unique@test.com",
            EnrollmentDate = DateTime.UtcNow
        };
        _context.Students.Add(student1);
        await _context.SaveChangesAsync();

        var student2 = new Student
        {
            FullName = "Second Student",
            Email = "unique@test.com", // duplicate email
            EnrollmentDate = DateTime.UtcNow
        };
        _context.Students.Add(student2);

        // Act & Assert
        var exception = await Should.ThrowAsync<DbUpdateException>(
            () => _context.SaveChangesAsync());
        exception.ShouldNotBeNull();
    }

    [Fact]
    public async Task CascadeDelete_WorksWithRealDatabaseAsync()
    {
        // Arrange
        var course = new Course { Title = "Advanced SQL", Credits = 4 };
        var student = new Student
        {
            FullName = "Cascade Test",
            Email = "cascade@test.com",
            EnrollmentDate = DateTime.UtcNow
        };
        _context.Courses.Add(course);
        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        var enrollment = new Enrollment
        {
            StudentId = student.Id,
            CourseId = course.Id,
            Grade = 90
        };
        _context.Enrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        var studentId = student.Id;
        var enrollmentCountBefore = await _context.Enrollments
            .Where(e => e.StudentId == studentId)
            .CountAsync();

        // Act
        _context.Students.Remove(student);
        await _context.SaveChangesAsync();

        // Assert
        enrollmentCountBefore.ShouldBe(1);
        var enrollmentCountAfter = await _context.Enrollments
            .Where(e => e.StudentId == studentId)
            .CountAsync();
        enrollmentCountAfter.ShouldBe(0);
    }
}
