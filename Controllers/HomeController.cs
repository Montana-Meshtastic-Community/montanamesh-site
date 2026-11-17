using Microsoft.AspNetCore.Mvc;
using MontanaMesh.Web.Models;
using System.Diagnostics;

namespace MontanaMesh.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();

    [HttpGet("/setup")]
    public IActionResult Setup() => View();

    [HttpGet("/connect")]
    public IActionResult Connect() => View();

    [HttpGet("/recommended-configuration-settings")]
    public IActionResult RecommendedConfigurationSettings() => View();

    [HttpGet("/optimal-settings")]
    public IActionResult OptimalSettings() => View();

    [HttpGet("/devices")]
    public IActionResult DeviceGuide() => View();

    [HttpGet("/host-a-node")]
    public IActionResult HostNode() => View();

    [HttpGet("/resources")]
    public IActionResult Resources() => View();

    [HttpGet("/privacy")]
    public IActionResult Privacy() => View();

    [HttpGet("/resources/other-mesh-networks")]
    public IActionResult OtherMeshNetworks() => View();

    [HttpGet("/error/{statusCode:int:range(400,599)}")]
    public IActionResult StatusCodePage(int statusCode)
    {
        Response.StatusCode = statusCode;
        return statusCode == StatusCodes.Status404NotFound ? View("NotFound") : Error();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
