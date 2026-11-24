using Microsoft.AspNetCore.Mvc;
using MontanaMesh.Web.Models;
using System.Diagnostics;

namespace MontanaMesh.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();

    [HttpGetOrHead("/setup")]
    public IActionResult Setup() => View();

    [HttpGetOrHead("/connect")]
    public IActionResult Connect() => View();

    [HttpGetOrHead("/recommended-configuration-settings")]
    public IActionResult RecommendedConfigurationSettings() => View();

    [HttpGetOrHead("/optimal-settings")]
    public IActionResult OptimalSettings() => View();

    [HttpGetOrHead("/devices")]
    public IActionResult DeviceGuide() => View();

    [HttpGetOrHead("/host-a-node")]
    public IActionResult HostNode() => View();

    [HttpGetOrHead("/resources")]
    public IActionResult Resources() => View();

    [HttpGetOrHead("/privacy")]
    public IActionResult Privacy() => View();

    [HttpGetOrHead("/resources/other-mesh-networks")]
    public IActionResult OtherMeshNetworks() => View();

    // Re-executed status pages keep the original method, so this route accepts any verb.
    [Route("/error/{statusCode:int:range(400,599)}")]
    public IActionResult StatusCodePage(int statusCode)
    {
        Response.StatusCode = statusCode;
        return statusCode == StatusCodes.Status404NotFound ? View("NotFound") : Error();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
