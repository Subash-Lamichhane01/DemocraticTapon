using Microsoft.AspNetCore.Mvc;

namespace DemocraticTapON.Controllers
{
    public class AboutUsController : Controller
    {
        // Make sure this method returns the correct view (aboutus.cshtml)
        public IActionResult Index()
        {
            return View();  // This will return the /Views/AboutUs/aboutus.cshtml view
        }
    }
}
