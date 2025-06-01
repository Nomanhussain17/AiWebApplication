using Microsoft.AspNetCore.Mvc;

namespace AichatBot3.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
