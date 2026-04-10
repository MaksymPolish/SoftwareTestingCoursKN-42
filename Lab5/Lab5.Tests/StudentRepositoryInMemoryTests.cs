using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Sdk;
using Lab5.Data;
using Lab5.Data.Models;
using Lab5.Data.Repositories;
using Shouldly;

namespace Lab5.Tests.InMemory;

/// <summary>
/// Task 1: Tests for StudentRepository using InMemory database provider
/// </summary>
public class StudentRepositoryInMemoryTests
{
    /// <summary>
    /// Creates an InMemory DbContext with a unique database name for test isolation
    /// </summary>
    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        return context;
    }

    [Fact]
    public async Task AddAsync_ValidStudent_SavesSuccessfullyAsync()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repo = new StudentRepository(context);
        var student = new Student
        {
            FullName = "John Doe",
            Email = "john@example.com",
            EnrollmentDate = DateTime.UtcNow
        };

        // Act
        await repo.AddAsync(student);

        // Assert
        var saved = await context.Students.FirstOrDefaultAsync(s => s.Email == "john@example.com", TestContext.Current.CancellationToken);
        saved.ShouldNotBeNull();
        saved.FullName.ShouldBe("John Doe");
        saved.Id.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task GetByIdAsync_IncludesEnrollmentsAsync()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var course = new Course { Title = "Testing 101", Credits = 3 };
        var student = new Student
        {
            FullName = "Jane Smith",
            Email = "jane@example.com",
            EnrollmentDate = DateTime.UtcNow,
            Enrollments = new List<Enrollment>
            {
                new Enrollment { Course = course, Grade = 95 }
            }
        };
        context.Students.Add(student);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repo = new StudentRepository(context);

        // Act
        var result = await repo.GetByIdAsync(student.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Enrollments.ShouldNotBeNull();
        result.Enrollments.Count.ShouldBe(1);
        result.Enrollments.First().Grade.ShouldBe(95m);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllStudentsAsync()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var student1 = new Student { FullName = "Alice", Email = "alice@test.com", EnrollmentDate = DateTime.UtcNow };
        var student2 = new Student { FullName = "Bob", Email = "bob@test.com", EnrollmentDate = DateTime.UtcNow };
        var student3 = new Student { FullName = "Charlie", Email = "charlie@test.com", EnrollmentDate = DateTime.UtcNow };

        context.Students.AddRange(student1, student2, student3);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repo = new StudentRepository(context);

        // Act
        var result = await repo.GetAllAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        result.Any(s => s.FullName == "Alice").ShouldBeTrue();
        result.Any(s => s.FullName == "Bob").ShouldBeTrue();
        result.Any(s => s.FullName == "Charlie").ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ModifiedStudent_SavesChangesAsync()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var student = new Student
        {
            FullName = "Original Name",
            Email = "update@test.com",
            EnrollmentDate = DateTime.UtcNow
        };
        context.Students.Add(student);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repo = new StudentRepository(context);
        student.FullName = "Updated Name";

        // Act
        await repo.UpdateAsync(student);

        // Assert
        var updated = await context.Students.FindAsync(new object[] { student.Id }, cancellationToken: TestContext.Current.CancellationToken);
        updated.ShouldNotBeNull();
        updated.FullName.ShouldBe("Updated Name");
    }

    [Fact]
    public async Task DeleteAsync_ExistingStudent_RemovesStudentAsync()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var student = new Student
        {
            FullName = "To Delete",
            Email = "delete@test.com",
            EnrollmentDate = DateTime.UtcNow
        };
        context.Students.Add(student);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var studentId = student.Id;

        var repo = new StudentRepository(context);

        // Act
        await repo.DeleteAsync(studentId);

        // Assert
        var deleted = await context.Students.FindAsync(new object[] { studentId }, cancellationToken: TestContext.Current.CancellationToken);
        deleted.ShouldBeNull();
    }

    [Fact]
    public async Task GetTopStudentsAsync_ReturnsOrderedByAverageGradeAsync()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var course1 = new Course { Title = "Math", Credits = 4 };
        var course2 = new Course { Title = "Science", Credits = 3 };

        var studentA = new Student
        {
            FullName = "Alice",
            Email = "alice@top.com",
            EnrollmentDate = DateTime.UtcNow,
            Enrollments = new List<Enrollment>
            {
                new Enrollment { Course = course1, Grade = 70 },
                new Enrollment { Course = course2, Grade = 80 }
            }
        };
        var studentB = new Student
        {
            FullName = "Bob",
            Email = "bob@top.com",
            EnrollmentDate = DateTime.UtcNow,
            Enrollments = new List<Enrollment>
            {
                new Enrollment { Course = course1, Grade = 90 },
                new Enrollment { Course = course2, Grade = 95 }
            }
        };

        context.Students.AddRange(studentA, studentB);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repo = new StudentRepository(context);

        // Act
        var top = await repo.GetTopStudentsAsync(1);

        // Assert
        top.Count.ShouldBe(1);
        top.First().FullName.ShouldBe("Bob"); // avg 92.5 > avg 75
    }

    [Fact]
    public async Task GetTopStudentsAsync_MultipleStudents_ReturnsCorrectOrderAsync()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var course1 = new Course { Title = "History", Credits = 3 };
        var course2 = new Course { Title = "Geography", Credits = 3 };
        var course3 = new Course { Title = "Literature", Credits = 4 };

        var studentA = new Student
        {
            FullName = "Anna",
            Email = "anna@test.com",
            EnrollmentDate = DateTime.UtcNow,
            Enrollments = new List<Enrollment>
            {
                new Enrollment { Course = course1, Grade = 60 }
            }
        };
        var studentB = new Student
        {
            FullName = "Boris",
            Email = "boris@test.com",
            EnrollmentDate = DateTime.UtcNow,
            Enrollments = new List<Enrollment>
            {
                new Enrollment { Course = course2, Grade = 80 }
            }
        };
        var studentC = new Student
        {
            FullName = "Catherine",
            Email = "catherine@test.com",
            EnrollmentDate = DateTime.UtcNow,
            Enrollments = new List<Enrollment>
            {
                new Enrollment { Course = course3, Grade = 95 }
            }
        };

        context.Students.AddRange(studentA, studentB, studentC);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repo = new StudentRepository(context);

        // Act
        var top2 = await repo.GetTopStudentsAsync(2);

        // Assert
        top2.Count.ShouldBe(2);
        top2[0].FullName.ShouldBe("Catherine"); // avg 95
        top2[1].FullName.ShouldBe("Boris");     // avg 80
    }
}
