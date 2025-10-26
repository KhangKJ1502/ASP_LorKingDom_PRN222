using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers;

public class ChatController : Controller
{
    // Demo page (optional)
    public IActionResult Demo() => View();

    // Customer chat widget page
    public IActionResult Customer(string? userId, string? name)
    {
        // Lấy từ cookie nếu không truyền vào
        userId ??= Request.Cookies["customerId"];
        name ??= Request.Cookies["customerName"];

        if (string.IsNullOrWhiteSpace(userId))
        {
            userId = $"customer_{Guid.NewGuid():N}";
            Response.Cookies.Append("customerId", userId, new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(7) });
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Khách hàng";
            Response.Cookies.Append("customerName", name, new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(7) });
        }

        ViewBag.UserId = userId;
        ViewBag.Name = name;

        return View();
    }

    // Staff chat dashboard page
    public IActionResult Staff(string? userId, string? name)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            // thử lấy từ cookie
            userId = Request.Cookies["staffId"];
        }
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("Staff ID không được để trống");

        if (string.IsNullOrWhiteSpace(name))
        {
            name = Request.Cookies["staffName"] ?? "Nhân viên";
        }

        // Lưu cookie 7 ngày
        Response.Cookies.Append("staffId", userId, new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(7) });
        Response.Cookies.Append("staffName", name, new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(7) });

        ViewBag.UserId = userId;
        ViewBag.Name = name;

        return View();
    }
}
