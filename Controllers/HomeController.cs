using Microsoft.AspNetCore.Mvc;
using MontanaMesh.Web.Models;
using System.Diagnostics;

namespace MontanaMesh.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Montana Meshtastic Community";
        ViewData["MetaDescription"] = "Join MontanaMesh, a volunteer Meshtastic community building practical off-grid LoRa messaging coverage around Billings and across Montana.";
        ViewData["BackgroundKey"] = "billings";
        return View();
    }

    [HttpGet("/setup")]
    public IActionResult Setup()
    {
        ViewData["MetaDescription"] = "See how MontanaMesh runs its public website, MQTT broker, MeshMonitor dashboard, PotatoMesh services, and private management stack.";
        ViewData["BackgroundKey"] = "missoula";
        return View("Setup");
    }

    [HttpGet("/connect")]
    public IActionResult Connect()
    {
        ViewData["MetaDescription"] = "Learn how to join MontanaMesh with a supported 915 MHz Meshtastic radio, the official app, and local recommended settings.";
        ViewData["BackgroundKey"] = "great-falls";
        return View("Connect");
    }

    [HttpGet("/recommended-configuration-settings")]
    public IActionResult RecommendedConfigurationSettings()
    {
        ViewData["MetaDescription"] = "Use MontanaMesh recommended Meshtastic settings for roles, hop limits, broadcast intervals, and clean shared-channel behavior.";
        ViewData["BackgroundKey"] = "bozeman";
        return View("RecommendedConfigurationSettings");
    }

    [HttpGet("/optimal-settings")]
    public IActionResult OptimalSettings()
    {
        ViewData["MetaDescription"] = "Compare MontanaMesh optimal Meshtastic settings for portable nodes, base stations, and planned router infrastructure.";
        ViewData["BackgroundKey"] = "bozeman";
        return View("OptimalSettings");
    }

    [HttpGet("/devices")]
    public IActionResult DeviceGuide()
    {
        ViewData["MetaDescription"] = "Choose a Meshtastic radio for MontanaMesh pocket carry, vehicle testing, rooftop coverage, solar relay sites, or standalone messaging.";
        ViewData["BackgroundKey"] = "bozeman";
        return View("DeviceGuide");
    }

    [HttpGet("/host-a-node")]
    public IActionResult HostNode()
    {
        ViewData["MetaDescription"] = "Host a MontanaMesh node from a window, roofline, shop, barn, hilltop, or remote site to improve local Meshtastic coverage.";
        ViewData["BackgroundKey"] = "billings";
        return View("HostNode");
    }

    [HttpGet("/resources")]
    public IActionResult Resources()
    {
        ViewData["MetaDescription"] = "Find MontanaMesh resources including Meshtastic docs, Discord, live node maps, device guidance, hosting info, and regional mesh groups.";
        ViewData["BackgroundKey"] = "butte";
        return View("Resources");
    }

    [HttpGet("/privacy")]
    public IActionResult Privacy()
    {
        ViewData["MetaDescription"] = "Review MontanaMesh privacy notes for website logs, Meshtastic radio traffic, public dashboards, and location sharing.";
        ViewData["BackgroundKey"] = "great-falls";
        return View("Privacy");
    }

    [HttpGet("/resources/other-mesh-networks")]
    public IActionResult OtherMeshNetworks()
    {
        ViewData["MetaDescription"] = "Discover nearby Montana Meshtastic groups and regional mesh networks connected to the broader off-grid radio community.";
        ViewData["BackgroundKey"] = "missoula";
        return View("OtherMeshNetworks");
    }

    [HttpGet("/error/{statusCode:int}")]
    public IActionResult StatusCodePage(int statusCode)
    {
        if (statusCode == StatusCodes.Status404NotFound)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            ViewData["Title"] = "Page Not Found";
            ViewData["MetaDescription"] = "The requested MontanaMesh page could not be found. Use the main routes to connect, choose a device, host a node, or open resources.";
            ViewData["BackgroundKey"] = "billings";
            return View("NotFound");
        }

        Response.StatusCode = statusCode;
        ViewData["Title"] = "Site Error";
        ViewData["MetaDescription"] = "MontanaMesh could not complete this request. Use the main site links to keep browsing.";
        ViewData["BackgroundKey"] = "billings";
        return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        ViewData["Title"] = "Site Error";
        ViewData["MetaDescription"] = "MontanaMesh could not complete this request. Use the main site links to keep browsing.";
        ViewData["BackgroundKey"] = "billings";
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
