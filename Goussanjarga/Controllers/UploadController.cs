using Microsoft.AspNetCore.Mvc;

namespace Goussanjarga.Controllers
{
    public class UploadController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [ActionName("Create")]
        public IActionResult Create()
        {
            return View();
        }
    }
}