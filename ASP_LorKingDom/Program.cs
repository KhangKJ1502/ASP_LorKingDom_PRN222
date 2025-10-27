// Program.cs (ASP.NET Core 8)
using BLL;
using BLL.Interfaces;
using BLL.Services;
using DAL;
using DAL.Interfaces;
using DAL.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using WebUI.BackgroundServices;
using WebUI.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ===== Config sources =====
builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables();

builder.Services.AddHostedService<NotificationWorkerService>();

var conn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");

// ===== DI: DAL / BLL / MVC =====
builder.Services.AddDAL(conn);
builder.Services.AddBLL();
builder.Services.AddControllersWithViews();

// Anti-Forgery (khớp header JS)
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");

// (Chỉ giữ nếu AddDAL/AddBLL CHƯA đăng ký 2 service này)
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<IAddressService, AddressService>();

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
        o.AccessDeniedPath = "/Home/Error";
        o.Cookie.HttpOnly = true;
        o.Cookie.IsEssential = true;
        o.ExpireTimeSpan = TimeSpan.FromDays(7);
        o.SlidingExpiration = false;
    })
    .AddCookie("AdminScheme", o =>
    {
        o.LoginPath = "/AdminAuth/Login"; // Admin login
        o.LogoutPath = "/AdminAuth/Logout";
        o.AccessDeniedPath = "/AdminAuth/AccessDenied";
        o.Cookie.HttpOnly = true;
        o.Cookie.IsEssential = true;
        o.ExpireTimeSpan = TimeSpan.FromDays(7);
        o.SlidingExpiration = false;
        o.Cookie.Name = "AdminAuth"; // Cookie riêng cho admin
    }); 

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

var app = builder.Build();

// ===== Pipeline =====
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Session nên đặt sau Routing
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
