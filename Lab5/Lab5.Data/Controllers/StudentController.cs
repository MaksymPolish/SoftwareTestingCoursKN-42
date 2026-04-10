using Lab5.Data.Models;
using Lab5.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lab5.Data.Controllers;

/// Student API Controller
/// Provides CRUD operations and complex queries for Student management
public class StudentController
{
    private readonly StudentRepository _repository;
    private readonly AppDbContext _context;

    public StudentController(StudentRepository repository, AppDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    /// Get student by ID with enrollments
    public async Task<Student?> GetStudentByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    /// Get all students
    public async Task<List<Student>> GetAllStudentsAsync()
    {
        return await _repository.GetAllAsync();
    }

    /// Create new student
    public async Task<Student> CreateStudentAsync(Student student)
    {
        await _repository.AddAsync(student);
        return student;
    }

    /// Update existing student
    public async Task<bool> UpdateStudentAsync(int id, Student updatedStudent)
    {
        var student = await _repository.GetByIdAsync(id);
        if (student == null) return false;

        student.FullName = updatedStudent.FullName;
        student.Email = updatedStudent.Email;
        
        await _repository.UpdateAsync(student);
        return true;
    }

    /// Delete student (cascade deletes enrollments)
    public async Task<bool> DeleteStudentAsync(int id)
    {
        var student = await _repository.GetByIdAsync(id);
        if (student == null) return false;

        await _repository.DeleteAsync(id);
        return true;
    }

    /// Get students by course  
    public async Task<List<Student>> GetStudentsInCourseAsync(int courseId)
    {
        return await _context.Students
            .Where(s => s.Enrollments.Any(e => e.CourseId == courseId))
            .Include(s => s.Enrollments)
            .ToListAsync();
    }

    /// Get top students by average grade
    public async Task<List<Student>> GetTopStudentsAsync(int count = 10)
    {
        return await _repository.GetTopStudentsAsync(count);
    }

    /// Get student's enrollments with course details
    public async Task<List<Enrollment>> GetStudentEnrollmentsAsync(int studentId)
    {
        return await _context.Enrollments
            .Where(e => e.StudentId == studentId)
            .Include(e => e.Course)
            .ToListAsync();
    }

    /// Get student enrollment statistics
    public async Task<object> GetStudentStatisticsAsync(int studentId)
    {
        var student = await _context.Students
            .Include(s => s.Enrollments)
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student == null) return new { error = "Student not found" };

        var enrollments = student.Enrollments;
        var averageGrade = enrollments.Any() 
            ? Math.Round(enrollments.Average(e => e.Grade ?? 0), 2)
            : 0;

        return new
        {
            studentId = student.Id,
            name = student.FullName,
            email = student.Email,
            enrollmentCount = enrollments.Count,
            averageGrade = averageGrade,
            enrolledCourses = enrollments.Count(e => e.Grade.HasValue),
            enrollmentDate = student.EnrollmentDate
        };
    }

    /// Enroll student in course
    public async Task<bool> EnrollStudentAsync(int studentId, int courseId, decimal? grade = null)
    {
        var student = await _context.Students.FindAsync(studentId);
        var course = await _context.Courses.FindAsync(courseId);

        if (student == null || course == null) return false;

        // Check if already enrolled
        var existing = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId);

        if (existing != null) return false; // Already enrolled

        var enrollment = new Enrollment
        {
            StudentId = studentId,
            CourseId = courseId,
            Grade = grade
        };

        _context.Enrollments.Add(enrollment);
        await _context.SaveChangesAsync();
        return true;
    }

    /// Update enrollment grade
    public async Task<bool> UpdateEnrollmentGradeAsync(int studentId, int courseId, decimal grade)
    {
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId);

        if (enrollment == null) return false;

        enrollment.Grade = grade;
        _context.Enrollments.Update(enrollment);
        await _context.SaveChangesAsync();
        return true;
    }
}
