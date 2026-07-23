using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestLog.API.Middleware;
using QuestLog.Application.Common.Behaviours;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Infrastructure.Data;
using QuestLog.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────

// IHttpContextAccessor — needed by CurrentUserService to read JWT claims
builder.Services.AddHttpContextAccessor();

// Current user service — scoped so it re-reads claims on every request
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

// EF Core + PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection is not configured.")));

// Register IAppDbContext → AppDbContext so Application handlers stay decoupled from Infrastructure
builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

// Register Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(QuestLog.Infrastructure.Repositories.Repository<>));
builder.Services.AddScoped<IHabitRepository, QuestLog.Infrastructure.Repositories.HabitRepository>();
builder.Services.AddScoped<IGoalRepository, QuestLog.Infrastructure.Repositories.GoalRepository>();
builder.Services.AddScoped<IDailyLogRepository, QuestLog.Infrastructure.Repositories.DailyLogRepository>();

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

// JWT Authentication
string jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

// Apply [Authorize] globally — every controller requires a valid JWT by default.
// AuthController overrides this with [AllowAnonymous].
builder.Services.AddControllers(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
})
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

// CORS — dynamic configuration
var allowedOriginsStr = builder.Configuration["AllowedOrigins"] ?? "http://localhost:4200";
var allowedOrigins = allowedOriginsStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

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

app.UseCors("AllowOrigins");
app.UseAuthentication(); // Must come before UseAuthorization
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready");

app.Run();
