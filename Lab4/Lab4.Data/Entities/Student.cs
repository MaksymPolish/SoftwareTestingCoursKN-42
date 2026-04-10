namespace Lab4.Data.Entities;

public class Student
{
    public int Id { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
