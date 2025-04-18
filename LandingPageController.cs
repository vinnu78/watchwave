using Microsoft.AspNetCore.Mvc;
using WatchWave.Models.Repo;

namespace WatchWave.Controllers
{
    public class LandingPageController : Controller
    {
        public IActionResult LandingPage()
        {
            return View();
        }
        
    }
}
