namespace Lab5.Data.Models;

public class CreateStudentRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime? EnrollmentDate { get; set; }
}

public class UpdateStudentRequest
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
