using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using photo_history_server.Application.Photos;
using photo_history_server.Application.Users;
using photo_history_server.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ---------- Services ----------

// Controllers
builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core – SQL Server Express
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

// CORS – development only policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Application services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SystemRegistrationService>();
builder.Services.AddScoped<AdminRegistrationService>();
builder.Services.AddScoped<AdminUserService>();
builder.Services.AddScoped<PhotoService>();

var app = builder.Build();

// Automatyczna inicjalizacja schematu DB przy starcie — tylko migracje przyrostowe.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var db = services.GetRequiredService<AppDbContext>();

    try
    {
        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count > 0)
        {
            logger.LogInformation("Applying {Count} pending migration(s): {Migrations}",
                pending.Count, string.Join(", ", pending));
            await db.Database.MigrateAsync();
            logger.LogInformation("EF Core migrations applied successfully.");
        }
        else
        {
            logger.LogInformation("Database is up to date — no pending migrations.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed during startup.");
        throw;
    }
}

// ---------- Middleware pipeline ----------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Serve uploaded photos as static files under /uploads
var photosPath = builder.Configuration["Storage:PhotosPath"]!;
Directory.CreateDirectory(photosPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(photosPath),
    RequestPath = "/uploads"
});

app.UseCors("DevCors");

app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();

app.Run();
