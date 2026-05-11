using System.Text;
using Auth.Api.Validation;
using Auth.Infrastructure.DependencyInjection;
using Auth.Infrastructure.Persistence;
using Auth.Infrastructure.Seed;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TournamentPlatform.Messaging.DependencyInjection;
using TournamentPlatform.Shared.Security;
using TournamentPlatform.Shared.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthInfrastructure(builder.Configuration);
builder.Services.AddRabbitMqMessaging(builder.Configuration);
builder.Services.AddTournamentPlatformApiDefaults();
builder.Services.AddControllers();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterPlayerRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddTournamentPlatformSwagger();
builder.Services.AddHealthChecks();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddTournamentPlatformPolicies();
});

var app = builder.Build();

app.UseTournamentPlatformExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseTournamentPlatformCorrelation();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { Service = "Auth.Api", Status = "Running" }));
app.MapControllers();

if (app.Configuration.GetValue<bool>("ApplyMigrations"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await dbContext.Database.MigrateAsync();
}

if (!string.IsNullOrWhiteSpace(app.Configuration["AdminSeed:Password"]))
{
    await app.Services.SeedAuthDatabaseAsync();
}

app.Run();
