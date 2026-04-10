using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lab5.Data;
using Lab5.Data.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Lab5.Tests;

public class SqliteRelationalTests
{
    public readonly AppDbContext Context;
    public readonly SqliteConnection Connection;

    public SqliteRelationalTests()
    {
        Connection = new SqliteConnection("DataSource=:memory:");
        Connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(Connection)
            .Options;

        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();
    }

    [Fact]
    public async Task ForeignKey_EnrollingInNonExistingCourse_ThrowsAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        using (Context)
        using (Connection)
        {
            // Arrange
            var enrollment = new Enrollment
            {
                StudentId = 999,
                CourseId = 999,
                Grade = 85
            };

            Context.Enrollments.Add(enrollment);
            
            // Act & Assert
            var exception = await Should.ThrowAsync<DbUpdateException>(
                () => Context.SaveChangesAsync(ct));
            exception.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task UniqueConstraint_DuplicateEmail_ThrowsAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        using (Connection)
        using (Context)
        {
            // Arrange
            var student1 = new Student
            {
                FullName = "Alice", Email = "dup@test.com",
                EnrollmentDate = DateTime.UtcNow
            };
            var student2 = new Student
            {
                FullName = "Bob", Email = "dup@test.com",
                EnrollmentDate = DateTime.UtcNow
            };

            Context.Students.Add(student1);
            await Context.SaveChangesAsync(ct);
            Context.Students.Add(student2);

            // Act & Assert
            await Should.ThrowAsync<DbUpdateException>(
                () => Context.SaveChangesAsync(ct));
        }
    }

    [Fact]
    public async Task CascadeDelete_DeletingStudent_RemovesEnrollmentsAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        using (Connection)
        using (Context)
        {
            // Arrange
            var course = new Course { Title = "CS101", Credits = 3 };
            var student = new Student
            {
                FullName = "Charlie", Email = "charlie@test.com",
                EnrollmentDate = DateTime.UtcNow,
                Enrollments = new List<Enrollment>
                {
                    new Enrollment { Course = course, Grade = 88 }
                }
            };
            Context.Students.Add(student);
            await Context.SaveChangesAsync(ct);

            // Act
            Context.Students.Remove(student);
            await Context.SaveChangesAsync(ct);

            // Assert
            var enrollments = await Context.Enrollments.ToListAsync(ct);
            enrollments.ShouldBeEmpty();
        }
    }
}
