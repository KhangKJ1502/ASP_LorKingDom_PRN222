using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers;

public class ChatController : Controller
{
    public IActionResult Demo()
    {
        return View();
    }

    public IActionResult Customer(string? userId, string? name)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            userId = $"customer_{Guid.NewGuid():N}";
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Khách hàng";
        }

        ViewBag.UserId = userId;
        ViewBag.Name = name;

        return View();
    }

    public IActionResult Staff(string? userId, string? name)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest("Staff ID không được để trống");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Nhân viên";
        }

        ViewBag.UserId = userId;
        ViewBag.Name = name;

        return View();
    }
}