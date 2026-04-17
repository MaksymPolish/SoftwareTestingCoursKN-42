using Lab7.Api.Data;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;

namespace Lab7.Api.Factory;

public class StudentContextFactory : IDesignTimeDbContextFactory<StudentContext>
{
    public StudentContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<StudentContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=Lab7;Username=postgres;Password=postgres");
        
        return new StudentContext(optionsBuilder.Options);
    }
}
