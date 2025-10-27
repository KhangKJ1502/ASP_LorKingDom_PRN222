using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
    [Route("Home")]
    public class CheckoutController : Controller
    {
        [HttpGet("Checkout")]
        public IActionResult Index()
        {
            return View("~/Views/Home/Checkout.cshtml");
        }
    }
}
