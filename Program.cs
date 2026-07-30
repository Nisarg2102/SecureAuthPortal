using Microsoft.EntityFrameworkCore;
using SecureAuthPortal.Data;
using SecureAuthPortal.Models;
using BCrypt.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<DapperContext>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseNpgsql(
builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<SecureAuthPortal.Services.ILogService, SecureAuthPortal.Services.LogService>();

// Add Authentication
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

// Use distributed memory cache for session state which is better for hosting
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Seed default data.
await app.SeedDataAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();

app.UseAuthorization();

// Error logging middleware placed AFTER UseSession so session is available
app.UseMiddleware<SecureAuthPortal.Middleware.ErrorLoggingMiddleware>();

app.Use(async (context, next) =>
{
    // Caching Headers
    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "0";

    // Anti-XSS and Security Headers
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["Content-Security-Policy"] = 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://www.google.com/recaptcha/ https://www.gstatic.com/recaptcha/ code.jquery.com cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' cdnjs.cloudflare.com fonts.googleapis.com cdn.jsdelivr.net; " +
        "font-src 'self' data: cdnjs.cloudflare.com fonts.gstatic.com cdn.jsdelivr.net; " +
        "img-src 'self' data: blob:; " +
        "connect-src 'self' https://www.google.com/recaptcha/; " +
        "frame-src 'self' https://www.google.com/recaptcha/ https://recaptcha.google.com/recaptcha/ data:; " +
        "object-src 'none';";

    await next();
});

app.MapAreaControllerRoute(
    name: "admin",
    areaName: "Admin",
    pattern: "Admin/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");
app.Run();

public static class DataSeedingExtension
{
    public static async Task SeedDataAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // ✅ DATA FIX: Ensure all existing users are active (migration defaulted to false)
        await context.Database.ExecuteSqlRawAsync(
            "UPDATE \"UserMaster\" SET \"IsActive\" = TRUE WHERE \"IsActive\" = FALSE");

        // Seed roles
        if (!await context.RoleMaster.AnyAsync())
        {
            logger.LogInformation("Seeding roles...");
            context.RoleMaster.AddRange(
                new RoleMaster { RoleName = "Admin", Description = "Administrator with full access", IsActive = true, CreatedDate = DateTime.Now },
                new RoleMaster { RoleName = "User", Description = "Regular user with limited access", IsActive = true, CreatedDate = DateTime.Now },
                new RoleMaster { RoleName = "Manager", Description = "Manager with user management access", IsActive = true, CreatedDate = DateTime.Now }
            );
            await context.SaveChangesAsync();
        }

        // Seed test user if no users exist
        if (!await context.UserMaster.AnyAsync())
        {
            logger.LogInformation("Seeding default admin user...");
            var adminRole = await context.RoleMaster.FirstOrDefaultAsync(r => r.RoleName == "Admin");
            if (adminRole != null)
            {
                context.UserMaster.Add(new UserMaster
                {
                    FullName = "Admin User",
                    Username = "admin",
                    Password = BCrypt.Net.BCrypt.HashPassword(configuration["DefaultAdminPassword"] ?? "admin123"),
                    EmailId = "admin@example.com",
                    MobileNo = "9876543210",
                    DOB = new DateTime(1990, 1, 1),
                    Gender = "Male",
                    RoleId = adminRole.RoleId,
                    CreatedDate = DateTime.Now
                });
                await context.SaveChangesAsync();
            }
        }
    }
}