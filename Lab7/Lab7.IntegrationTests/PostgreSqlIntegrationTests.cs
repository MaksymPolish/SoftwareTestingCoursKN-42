using Testcontainers.PostgreSql;
using Xunit;
using Lab7.Api.Data;
using Lab7.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Lab7.IntegrationTests;

public class PostgreSqlIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;
    private StudentContext _context = null!;

    public async Task InitializeAsync()
    {
        // Створити контейнер PostgreSQL
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .Build();

        // Запустити контейнер
        await _container.StartAsync();

        // Налаштувати DbContext з connection string контейнера
        var connectionString = _container.GetConnectionString();
        var optionsBuilder = new DbContextOptionsBuilder<StudentContext>();
        optionsBuilder.UseNpgsql(connectionString);

        _context = new StudentContext(optionsBuilder.Options);

        // Застосувати міграції
        await _context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        // Очистити контекст
        await _context.DisposeAsync();

        // Зупинити контейнер
        await _container.StopAsync();
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task DatabaseConnection_ShouldConnect_Successfully()
    {
        // Act & Assert
        Assert.NotNull(_context.Database);
        var canConnect = await _context.Database.CanConnectAsync();
        Assert.True(canConnect, "Database connection failed");
    }

    [Fact]
    public async Task CreateStudent_ShouldPersist_InDatabase()
    {
        // Arrange
        var student = new Student
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@test.com",
            StudentNumber = "TEST001",
            CourseYear = 1,
            GPA = 3.5m,
            EnrollmentDate = DateTime.UtcNow
        };

        // Act
        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        // Assert
        var savedStudent = await _context.Students.FirstOrDefaultAsync(s => s.StudentNumber == "TEST001");
        Assert.NotNull(savedStudent);
        Assert.Equal("John", savedStudent.FirstName);
        Assert.Equal("Doe", savedStudent.LastName);
        Assert.Equal("john.doe@test.com", savedStudent.Email);
    }

    [Fact]
    public async Task QueryStudents_ShouldReturnAllRecords()
    {
        // Arrange
        var students = new List<Student>
        {
            new Student { FirstName = "Alice", LastName = "Smith", Email = "alice@test.com", StudentNumber = "TEST002", CourseYear = 1, GPA = 3.8m, EnrollmentDate = DateTime.UtcNow },
            new Student { FirstName = "Bob", LastName = "Jones", Email = "bob@test.com", StudentNumber = "TEST003", CourseYear = 2, GPA = 3.5m, EnrollmentDate = DateTime.UtcNow }
        };

        // Act
        _context.Students.AddRange(students);
        await _context.SaveChangesAsync();

        var allStudents = await _context.Students.ToListAsync();

        // Assert
        Assert.NotEmpty(allStudents);
        Assert.Equal(2, allStudents.Count);
    }

    [Fact]
    public async Task UniqueConstraint_StudentNumber_ShouldEnforce()
    {
        // Arrange
        var student1 = new Student
        {
            FirstName = "Test1",
            LastName = "User1",
            Email = "test1@test.com",
            StudentNumber = "UNIQUE001",
            CourseYear = 1,
            GPA = 3.0m,
            EnrollmentDate = DateTime.UtcNow
        };

        var student2 = new Student
        {
            FirstName = "Test2",
            LastName = "User2",
            Email = "test2@test.com",
            StudentNumber = "UNIQUE001", // Same as student1
            CourseYear = 1,
            GPA = 3.0m,
            EnrollmentDate = DateTime.UtcNow
        };

        // Act & Assert
        _context.Students.Add(student1);
        await _context.SaveChangesAsync();

        _context.Students.Add(student2);
        var exception = await Assert.ThrowsAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());

        Assert.NotNull(exception);
    }

    [Fact]
    public async Task FilterByYear_ShouldReturnCorrectStudents()
    {
        // Arrange
        var students = new List<Student>
        {
            new Student { FirstName = "Year1", LastName = "User", Email = "y1@test.com", StudentNumber = "Y1", CourseYear = 1, GPA = 3.0m, EnrollmentDate = DateTime.UtcNow },
            new Student { FirstName = "Year2", LastName = "User", Email = "y2@test.com", StudentNumber = "Y2", CourseYear = 2, GPA = 3.0m, EnrollmentDate = DateTime.UtcNow },
            new Student { FirstName = "Year1Again", LastName = "User", Email = "y1a@test.com", StudentNumber = "Y1A", CourseYear = 1, GPA = 3.0m, EnrollmentDate = DateTime.UtcNow }
        };

        _context.Students.AddRange(students);
        await _context.SaveChangesAsync();

        // Act
        var year1Students = await _context.Students.Where(s => s.CourseYear == 1).ToListAsync();

        // Assert
        Assert.Equal(2, year1Students.Count);
        Assert.All(year1Students, s => Assert.Equal(1, s.CourseYear));
    }

    [Fact]
    public async Task SearchByEmail_ShouldFindStudent()
    {
        // Arrange
        var student = new Student
        {
            FirstName = "Search",
            LastName = "Test",
            Email = "search.test@university.edu",
            StudentNumber = "SEARCH001",
            CourseYear = 3,
            GPA = 3.9m,
            EnrollmentDate = DateTime.UtcNow
        };

        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        // Act
        var foundStudent = await _context.Students.FirstOrDefaultAsync(s => s.Email == "search.test@university.edu");

        // Assert
        Assert.NotNull(foundStudent);
        Assert.Equal("Search", foundStudent.FirstName);
    }

    [Fact]
    public async Task DeleteStudent_ShouldRemoveFromDatabase()
    {
        // Arrange
        var student = new Student
        {
            FirstName = "Delete",
            LastName = "Me",
            Email = "delete@test.com",
            StudentNumber = "DELETE001",
            CourseYear = 1,
            GPA = 2.0m,
            EnrollmentDate = DateTime.UtcNow
        };

        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        var id = student.Id;

        // Act
        var studentToDelete = await _context.Students.FindAsync(id);
        Assert.NotNull(studentToDelete);

        _context.Students.Remove(studentToDelete);
        await _context.SaveChangesAsync();

        // Assert
        var deletedStudent = await _context.Students.FindAsync(id);
        Assert.Null(deletedStudent);
    }

    [Fact]
    public async Task MigrationApplied_ShouldHaveCorrectSchema()
    {
        // Act
        var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync();
        
        using var command = connection.CreateCommand();
        command.CommandText = @"SELECT COUNT(*) FROM information_schema.tables 
                                WHERE table_schema = 'public' AND table_name = 'Students'";
        var result = await command.ExecuteScalarAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, Convert.ToInt32(result));
    }
}
