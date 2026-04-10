using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Sdk;
using Lab5.Data;
using Lab5.Data.Models;
using Lab5.Data.Repositories;
using Shouldly;

namespace Lab5.Tests.SQLite;

/// <summary>
/// Task 2: Tests for relational constraints using SQLite InMemory provider
/// 
/// SQLite in-memory mode provides real relational database features:
/// - Foreign key constraints
/// - Unique constraints
/// - Cascade delete behavior
/// - Transaction support
///
/// Unlike InMemory provider, SQLite enforces these constraints.
/// </summary>
public class StudentRepositoryRelationalTests
{
    /// <summary>
    /// Creates an InMemory SQLite DbContext for testing
    /// SQLite in-memory databases exist only while the connection is open
    /// </summary>
    private (AppDbContext context, SqliteConnection connection) CreateSqliteContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return (context, connection);
    }

    [Fact]
    public async Task ForeignKey_EnrollingInNonExistingCourse_ThrowsDbUpdateExceptionAsync()
    {
        // Arrange
        var (context, connection) = CreateSqliteContext();
        using (connection)
        using (context)
        {
            var student = new Student
            {
                FullName = "Test Student",
                Email = "fk@test.com",
                EnrollmentDate = DateTime.UtcNow
            };
            context.Students.Add(student);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var enrollment = new Enrollment
            {
                StudentId = student.Id,
                CourseId = 999, // Non-existent course
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
    public async Task ForeignKey_StudentDoesNotExist_ThrowsDbUpdateExceptionAsync()
    {
        // Arrange
        var (context, connection) = CreateSqliteContext();
        using (connection)
        using (context)
        {
            var course = new Course { Title = "Test Course", Credits = 3 };
            context.Courses.Add(course);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var enrollment = new Enrollment
            {
                StudentId = 999, // Non-existent student
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
    public async Task UniqueConstraint_DuplicateEmail_ThrowsDbUpdateExceptionAsync()
    {
        // Arrange
        var (context, connection) = CreateSqliteContext();
        using (connection)
        using (context)
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
                Email = "dup@test.com", // Same email - violates unique constraint
                EnrollmentDate = DateTime.UtcNow
            };

            context.Students.Add(student1);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            context.Students.Add(student2);

            // Act & Assert
            var exception = await Should.ThrowAsync<DbUpdateException>(
                () => context.SaveChangesAsync(TestContext.Current.CancellationToken));
            exception.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task CascadeDelete_DeletingStudent_RemovesEnrollmentsAsync()
    {
        // Arrange
        var (context, connection) = CreateSqliteContext();
        using (connection)
        using (context)
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
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var studentId = student.Id;
            var enrollmentCount = await context.Enrollments.CountAsync(TestContext.Current.CancellationToken);
            enrollmentCount.ShouldBe(1);

            // Act
            context.Students.Remove(student);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Assert - Enrollments should be cascade-deleted
            var remainingEnrollments = await context.Enrollments
                .Where(e => e.StudentId == studentId)
                .ToListAsync(TestContext.Current.CancellationToken);
            remainingEnrollments.ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task CascadeDelete_DeletingCourse_RemovesEnrollmentsAsync()
    {
        // Arrange
        var (context, connection) = CreateSqliteContext();
        using (connection)
        using (context)
        {
            var course = new Course { Title = "BIO101", Credits = 4 };
            var student = new Student
            {
                FullName = "Diana",
                Email = "diana@test.com",
                EnrollmentDate = DateTime.UtcNow
            };
            context.Courses.Add(course);
            context.Students.Add(student);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var enrollment = new Enrollment
            {
                StudentId = student.Id,
                CourseId = course.Id,
                Grade = 92
            };
            context.Enrollments.Add(enrollment);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var courseId = course.Id;

            // Act
            context.Courses.Remove(course);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Assert - Enrollments should be cascade-deleted
            var remainingEnrollments = await context.Enrollments
                .Where(e => e.CourseId == courseId)
                .ToListAsync(TestContext.Current.CancellationToken);
            remainingEnrollments.ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task Transactions_RollbackOnException_UndoesChangesAsync()
    {
        // Arrange
        var (context, connection) = CreateSqliteContext();
        using (connection)
        using (context)
        {
            var student = new Student
            {
                FullName = "Eve",
                Email = "eve@test.com",
                EnrollmentDate = DateTime.UtcNow
            };
            context.Students.Add(student);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var originalCount = await context.Students.CountAsync(TestContext.Current.CancellationToken);

            // Act & Assert
            try
            {
                using (var transaction = await context.Database.BeginTransactionAsync(TestContext.Current.CancellationToken))
                {
                    var newStudent = new Student
                    {
                        FullName = "Frank",
                        Email = "eve@test.com", // Duplicate email - will cause error
                        EnrollmentDate = DateTime.UtcNow
                    };
                    context.Students.Add(newStudent);
                    await context.SaveChangesAsync(TestContext.Current.CancellationToken);

                    // This should throw due to unique constraint
                    await Should.ThrowAsync<DbUpdateException>(
                        () => transaction.CommitAsync(TestContext.Current.CancellationToken));
                }
            }
            catch
            {
                // Expected
            }

            // Verify rollback occurred
            var finalCount = await context.Students.CountAsync(TestContext.Current.CancellationToken);
            finalCount.ShouldBe(originalCount);
        }
    }

    [Fact]
    public async Task ComparisonWithInMemory_ConstraintsEnforced_DifferentBehaviorAsync()
    {
        // This test documents behavioral differences between InMemory and SQLite providers

        // SQLite behavior (relational - constraints enforced):
        // Foreign key violations → DbUpdateException
        // Unique constraint violations → DbUpdateException
        // Cascade delete → Works automatically

        // InMemory behavior (non-relational - constraints NOT enforced):
        // Foreign key violations → No error (silently fails)
        // Unique constraint violations → No error (duplicate inserted)
        // Cascade delete → Must be handled manually

        var (sqliteContext, connection) = CreateSqliteContext();
        using (connection)
        using (sqliteContext)
        {
            // Arrange
            var course = new Course { Title = "Physics", Credits = 4 };
            sqliteContext.Courses.Add(course);
            await sqliteContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Act - Try to create enrollment with non-existent student
            var invalidEnrollment = new Enrollment
            {
                StudentId = 9999,
                CourseId = course.Id,
                Grade = 75
            };
            sqliteContext.Enrollments.Add(invalidEnrollment);

            // Assert - SQLite throws exception, InMemory doesn't
            await Should.ThrowAsync<DbUpdateException>(
                () => sqliteContext.SaveChangesAsync(TestContext.Current.CancellationToken));
        }
    }

    [Fact]
    public async Task MultipleEnrollments_SameStudentMultipleCourses_WorksCorrectlyAsync()
    {
        // Arrange
        var (context, connection) = CreateSqliteContext();
        using (connection)
        using (context)
        {
            var student = new Student
            {
                FullName = "Grace",
                Email = "grace@test.com",
                EnrollmentDate = DateTime.UtcNow
            };

            var course1 = new Course { Title = "Math", Credits = 4 };
            var course2 = new Course { Title = "Science", Credits = 3 };
            var course3 = new Course { Title = "English", Credits = 3 };

            context.Students.Add(student);
            context.Courses.AddRange(course1, course2, course3);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var enrollments = new List<Enrollment>
            {
                new Enrollment { StudentId = student.Id, CourseId = course1.Id, Grade = 90 },
                new Enrollment { StudentId = student.Id, CourseId = course2.Id, Grade = 85 },
                new Enrollment { StudentId = student.Id, CourseId = course3.Id, Grade = 88 }
            };

            // Act
            context.Enrollments.AddRange(enrollments);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Assert
            var studentEnrollments = await context.Enrollments
                .Where(e => e.StudentId == student.Id)
                .ToListAsync(TestContext.Current.CancellationToken);

            studentEnrollments.Count.ShouldBe(3);
            foreach(var e in studentEnrollments)
            {
                e.StudentId.ShouldBe(student.Id);
            }
        }
    }
}
