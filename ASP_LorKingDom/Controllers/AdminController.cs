using Microsoft.AspNetCore.Mvc;

namespace ASP_LorKingDom.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Dashboard()
        {
            return View(); // tìm Views/Admin/Dashboard.cshtml
        }
    }
}