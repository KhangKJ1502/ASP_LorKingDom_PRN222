// Program.cs (ASP.NET Core 8)
using BLL; // AddBLL()
using BLL.Interfaces;
using BLL.Services;
using DAL;
using DAL.Interfaces;
using DAL.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using WebUI.BackgroundServices; // AddDAL()
using WebUI.Hubs; // AddDAL()

var builder = WebApplication.CreateBuilder(args);

// 1) Nạp cấu hình theo lớp (không bắt buộc Local/UserSecrets nhưng nên có khi làm team)
builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true) // gitignore
    .AddUserSecrets<Program>(optional: true)                                     // mỗi dev tự set
    .AddEnvironmentVariables();
builder.Services.AddHostedService<NotificationWorkerService>();
// 2) Lấy connection string
var conn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");

// 3) Đăng ký DI: DAL/BLL + MVC
builder.Services.AddDAL(conn);
builder.Services.AddBLL();
builder.Services.AddControllersWithViews();

// Thêm Session (bắt buộc cho SignupEmail, SignupPassword)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

//ChatHUb
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// Authentication với 2 scheme: Customer và Admin
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Auth/Login"; // Customer login
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Home/Error";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = false;
    })
    .AddCookie("AdminScheme", options =>
    {
        options.LoginPath = "/AdminAuth/Login"; // Admin login
        options.LogoutPath = "/AdminAuth/Logout";
        options.AccessDeniedPath = "/AdminAuth/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = false;
        options.Cookie.Name = "AdminAuth"; // Cookie riêng cho admin
    });

// Thêm DI cho Cart và Product (giả định bạn đã có IProductService và ProductService trong BLL; nếu không, bỏ dòng đó)
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IProductService, ProductService>(); // Nếu có IProductService

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

var app = builder.Build();

// 4) Pipeline mặc định
app.UseSession(); // Thêm Session middleware

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.MapHub<ChatHub>("/chatHub");

// 5) Map cả attribute-routed controllers (ví dụ /health/db) lẫn conventional route
app.MapControllers(); // để các controller có [Route] hoạt động (HealthController)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();