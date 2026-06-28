using FluentValidation;
using Microsoft.EntityFrameworkCore;
using QuestLog.API.Middleware;
using QuestLog.Application.Common.Behaviours;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────

// EF Core + SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=questlog.db"));

// Register IAppDbContext → AppDbContext so Application handlers stay decoupled from Infrastructure
builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

// MediatR — scans Application assembly for all handlers
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(QuestLog.Application.AssemblyMarker).Assembly);

    // Register the validation pipeline so validators run before every handler
    cfg.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
});

// FluentValidation — scans Application assembly for all validators
builder.Services.AddValidatorsFromAssembly(
    typeof(QuestLog.Application.AssemblyMarker).Assembly);

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize enums as strings in JSON
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "QuestLog API", Version = "v1" });
});

// CORS — allow Angular dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ── App Pipeline ──────────────────────────────────────────────────────────────

var app = builder.Build();

// Auto-apply migrations on startup (great for dev/SQLite)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Global exception handling — must be first in pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "QuestLog API v1");
        c.RoutePrefix = string.Empty; // Swagger at root: http://localhost:5000
    });
}

app.UseCors("AllowAngularDev");
app.UseAuthorization();
app.MapControllers();

app.Run();
