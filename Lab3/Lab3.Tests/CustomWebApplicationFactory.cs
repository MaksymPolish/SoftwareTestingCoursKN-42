namespace Lab3.Tests;

using Lab3.Api.Models;
using Lab3.Api.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real repository registration
            var descriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(IProductRepository));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add seeded in-memory repository
            services.AddSingleton<IProductRepository>(sp =>
            {
                var repo = new InMemoryProductRepository();
                repo.Add(new Product { Name = "Laptop", Price = 999.99m });
                repo.Add(new Product { Name = "Mouse", Price = 29.99m });
                return repo;
            });
        });
    }
}
