using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SunriseClinic.Data;

var builder = WebApplication.CreateBuilder(args);

// Get connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

// Services
builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    // Default session 30 দিন রাখুন, কিন্তু আমরা controller এ manage করব
    options.IdleTimeout = TimeSpan.FromDays(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.MaxAge = TimeSpan.FromDays(30); // Maximum 30 days
});

// Database Context - WITH RETRY POLICY
builder.Services.AddDbContext<SunriseDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
    }));

// Authentication
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30); // Maximum expiry

        // Cookie events
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                // Remember Me চেক করার logic
                var rememberMeClaim = context.Principal?.FindFirst("RememberMe");
                if (rememberMeClaim != null && rememberMeClaim.Value == "true")
                {
                    // 30 দিনের জন্য renew করুন
                    context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30);
                    context.ShouldRenew = true;
                }
                else
                {
                    // 24 ঘণ্টার জন্য renew করুন
                    context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24);
                    context.ShouldRenew = true;
                }
            }
        };
    });

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ========== MINIMAL DATABASE HEALTH CHECK ==========
// Non-blocking, runs in background
_ = Task.Run(async () =>
{
    await Task.Delay(2000); // Wait 2 seconds for app to initialize

    try
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SunriseDbContext>();

        // Quick test - just get server version
        var serverVersion = dbContext.Database.GetDbConnection().ServerVersion;
        Console.WriteLine($"✅ Database Connected - Server: {serverVersion?.Split('\n').FirstOrDefault()}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Database Warning: {ex.Message}");
        Console.WriteLine("ℹ️ Application will continue, but database operations may fail.");
    }
});
// ===================================================

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// ==================== ROUTES ====================
app.MapControllerRoute(
    name: "account",
    pattern: "Account/{action}/{id?}",
    defaults: new { controller = "Account" });

app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{action=Dashboard}/{id?}",
    defaults: new { controller = "Admin" });

app.MapControllerRoute(
    name: "patient",
    pattern: "Patient/{action=Dashboard}/{id?}",
    defaults: new { controller = "Patient" });

app.MapControllerRoute(
    name: "appointment",
    pattern: "Appointment/{action=Create}/{id?}",
    defaults: new { controller = "Appointment" });

app.MapControllerRoute(
    name: "doctors",
    pattern: "Doctors/{action=Index}/{id?}",
    defaults: new { controller = "Doctors" });

app.MapControllerRoute(
    name: "nurse",
    pattern: "Nurse/{action=Dashboard}/{id?}",
    defaults: new { controller = "Nurse" });

app.MapControllerRoute(
    name: "quickAppointment",
    pattern: "QuickAppointment/{action}/{code?}",
    defaults: new { controller = "QuickAppointment" });

app.MapControllerRoute(
    name: "developer",
    pattern: "DevelopedBy/",
    defaults: new { controller = "Home", action = "Developer" });

app.MapControllerRoute(
    name: "sitemap",
    pattern: "sitemap.xml",
    defaults: new { controller = "Sitemap", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
// ================================================

// Global error handling middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        // Database connection errors handle
        if (ex is Microsoft.Data.SqlClient.SqlException ||
            ex is System.Net.Sockets.SocketException ||
            ex.Message.Contains("database", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"🔴 Database Error: {ex.Message}");

            // Session e error
            context.Session.SetString("DatabaseError", ex.Message);

            // DatabaseError page
            context.Response.Redirect("/Home/DatabaseError");
            return;
        }
        throw;
    }
});

Console.WriteLine("🌅 Sunrise Clinic Application Started");
app.Run();