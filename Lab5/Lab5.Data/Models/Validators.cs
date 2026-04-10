using FluentValidation;

namespace Lab5.Data.Models;

public class CreateStudentRequestValidator : AbstractValidator<CreateStudentRequest>
{
    public CreateStudentRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("FullName is required and cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("A valid Email is required.");

        RuleFor(x => x.EnrollmentDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("EnrollmentDate cannot be in the future.");
    }
}

public class UpdateStudentRequestValidator : AbstractValidator<UpdateStudentRequest>
{
    public UpdateStudentRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("A valid Id is required.");

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("FullName is required and cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("A valid Email is required.");
    }
}
