using Microsoft.EntityFrameworkCore;
using Lab5.Data.Models;

namespace Lab5.Data.Repositories;

public class StudentRepository
{
    private readonly AppDbContext _context;

    public StudentRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// Gets a student by ID with their enrollments included
    public async Task<Student?> GetByIdAsync(int id)
    {
        return await _context.Students
            .Include(s => s.Enrollments)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    /// Gets all students
    public async Task<List<Student>> GetAllAsync()
    {
        return await _context.Students.ToListAsync();
    }

    /// Adds a new student
    public async Task AddAsync(Student student)
    {
        if (student == null)
            throw new ArgumentNullException(nameof(student));

        _context.Students.Add(student);
        await _context.SaveChangesAsync();
    }

    /// Updates an existing student
    public async Task UpdateAsync(Student student)
    {
        if (student == null)
            throw new ArgumentNullException(nameof(student));

        _context.Students.Update(student);
        await _context.SaveChangesAsync();
    }

    /// Deletes a student by ID
    public async Task DeleteAsync(int id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student != null)
        {
            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
        }
    }

    /// Gets top N students by average grade across all their enrollments
    public async Task<List<Student>> GetTopStudentsAsync(int count)
    {
        var students = await _context.Students
            .Include(s => s.Enrollments)
            .ToListAsync();

        return students
            .OrderByDescending(s =>
            {
                var enrollmentsWithGrades = s.Enrollments
                    .Where(e => e.Grade.HasValue)
                    .Select(e => e.Grade.Value)
                    .ToList();

                return enrollmentsWithGrades.Count > 0
                    ? enrollmentsWithGrades.Average()
                    : 0m;
            })
            .Take(count)
            .ToList();
    }
}
