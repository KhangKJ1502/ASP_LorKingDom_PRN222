using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
    public class BlogAdminController : Controller
    {
        [HttpGet]
        public IActionResult ManageBlog()
        {
            return View("~/Views/Admin/ManageBlog.cshtml");
        }

        [HttpGet]
        public IActionResult ManageBlogReview()
        {
            return View("~/Views/Admin/ManageBlogReview.cshtml");
        }
    }
}
