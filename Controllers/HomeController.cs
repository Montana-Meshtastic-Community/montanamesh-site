using Microsoft.AspNetCore.Mvc;

namespace MontanaMesh.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        ViewData["BackgroundKey"] = "billings";
        return View();
    }

    [HttpGet("/setup")]
    public IActionResult Setup()
    {
        ViewData["BackgroundKey"] = "missoula";
        return View("Setup");
    }

    [HttpGet("/connect")]
    public IActionResult Connect()
    {
        ViewData["BackgroundKey"] = "great-falls";
        return View("Connect");
    }

    [HttpGet("/recommended-configuration-settings")]
    public IActionResult RecommendedConfigurationSettings()
    {
        ViewData["BackgroundKey"] = "bozeman";
        return View("RecommendedConfigurationSettings");
    }

    [HttpGet("/resources")]
    public IActionResult Resources()
    {
        ViewData["BackgroundKey"] = "butte";
        return View("Resources");
    }
}
