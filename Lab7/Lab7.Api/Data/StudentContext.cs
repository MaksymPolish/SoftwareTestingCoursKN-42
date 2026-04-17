using Lab7.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Lab7.Api.Data;

public class StudentContext : DbContext
{
    public StudentContext(DbContextOptions<StudentContext> options) : base(options)
    {
    }

    public DbSet<Student> Students { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.StudentNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CourseYear).IsRequired();
            entity.Property(e => e.GPA).HasPrecision(3, 2);
            entity.Property(e => e.EnrollmentDate).IsRequired();

            // Index for search queries
            entity.HasIndex(e => e.StudentNumber).IsUnique();
            entity.HasIndex(e => e.Email);
        });
    }
}
