using AutoFixture;
using Lab7.Api.Data;
using Lab7.Api.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Port=5432;Database=Lab7;Username=postgres;Password=postgres";

builder.Services.AddDbContext<StudentContext>(options =>
    options.UseNpgsql(connectionString)
);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseResponseCompression();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<StudentContext>();
    context.Database.Migrate();
    
    if (!context.Students.Any())
    {
        // Seed database with AutoFixture
        var fixture = new Fixture();

        fixture.Customize<Student>(composer => composer
            .With(s => s.Email, () => $"student{fixture.Create<int>():D5}@university.edu")
            .With(s => s.StudentNumber, () => $"STU{fixture.Create<int>():D6}")
            .With(s => s.CourseYear, () => (fixture.Create<int>() % 4) + 1)
            .With(s => s.GPA, () => Math.Round((decimal)(fixture.Create<double>() * 4.0), 2))
            .With(s => s.EnrollmentDate, () => DateTime.UtcNow.AddDays(-(fixture.Create<int>() % (365 * 4))))
            .Without(s => s.Id)
        );

        var students = fixture.CreateMany<Student>(10000).ToList();
        context.Students.AddRange(students);
        context.SaveChanges();
        
        Console.WriteLine($"Database seeded with {students.Count} students using AutoFixture.");
    }
}

app.Run();
