using Xunit;
using Lab7.Api.Controllers;
using Lab7.Api.Data;
using Lab7.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lab7.IntegrationTests;

public class StudentsControllerTests
{
    private StudentContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<StudentContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new StudentContext(options);
    }

    [Fact]
    public async Task GetStudents_ShouldReturnOkResult()
    {
        // Arrange
        var context = GetInMemoryContext();
        var students = new List<Student>
        {
            new Student { FirstName = "John", LastName = "Doe", Email = "john@test.com", StudentNumber = "STU001", CourseYear = 1, GPA = 3.5m, EnrollmentDate = DateTime.UtcNow }
        };
        context.Students.AddRange(students);
        await context.SaveChangesAsync();

        var mockLogger = new Mock<ILogger<StudentsController>>();
        var controller = new StudentsController(context, mockLogger.Object);

        // Act
        var result = await controller.GetStudents();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
        
        var returnedStudents = Assert.IsType<List<Student>>(okResult.Value);
        Assert.Single(returnedStudents);
        Assert.Equal("John", returnedStudents[0].FirstName);
    }

    [Fact]
    public async Task GetStudent_WithValidId_ShouldReturnOkResult()
    {
        // Arrange
        var context = GetInMemoryContext();
        var student = new Student 
        { 
            FirstName = "Jane", 
            LastName = "Smith", 
            Email = "jane@test.com", 
            StudentNumber = "STU002", 
            CourseYear = 2, 
            GPA = 3.8m, 
            EnrollmentDate = DateTime.UtcNow 
        };
        context.Students.Add(student);
        await context.SaveChangesAsync();

        var mockLogger = new Mock<ILogger<StudentsController>>();
        var controller = new StudentsController(context, mockLogger.Object);

        // Act
        var result = await controller.GetStudent(student.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStudent = Assert.IsType<Student>(okResult.Value);
        Assert.Equal("Jane", returnedStudent.FirstName);
    }

    [Fact]
    public async Task GetStudent_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var context = GetInMemoryContext();
        var mockLogger = new Mock<ILogger<StudentsController>>();
        var controller = new StudentsController(context, mockLogger.Object);

        // Act
        var result = await controller.GetStudent(9999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateStudent_ShouldReturnCreatedAtActionResult()
    {
        // Arrange
        var context = GetInMemoryContext();
        var newStudent = new Student 
        { 
            FirstName = "New", 
            LastName = "Student", 
            Email = "new@test.com", 
            StudentNumber = "STU003", 
            CourseYear = 1, 
            GPA = 3.0m, 
            EnrollmentDate = DateTime.UtcNow 
        };

        var mockLogger = new Mock<ILogger<StudentsController>>();
        var controller = new StudentsController(context, mockLogger.Object);

        // Act
        var result = await controller.CreateStudent(newStudent);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(StudentsController.GetStudent), createdResult.ActionName);
    }

    [Fact]
    public async Task Search_ShouldReturnMatchingStudents()
    {
        // Arrange
        var context = GetInMemoryContext();
        var students = new List<Student>
        {
            new Student { FirstName = "Alice", LastName = "Johnson", Email = "alice@test.com", StudentNumber = "STU004", CourseYear = 1, GPA = 3.5m, EnrollmentDate = DateTime.UtcNow },
            new Student { FirstName = "Bob", LastName = "Brown", Email = "bob@test.com", StudentNumber = "STU005", CourseYear = 2, GPA = 3.2m, EnrollmentDate = DateTime.UtcNow }
        };
        context.Students.AddRange(students);
        await context.SaveChangesAsync();

        var mockLogger = new Mock<ILogger<StudentsController>>();
        var controller = new StudentsController(context, mockLogger.Object);

        // Act
        var result = await controller.Search("Alice");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStudents = Assert.IsType<List<Student>>(okResult.Value);
        Assert.Single(returnedStudents);
        Assert.Equal("Alice", returnedStudents[0].FirstName);
    }

    [Fact]
    public async Task GetByYear_ShouldReturnStudentsOfSpecificYear()
    {
        // Arrange
        var context = GetInMemoryContext();
        var students = new List<Student>
        {
            new Student { FirstName = "Year1", LastName = "User", Email = "y1@test.com", StudentNumber = "STU006", CourseYear = 1, GPA = 3.0m, EnrollmentDate = DateTime.UtcNow },
            new Student { FirstName = "Year2", LastName = "User", Email = "y2@test.com", StudentNumber = "STU007", CourseYear = 2, GPA = 3.5m, EnrollmentDate = DateTime.UtcNow }
        };
        context.Students.AddRange(students);
        await context.SaveChangesAsync();

        var mockLogger = new Mock<ILogger<StudentsController>>();
        var controller = new StudentsController(context, mockLogger.Object);

        // Act
        var result = await controller.GetByYear(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStudents = Assert.IsType<List<Student>>(okResult.Value);
        Assert.Single(returnedStudents);
        Assert.Equal(1, returnedStudents[0].CourseYear);
    }
}
