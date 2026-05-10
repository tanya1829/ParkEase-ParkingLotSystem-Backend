using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ParkEase.SpotService.Data;
using ParkEase.SpotService.Interfaces;
using ParkEase.SpotService.Repositories;
using ParkEase.SpotService.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── PostgreSQL + EF Core ─────────────────────────────────────────────────────
// Spot Service uses its own database: parkease_spots
builder.Services.AddDbContext<SpotDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── Dependency Injection ─────────────────────────────────────────────────────
builder.Services.AddScoped<ISpotRepository, SpotRepository>();
builder.Services.AddScoped<ISpotService, SpotService>();

// ─── JWT Authentication (same secret as Auth Service) ────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// ─── Swagger with Annotations ─────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ParkEase — Spot Service",
        Version = "v1",
        Description = "Parking Spot Management API — handles individual spot CRUD, status transitions, and filtering."
    });

    c.EnableAnnotations();

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter your JWT token: Bearer eyJhbGci...",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                    { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ─── CORS ─────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// ─── Auto-migrate on startup ──────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SpotDbContext>();
    db.Database.Migrate();
}

// ─── Middleware Pipeline ──────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ParkEase Spot Service v1");
    c.RoutePrefix = string.Empty;
});

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Service = "spot-service",
    Database = "parkease_spots",
    Port = 5003
})).ExcludeFromDescription();

app.Run();
