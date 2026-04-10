using Lab5.Data.Models;
using Shouldly;
using Xunit;

namespace Lab5.Tests;

public class StudentValidationTests
{
    private readonly CreateStudentRequestValidator _createValidator = new();
    private readonly UpdateStudentRequestValidator _updateValidator = new();

    [Fact]
    public async Task CreateStudent_ValidRequest_ShouldNotHaveAnyErrors()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var request = new CreateStudentRequest
        {
            FullName = "John Doe",
            Email = "johndoe@example.com",
            EnrollmentDate = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var result = await _createValidator.ValidateAsync(request, ct);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateStudent_EmptyFullName_ShouldHaveValidationErrorForFullName()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var request = new CreateStudentRequest
        {
            FullName = "",
            Email = "johndoe@example.com",
            EnrollmentDate = DateTime.UtcNow
        };

        // Act
        var result = await _createValidator.ValidateAsync(request, ct);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldNotBeEmpty();
        result.Errors.Any(e => e.PropertyName == "FullName").ShouldBeTrue();
    }

    [Fact]
    public async Task CreateStudent_InvalidEmail_ShouldHaveValidationErrorForEmail()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var request = new CreateStudentRequest
        {
            FullName = "John Doe",
            Email = "invalid-email-format",
            EnrollmentDate = DateTime.UtcNow
        };

        // Act
        var result = await _createValidator.ValidateAsync(request, ct);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Any(e => e.PropertyName == "Email").ShouldBeTrue();
    }

    [Fact]
    public async Task CreateStudent_FutureEnrollmentDate_ShouldHaveValidationErrorForEnrollmentDate()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var request = new CreateStudentRequest
        {
            FullName = "John Doe",
            Email = "johndoe@example.com",
            EnrollmentDate = DateTime.UtcNow.AddDays(1)
        };

        // Act
        var result = await _createValidator.ValidateAsync(request, ct);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Any(e => e.PropertyName == "EnrollmentDate").ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateStudent_ZeroId_ShouldHaveValidationErrorForId()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var request = new UpdateStudentRequest
        {
            Id = 0,
            FullName = "Jane Doe",
            Email = "jane@example.com"
        };

        // Act
        var result = await _updateValidator.ValidateAsync(request, ct);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Any(e => e.PropertyName == "Id").ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateStudent_EmptyEmail_ShouldHaveValidationErrorForEmail()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var request = new UpdateStudentRequest
        {
            Id = 1,
            FullName = "Jane Doe",
            Email = ""
        };

        // Act
        var result = await _updateValidator.ValidateAsync(request, ct);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Any(e => e.PropertyName == "Email").ShouldBeTrue();
    }
}