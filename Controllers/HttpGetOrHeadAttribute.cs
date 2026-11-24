using Microsoft.AspNetCore.Mvc.Routing;

namespace MontanaMesh.Web.Controllers;

/// <summary>
/// Routes GET and HEAD to the same action so link checkers and uptime probes that use
/// HEAD see the page's real status. Kestrel omits the response body for HEAD.
/// </summary>
public sealed class HttpGetOrHeadAttribute(string template)
    : HttpMethodAttribute(["GET", "HEAD"], template);
