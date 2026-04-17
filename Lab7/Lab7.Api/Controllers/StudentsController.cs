using Lab7.Api.Data;
using Lab7.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lab7.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly StudentContext _context;
    private readonly ILogger<StudentsController> _logger;

    public StudentsController(StudentContext context, ILogger<StudentsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Student>>> GetStudents()
    {
        // Simulate database delay
        await Task.Delay(50);
        var students = await _context.Students.ToListAsync();
        return Ok(students);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Student>> GetStudent(int id)
    {
        // Simulate database delay
        await Task.Delay(20);
        var student = await _context.Students.FindAsync(id);
        
        if (student == null)
        {
            return NotFound();
        }

        return Ok(student);
    }

    [HttpPost]
    public async Task<ActionResult<Student>> CreateStudent([FromBody] Student student)
    {
        // Simulate database delay
        await Task.Delay(30);
        
        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetStudent), new { id = student.Id }, student);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<Student>>> Search([FromQuery] string q)
    {
        // Simulate expensive computation
        await Task.Delay(150);
        
        var results = await _context.Students
            .Where(s => s.FirstName.Contains(q) || 
                       s.LastName.Contains(q) || 
                       s.Email.Contains(q))
            .ToListAsync();

        return Ok(results);
    }

    [HttpGet("bycourse/{courseYear:int}")]
    public async Task<ActionResult<IEnumerable<Student>>> GetByYear(int courseYear)
    {
        // Simulate database delay
        await Task.Delay(40);
        
        var students = await _context.Students
            .Where(s => s.CourseYear == courseYear)
            .ToListAsync();

        return Ok(students);
    }
}
