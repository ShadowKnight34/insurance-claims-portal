using System.Text;
using System.Text.Json.Serialization;
using ClaimsPortal.Claims.Api.Infrastructure;
using ClaimsPortal.Claims.Api.Security;
using ClaimsPortal.Claims.Application.Abstractions;
using ClaimsPortal.Claims.Application.Services;
using ClaimsPortal.Claims.Infrastructure;
using ClaimsPortal.Claims.Infrastructure.Persistence;
using ClaimsPortal.Claims.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Load User Secrets in Development (or when present)
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ClaimService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddClaimsInfrastructure(builder.Configuration, builder.Environment);

// JWT configuration
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.Name
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Claims Portal Claims API",
        Version = "v1",
        Description = "Claims intake, assessment and dashboard API for the Claims Portal demo."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the access token returned by POST /api/auth/login."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Claims Portal Claims API"));
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Optional migration/seeding flags (default false)
bool runMigrations = builder.Configuration.GetValue<bool>("Database:RunMigrations");
bool runSeeding = builder.Configuration.GetValue<bool>("Database:RunSeeding");

if (app.Environment.IsDevelopment())
{
    if (runMigrations)
    {
        using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();
        await db.Database.MigrateAsync();
    }
    if (runSeeding)
    {
        using var scope = app.Services.CreateAsyncScope();
        await DemoDataSeeder.SeedAsync(scope.ServiceProvider);
    }
}

app.Run();

public partial class Program;
