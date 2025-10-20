using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
    public class BlogController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Detail()
        {
            return View();
        }
    }
}
