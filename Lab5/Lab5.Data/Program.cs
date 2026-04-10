// Lab5 API - EntryPoint
using Lab5.Data;
using Lab5.Data.Controllers;
using Lab5.Data.Repositories;
using Lab5.Data.Models;
using Microsoft.EntityFrameworkCore;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
// 1. Database Context (supports SQLite, SQL Server, PostgreSQL)
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=/app/data/lab5.db";
    
    Console.WriteLine($"[INFO] Connection String: {connectionString}");
    
    // Determine provider based on connection string
    if (connectionString.Contains("Host="))
    {
        // PostgreSQL
        Console.WriteLine("[INFO] Using PostgreSQL provider");
        options.UseNpgsql(connectionString);
    }
    else if (connectionString.Contains("Server="))
    {
        // SQL Server
        Console.WriteLine("[INFO] Using SQL Server provider");
        options.UseSqlServer(connectionString);
    }
    else
    {
        // SQLite (default)
        Console.WriteLine("[INFO] Using SQLite provider");
        options.UseSqlite(connectionString);
    }
});

// 2. Repository
builder.Services.AddScoped<StudentRepository>();

// 3. Controller
builder.Services.AddScoped<StudentController>();

// 4. FluentValidation
builder.Services.AddScoped<IValidator<CreateStudentRequest>, CreateStudentRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateStudentRequest>, UpdateStudentRequestValidator>();

// 5. API Explorer
builder.Services.AddEndpointsApiExplorer();

// 6. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Initialize database
try
{
    Console.WriteLine("[INFO] Starting database initialization...");
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Console.WriteLine("[INFO] Creating/migrating database...");
        context.Database.EnsureCreated();
        Console.WriteLine("[INFO] Database creation completed successfully");
        
        // Seed sample data if empty
        if (!context.Students.Any())
        {
            Console.WriteLine("[INFO] Seeding sample data...");
            SeedSampleData(context);
            Console.WriteLine("[INFO] Sample data seeded successfully");
        }
    }
    Console.WriteLine("[INFO] Database initialization completed");
}
catch (Exception ex)
{
    Console.WriteLine($"[ERROR] Database initialization error: {ex.Message}");
    Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
}

// Configure the HTTP request pipeline
app.UseCors("AllowAll");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck");

// Student endpoints using dependency injection
var group = app.MapGroup("/api/students")
    .WithTags("Students");

// GET /api/students
group.MapGet("/", GetAllStudents)
    .WithName("GetAllStudents");

// GET /api/students/{id}
group.MapGet("/{id}", GetStudentById)
    .WithName("GetStudentById");

// POST /api/students
group.MapPost("/", CreateStudent)
    .WithName("CreateStudent");

// PUT /api/students/{id}
group.MapPut("/{id}", UpdateStudent)
    .WithName("UpdateStudent");

// DELETE /api/students/{id}
group.MapDelete("/{id}", DeleteStudent)
    .WithName("DeleteStudent");

// GET /api/students/top/{count}
group.MapGet("/top/{count:int}", GetTopStudents)
    .WithName("GetTopStudents");

// GET /api/students/{id}/stats
group.MapGet("/{id}/stats", GetStudentStatistics)
    .WithName("GetStudentStatistics");

// GET /api/students/{id}/enrollments
group.MapGet("/{id}/enrollments", GetStudentEnrollments)
    .WithName("GetStudentEnrollments");

app.Run();

// ============ Endpoint Handlers ============

static async Task<IResult> GetAllStudents(StudentController controller)
{
    var students = await controller.GetAllStudentsAsync();
    return Results.Ok(students);
}

static async Task<IResult> GetStudentById(int id, StudentController controller)
{
    var student = await controller.GetStudentByIdAsync(id);
    return student == null ? Results.NotFound() : Results.Ok(student);
}

static async Task<IResult> CreateStudent(
    Lab5.Data.Models.CreateStudentRequest request,
    StudentController controller,
    IValidator<Lab5.Data.Models.CreateStudentRequest> validator)
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.BadRequest(
            string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));

    var student = new Student
    {
        FullName = request.FullName,
        Email = request.Email,
        EnrollmentDate = request.EnrollmentDate ?? DateTime.UtcNow
    };

    var created = await controller.CreateStudentAsync(student);
    return Results.Created($"/api/students/{created.Id}", created);
}

static async Task<IResult> UpdateStudent(
    int id,
    Lab5.Data.Models.UpdateStudentRequest request,
    StudentController controller,
    IValidator<Lab5.Data.Models.UpdateStudentRequest> validator)
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.BadRequest(
            string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));

    var updated = await controller.UpdateStudentAsync(id, new Student
    {
        Id = id,
        FullName = request.FullName,
        Email = request.Email
    });

    return updated ? Results.NoContent() : Results.NotFound();
}

static async Task<IResult> DeleteStudent(int id, StudentController controller)
{
    var deleted = await controller.DeleteStudentAsync(id);
    return deleted ? Results.NoContent() : Results.NotFound();
}

static async Task<IResult> GetTopStudents(int count, StudentController controller)
{
    var students = await controller.GetTopStudentsAsync(count);
    return Results.Ok(students);
}

static async Task<IResult> GetStudentStatistics(int id, StudentController controller)
{
    var stats = await controller.GetStudentStatisticsAsync(id);
    return Results.Ok(stats);
}

static async Task<IResult> GetStudentEnrollments(int id, StudentController controller)
{
    var enrollments = await controller.GetStudentEnrollmentsAsync(id);
    return Results.Ok(enrollments);
}

// ============ Seed Data ============

static void SeedSampleData(AppDbContext context)
{
    var course1 = new Course { Title = "Database Design", Credits = 4 };
    var course2 = new Course { Title = "Web Development", Credits = 3 };
    var course3 = new Course { Title = "Cloud Computing", Credits = 4 };

    context.Courses.AddRange(course1, course2, course3);
    context.SaveChanges();

    var student1 = new Student
    {
        FullName = "John Smith",
        Email = "john@example.com",
        EnrollmentDate = DateTime.UtcNow.AddMonths(-6)
    };
    var student2 = new Student
    {
        FullName = "Jane Doe",
        Email = "jane@example.com",
        EnrollmentDate = DateTime.UtcNow.AddMonths(-4)
    };
    var student3 = new Student
    {
        FullName = "Bob Johnson",
        Email = "bob@example.com",
        EnrollmentDate = DateTime.UtcNow.AddMonths(-2)
    };

    context.Students.AddRange(student1, student2, student3);
    context.SaveChanges();

    var enrollment1 = new Enrollment
    {
        StudentId = student1.Id,
        CourseId = course1.Id,
        Grade = 92
    };
    var enrollment2 = new Enrollment
    {
        StudentId = student1.Id,
        CourseId = course2.Id,
        Grade = 88
    };
    var enrollment3 = new Enrollment
    {
        StudentId = student2.Id,
        CourseId = course1.Id,
        Grade = 85
    };
    var enrollment4 = new Enrollment
    {
        StudentId = student2.Id,
        CourseId = course3.Id,
        Grade = 91
    };
    var enrollment5 = new Enrollment
    {
        StudentId = student3.Id,
        CourseId = course2.Id,
        Grade = 79
    };

    context.Enrollments.AddRange(enrollment1, enrollment2, enrollment3, enrollment4, enrollment5);
    context.SaveChanges();
}
