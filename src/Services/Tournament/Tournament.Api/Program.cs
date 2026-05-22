using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Tournament.Api.Validation;
using Tournament.Application.DependencyInjection;
using Tournament.Infrastructure.DependencyInjection;
using Tournament.Infrastructure.Persistence;
using TournamentPlatform.Messaging.DependencyInjection;
using TournamentPlatform.Shared.Security;
using TournamentPlatform.Shared.Web;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRabbitMqMessaging(builder.Configuration);
builder.Services.AddTournamentApplication(builder.Configuration);
builder.Services.AddTournamentInfrastructure(builder.Configuration);
builder.Services.AddTournamentPlatformApiDefaults();
builder.Services.AddControllers();
builder.Services.AddValidatorsFromAssemblyContaining<CreateTournamentRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddTournamentPlatformSwagger();
builder.Services.AddHealthChecks();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
{
    throw new InvalidOperationException("Jwt:Secret must be configured and at least 32 characters long.");
}

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

builder.Services.AddAuthorization(options => options.AddTournamentPlatformPolicies());

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
app.MapGet("/", () => Results.Ok(new { Service = "Tournament.Api", Status = "Running" }));
app.MapControllers();

if (app.Configuration.GetValue<bool>("ApplyMigrations"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<TournamentDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();
