using Microsoft.AspNetCore.Mvc;

namespace WatchWave.Controllers
{
    public class AboutUsController : Controller
    {
        public IActionResult AboutUs()
        {
            return View();
        }
    }
}

