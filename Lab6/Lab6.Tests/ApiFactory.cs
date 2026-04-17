using Lab6.Api;
using Lab6.Api.Data;
using Lab6.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Xunit;

namespace Lab6.Tests;

public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("YourPassword@123")
        .Build();

    public string BaseUrl => Server.BaseAddress.ToString().TrimEnd('/');

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add DbContext with Testcontainers connection string
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(_dbContainer.GetConnectionString()));
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        // Apply migrations after container starts
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        // Seed test data
        await SeedDataAsync(context);
    }

    private static async Task SeedDataAsync(AppDbContext context)
    {
        // Clear existing data
        context.Products.RemoveRange(context.Products);
        await context.SaveChangesAsync();

        // Seed 100 products
        var products = Enumerable.Range(1, 100).Select(i => new Product
        {
            Name = $"Product {i:D3}",
            Price = 9.99m + i,
            Category = i % 3 == 0 ? "Electronics" : i % 3 == 1 ? "Books" : "Clothing",
            StockQuantity = i * 10,
            CreatedAt = DateTime.UtcNow
        });

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
