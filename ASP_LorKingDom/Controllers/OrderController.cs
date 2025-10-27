using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
    public class OrderController : Controller
    {
        [HttpGet("/Home/OrderDetails")]
        public IActionResult Index()
        {
            return View("~/Views/Home/OrderDetails.cshtml");
        }

        [HttpGet("/Order/Manage")]
        public IActionResult Manage()
        {
            return View("~/Views/Admin/ManageOrder.cshtml");
        }
    }
}
