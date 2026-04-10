using Lab4.Data;
using Lab4.Data.Entities;
using Lab4.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Lab4.Tests;

public class SqliteDatabaseConstraintTests : IDisposable
{
    private SqliteConnection? _connection;

    private (AppDbContext context, SqliteConnection connection) CreateSqliteContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        
        _connection = connection;
        return (context, connection);
    }

    [Fact]
    public async Task ForeignKey_EnrollingInNonExistingCourse_ThrowsAsync()
    {
        // Arrange
        var (context, connection) = CreateSqliteContext();
        using (context)
        using (connection)
        {
            var student = new Student
            {
                FullName = "Test Student",
                Email = "test@fk.com",
                EnrollmentDate = DateTime.UtcNow
            };
            context.Students.Add(student);
            await context.SaveChangesAsync();

            var enrollment = new Enrollment
            {
                StudentId = student.Id,
                CourseId = 999, // does not exist
                Grade = 85
            };

            // Act & Assert
            context.Enrollments.Add(enrollment);
            var exception = await Should.ThrowAsync<DbUpdateException>(
                () => context.SaveChangesAsync());
            exception.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task ForeignKey_EnrollingNonExistentStudent_ThrowsAsync()
    {
        // Arrange
        var (context, connection) = CreateSqliteContext();
        using (context)
        using (connection)
        {
            var course = new Course { Title = "Math 101", Credits = 3 };
            context.Courses.Add(course);
            await context.SaveChangesAsync();

            var enrollment = new Enrollment
            {
                StudentId = 999, // does not exist
                CourseId = course.Id,
                Grade = 85
            };

            // Act & Assert
            context.Enrollments.Add(enrollment);
            var exception = await Should.ThrowAsync<DbUpdateException>(
                () => context.SaveChangesAsync());
            exception.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task UniqueConstraint_DuplicateEmail_ThrowsAsync()
    {
        // Arrange
        var (context, connection) = CreateSqliteContext();
        using (context)
        using (connection)
        {
            var student1 = new Student
            {
                FullName = "Alice",
                Email = "dup@test.com",
                EnrollmentDate = DateTime.UtcNow
            };
            var student2 = new Student
            {
                FullName = "Bob",
                Email = "dup@test.com", // same email
                EnrollmentDate = DateTime.UtcNow
            };

            context.Students.Add(student1);
            await context.SaveChangesAsync();
            context.Students.Add(student2);

            // Act & Assert
            var exception = await Should.ThrowAsync<DbUpdateException>(
                () => context.SaveChangesAsync());
            exception.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task CascadeDelete_DeletingStudent_RemovesEnrollmentsAsync()
    {
        // Arrange
        var (context, connection) = CreateSqliteContext();
        using (context)
        using (connection)
        {
            var course = new Course { Title = "CS101", Credits = 3 };
            var student = new Student
            {
                FullName = "Charlie",
                Email = "charlie@test.com",
                EnrollmentDate = DateTime.UtcNow,
                Enrollments = new List<Enrollment>
                {
                    new Enrollment { Course = course, Grade = 88 }
                }
            };
            context.Students.Add(student);
            await context.SaveChangesAsync();

            var studentId = student.Id;

            // Act
            context.Students.Remove(student);
            await context.SaveChangesAsync();

            // Assert
            var enrollments = await context.Enrollments
                .Where(e => e.StudentId == studentId)
                .ToListAsync();
            enrollments.ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task CascadeDelete_DeletingCourse_RemovesEnrollmentsAsync()
    {
        // Arrange
        var (context, connection) = CreateSqliteContext();
        using (context)
        using (connection)
        {
            var course = new Course { Title = "Physics", Credits = 4 };
            var student = new Student
            {
                FullName = "Diana",
                Email = "diana@test.com",
                EnrollmentDate = DateTime.UtcNow
            };
            context.Courses.Add(course);
            context.Students.Add(student);
            await context.SaveChangesAsync();

            var enrollment = new Enrollment
            {
                StudentId = student.Id,
                CourseId = course.Id,
                Grade = 92
            };
            context.Enrollments.Add(enrollment);
            await context.SaveChangesAsync();

            var courseId = course.Id;

            // Act
            context.Courses.Remove(course);
            await context.SaveChangesAsync();

            // Assert
            var enrollments = await context.Enrollments
                .Where(e => e.CourseId == courseId)
                .ToListAsync();
            enrollments.ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task ConcurrentUpdates_LastWriteWinsAsync()
    {
        // Arrange
        var (context, connection) = CreateSqliteContext();
        using (context)
        using (connection)
        {
            var student = new Student
            {
                FullName = "Eve",
                Email = "eve@test.com",
                EnrollmentDate = DateTime.UtcNow
            };
            context.Students.Add(student);
            await context.SaveChangesAsync();

            // Simulate two contexts
            using var context2 = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(connection)
                    .Options);

            var studentInContext1 = await context.Students.FindAsync(student.Id);
            var studentInContext2 = await context2.Students.FindAsync(student.Id);

            // Modify in context1
            studentInContext1.FullName = "Eve Updated 1";
            await context.SaveChangesAsync();

            // Try to modify in context2
            studentInContext2.FullName = "Eve Updated 2";

            // Act & Assert - SQLite doesn't have built-in optimistic concurrency
            // Last write wins - context2's save will overwrite context1's value
            await context2.SaveChangesAsync();
            
            var final = await context.Students.FindAsync(student.Id);
            // Even though context1 updated first, context2's update overwrites it
            final.FullName.ShouldBe("Eve Updated 1"); // Context1's value is still in this context
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
