// Program.cs (ASP.NET Core 8)
using BLL; // AddBLL()
using DAL; // AddDAL()

var builder = WebApplication.CreateBuilder(args);

// 1) Nạp cấu hình theo lớp (không bắt buộc Local/UserSecrets nhưng nên có khi làm team)
builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true) // gitignore
    .AddUserSecrets<Program>(optional: true)                                     // mỗi dev tự set
    .AddEnvironmentVariables();

// 2) Lấy connection string
var conn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");

// 3) Đăng ký DI: DAL/BLL + MVC
builder.Services.AddDAL(conn);
builder.Services.AddBLL();
builder.Services.AddControllersWithViews();

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
app.UseAuthorization();

// 5) Map cả attribute-routed controllers (ví dụ /health/db) lẫn conventional route
app.MapControllers(); // để các controller có [Route] hoạt động (HealthController)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
