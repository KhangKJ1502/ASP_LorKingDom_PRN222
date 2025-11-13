// Program.cs (ASP.NET Core 8)
using BLL;
using BLL.Interfaces;
using BLL.Services;
using DAL;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using WebUI.BackgroundServices;
using WebUI.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ===== Config sources =====
builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables();

// Background Workers
builder.Services.AddHostedService<NotificationWorkerService>();
builder.Services.AddHostedService<WebUI.Workers.PromotionWorkerService>();

var conn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");

// ===== SHARED DATA PROTECTION (để share cookie với RazorUI) =====
var dataProtectionPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "shared-keys");
Directory.CreateDirectory(dataProtectionPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("LorKingDom");

// ===== CORS để 2 projects có thể communicate =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowRazorUI", policy =>
    {
        policy.WithOrigins(
            "https://localhost:7226",
            "http://localhost:5177"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// ===== DI: DAL / BLL / MVC =====
builder.Services.AddDAL(conn);
builder.Services.AddBLL();
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IAvatarService, AvatarService>();


// Anti-Forgery (khớp header JS)
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");


// Session
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(30);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

// SignalR
builder.Services.AddSignalR();

// Auth (Customer + Admin)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, o =>
    {
        o.LoginPath = "/Auth/Login";
        o.LogoutPath = "/Auth/Logout";
        o.Cookie.HttpOnly = true;
        o.Cookie.IsEssential = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        o.ExpireTimeSpan = TimeSpan.FromDays(7);
        o.SlidingExpiration = false;
    })
    .AddCookie("AdminScheme", o =>
    {
        // Redirect admin login sang RazorUI
        o.LoginPath = "/AdminAuth/RedirectToRazorUI";
        o.LogoutPath = "/AdminAuth/Logout";
        o.AccessDeniedPath = "/AdminAuth/AccessDenied";
        o.Cookie.HttpOnly = true;
        o.Cookie.IsEssential = true;
        o.Cookie.Name = "AdminAuth"; // SHARED cookie name với RazorUI
        o.Cookie.Domain = null; // cho phép share giữa localhost:7777 và localhost:7226
        o.Cookie.Path = "/"; // Cookie available cho tất cả path
        o.Cookie.SameSite = SameSiteMode.Lax; // Gửi cookie khi điều hướng giữa domain cùng nguồn gốc
        o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Nếu request dùng HTTPS → cookie chỉ gửi qua HTTPS
        o.ExpireTimeSpan = TimeSpan.FromDays(7);
        o.SlidingExpiration = false;

        // Events để trace cookie
        o.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = context =>
            {
                // Log cookie validation
                return Task.CompletedTask;
            }
        };
    });

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();

    var admin = await accountService.GetByEmailAsync("admin@gmail.com");
    if (admin == null)
    {
        await accountService.CreateAsync(new BLL.DTOs.AccountDto
        {
            AccountName = "Admin",
            Email = "admin@gmail.com",
            Password = "123456",
            RoleId = 1
        });
    }

    var staff = await accountService.GetByEmailAsync("staff@gmail.com");
    if (staff == null)
    {
        await accountService.CreateAsync(new BLL.DTOs.AccountDto
        {
            AccountName = "Staff",
            Email = "staff@gmail.com",
            Password = "123456",
            RoleId = 2
        });
    }

    var warehouse = await accountService.GetByEmailAsync("warehouse@gmail.com");
    if (warehouse == null)
    {
        await accountService.CreateAsync(new BLL.DTOs.AccountDto
        {
            AccountName = "Warehouse",
            Email = "warehouse@gmail.com",
            Password = "123456",
            RoleId = 3
        });
    }
}

// ===== Pipeline =====
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// CORS phải đứng trước Session để share cookie
app.UseCors("AllowRazorUI");

// Session nên đặt sau CORS
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<ChatHub>("/chatHub");

// Controller có [Route] + conventional
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
