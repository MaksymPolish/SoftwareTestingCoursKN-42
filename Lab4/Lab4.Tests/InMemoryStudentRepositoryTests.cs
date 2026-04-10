using Lab4.Data;
using Lab4.Data.Entities;
using Lab4.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Lab4.Tests;

public class InMemoryStudentRepositoryTests
{
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
        var saved = await context.Students.FirstOrDefaultAsync(s => s.Email == "john@example.com");
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
        await context.SaveChangesAsync();

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
        var student1 = new Student
        {
            FullName = "Alice",
            Email = "alice@example.com",
            EnrollmentDate = DateTime.UtcNow
        };
        var student2 = new Student
        {
            FullName = "Bob",
            Email = "bob@example.com",
            EnrollmentDate = DateTime.UtcNow
        };
        context.Students.AddRange(student1, student2);
        await context.SaveChangesAsync();

        var repo = new StudentRepository(context);

        // Act
        var result = await repo.GetAllAsync();

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(s => s.FullName == "Alice");
        result.ShouldContain(s => s.FullName == "Bob");
    }

    [Fact]
    public async Task UpdateAsync_ModifiesStudentAsync()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var student = new Student
        {
            FullName = "Original Name",
            Email = "update@example.com",
            EnrollmentDate = DateTime.UtcNow
        };
        context.Students.Add(student);
        await context.SaveChangesAsync();

        var repo = new StudentRepository(context);
        student.FullName = "Updated Name";

        // Act
        await repo.UpdateAsync(student);

        // Assert
        var updated = await context.Students.FindAsync(student.Id);
        updated.FullName.ShouldBe("Updated Name");
    }

    [Fact]
    public async Task DeleteAsync_RemovesStudentAsync()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var student = new Student
        {
            FullName = "To Delete",
            Email = "delete@example.com",
            EnrollmentDate = DateTime.UtcNow
        };
        context.Students.Add(student);
        await context.SaveChangesAsync();

        var repo = new StudentRepository(context);

        // Act
        await repo.DeleteAsync(student.Id);

        // Assert
        var deleted = await context.Students.FindAsync(student.Id);
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
            Email = "alice@test.com",
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
            Email = "bob@test.com",
            EnrollmentDate = DateTime.UtcNow,
            Enrollments = new List<Enrollment>
            {
                new Enrollment { Course = course1, Grade = 90 },
                new Enrollment { Course = course2, Grade = 95 }
            }
        };

        context.Students.AddRange(studentA, studentB);
        await context.SaveChangesAsync();

        var repo = new StudentRepository(context);

        // Act
        var top = await repo.GetTopStudentsAsync(1);

        // Assert
        top.Count.ShouldBe(1);
        top.First().FullName.ShouldBe("Bob"); // avg 92.5 > avg 75
    }
}
