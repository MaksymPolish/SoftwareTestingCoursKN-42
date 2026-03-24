using Lab3.Api.Repositories;
using Lab3.Api.Middleware;
using Lab3.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICorrelationIdProvider, CorrelationIdProvider>();

var app = builder.Build();

// Configure HTTP pipeline - middleware order matters
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program { }
