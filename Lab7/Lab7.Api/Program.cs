using Lab7.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure PostgreSQL connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Port=5432;Database=Lab7;Username=postgres;Password=postgres";

builder.Services.AddDbContext<StudentContext>(options =>
    options.UseNpgsql(connectionString)
);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Seed database on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<StudentContext>();
    context.Database.Migrate();
    
    if (!context.Students.Any())
    {
        SeedDatabase(context);
    }
}

app.Run();

void SeedDatabase(StudentContext context)
{
    var random = new Random(42); // For reproducible results
    var firstNames = new[] { "Alice", "Bob", "Charlie", "Diana", "Eve", "Frank", "Grace", "Henry", "Iris", "Jack" };
    var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez" };
    var students = new List<Lab7.Api.Models.Student>();

    for (int i = 1; i <= 10000; i++)
    {
        students.Add(new Lab7.Api.Models.Student
        {
            FirstName = firstNames[random.Next(firstNames.Length)],
            LastName = lastNames[random.Next(lastNames.Length)],
            Email = $"student{i:D5}@university.edu",
            StudentNumber = $"STU{i:D6}",
            CourseYear = random.Next(1, 5),
            GPA = (decimal)(random.NextDouble() * 4.0),
            EnrollmentDate = DateTime.UtcNow.AddDays(-random.Next(0, 365 * 4))
        });
    }

    context.Students.AddRange(students);
    context.SaveChanges();
    Console.WriteLine("Database seeded with 10,000 students.");
}
