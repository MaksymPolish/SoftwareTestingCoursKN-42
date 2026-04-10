using Lab4.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lab4.Data.Repositories;

public class StudentRepository
{
    private readonly AppDbContext _context;

    public StudentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Student?> GetByIdAsync(int id)
    {
        return await _context.Students
            .Include(s => s.Enrollments)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<Student>> GetAllAsync()
    {
        return await _context.Students.ToListAsync();
    }

    public async Task AddAsync(Student student)
    {
        _context.Students.Add(student);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Student student)
    {
        _context.Students.Update(student);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student != null)
        {
            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Student>> GetTopStudentsAsync(int count)
    {
        var students = await _context.Students
            .Include(s => s.Enrollments)
            .ToListAsync();
        
        return students
            .OrderByDescending(s => s.Enrollments.Any() ? s.Enrollments.Average(e => e.Grade ?? 0) : 0)
            .Take(count)
            .ToList();
    }
}
