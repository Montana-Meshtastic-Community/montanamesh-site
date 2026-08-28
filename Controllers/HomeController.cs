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

    [HttpGet("/devices")]
    public IActionResult DeviceGuide()
    {
        ViewData["BackgroundKey"] = "bozeman";
        return View("DeviceGuide");
    }

    [HttpGet("/host-a-node")]
    public IActionResult HostNode()
    {
        ViewData["BackgroundKey"] = "billings";
        return View("HostNode");
    }

    [HttpGet("/resources")]
    public IActionResult Resources()
    {
        ViewData["BackgroundKey"] = "butte";
        return View("Resources");
    }

    [HttpGet("/privacy")]
    public IActionResult Privacy()
    {
        ViewData["BackgroundKey"] = "great-falls";
        return View("Privacy");
    }

    [HttpGet("/resources/other-mesh-networks")]
    public IActionResult OtherMeshNetworks()
    {
        ViewData["BackgroundKey"] = "missoula";
        return View("OtherMeshNetworks");
    }
}
