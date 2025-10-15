using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
    public class ProductsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
