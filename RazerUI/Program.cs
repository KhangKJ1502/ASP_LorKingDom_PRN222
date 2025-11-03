using BLL;
using DAL;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

namespace RazerUI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ===== Config sources =====
            builder.Configuration
                .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<Program>(optional: true)
                .AddEnvironmentVariables();

            var conn = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");

            // ===== SHARED DATA PROTECTION (để share cookie với WebUI) =====
            var dataProtectionPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "shared-keys");
            Directory.CreateDirectory(dataProtectionPath);

            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
                .SetApplicationName("LorKingDom");

            // ===== CORS để 2 projects có thể communicate =====
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowWebUI", policy =>
                {
                    policy.WithOrigins(
                        "https://localhost:7777",
                        "http://localhost:4444"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
                });
            });

            // ===== DI: DAL / BLL =====
            builder.Services.AddDAL(conn);
            builder.Services.AddBLL();

            // Add services to the container.
            builder.Services.AddRazorPages();

            // Session
            builder.Services.AddSession(o =>
            {
                o.IdleTimeout = TimeSpan.FromMinutes(30);
                o.Cookie.HttpOnly = true;
                o.Cookie.IsEssential = true;
                o.Cookie.Name = "RazorUI.Session";
                o.Cookie.Domain = null; // Allow cookie sharing
                o.Cookie.SameSite = SameSiteMode.Lax;
            });

            // Anti-Forgery (khớp header JS)
            builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");

            // Auth (Admin Scheme) - SHARED với WebUI
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie("AdminScheme", o =>
                {
                    o.LoginPath = "/AdminAuth/Login";
                    // Dùng relative path (phải bắt đầu bằng /)
                    o.LogoutPath = "/AdminAuth/Logout";
                    o.AccessDeniedPath = "/AdminAuth/AccessDenied";
                    o.Cookie.HttpOnly = true;
                    o.Cookie.IsEssential = true;
                    o.Cookie.Name = "AdminAuth"; // SAME cookie name as WebUI
                    o.Cookie.Domain = null; // Allow localhost sharing
                    o.Cookie.Path = "/"; // Cookie available for all paths
                    o.Cookie.SameSite = SameSiteMode.Lax;
                    o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    o.ExpireTimeSpan = TimeSpan.FromDays(7);
                    o.SlidingExpiration = false;

                    // Event handler để redirect Logout/AccessDenied sang WebUI
                    o.Events = new CookieAuthenticationEvents
                    {
                        OnRedirectToLogout = context =>
                        {
                            context.Response.Redirect("https://localhost:7777/AdminAuth/Logout");
                            return Task.CompletedTask;
                        },
                        OnRedirectToAccessDenied = context =>
                        {
                            context.Response.Redirect("https://localhost:7777/AdminAuth/AccessDenied");
                            return Task.CompletedTask;
                        }
                    };
                });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // CORS - phải đặt trước Authentication
            app.UseCors("AllowWebUI");

            // Session nên đặt sau Routing
            app.UseSession();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorPages();

            app.Run();
        }
    }
}
