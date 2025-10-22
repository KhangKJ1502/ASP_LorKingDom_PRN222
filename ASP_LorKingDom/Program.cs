// Program.cs (ASP.NET Core 8)
using BLL; // AddBLL()
using DAL;
using WebUI.Hubs; // AddDAL()
using Microsoft.AspNetCore.Authentication.Cookies;
using WebUI.BackgroundServices; // AddDAL()
using DAL.Interfaces;
using DAL.Repositories;
using BLL.Interfaces;
using BLL.Services;

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

//ChatHUb
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login"; // Đường dẫn đến trang đăng nhập
        options.LogoutPath = "/Auth/Logout"; // Đường dẫn đăng xuất
        options.AccessDeniedPath = "/Home/Error"; // Trang lỗi khi không có quyền
        options.ExpireTimeSpan = TimeSpan.FromDays(7); // Cookie hết hạn sau 7 ngày
        options.SlidingExpiration = true; // Gia hạn cookie nếu hoạt động
    });

// Thêm DI cho Cart và Product (giả định bạn đã có IProductService và ProductService trong BLL; nếu không, bỏ dòng đó)
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IProductService, ProductService>(); // Nếu có IProductService

var app = builder.Build();

// 4) Pipeline mặc định
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