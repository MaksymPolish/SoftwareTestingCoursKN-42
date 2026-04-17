namespace Lab7.Api.Models;

public class Student
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;
    public int CourseYear { get; set; }
    public decimal GPA { get; set; }
    public DateTime EnrollmentDate { get; set; }
}
