using Microsoft.AspNetCore.Mvc;
using MontanaMesh.Web.Models;
using System.Text.Json;

namespace MontanaMesh.Web.Controllers;

[ApiController]
[Route("api/nodes")]
public class NodesController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<NodesController> _logger;

    public NodesController(IWebHostEnvironment environment, ILogger<NodesController> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    [HttpGet("stats")]
    public ActionResult<NodeStatsResponse> GetStats()
    {
        var statsFilePath = Path.Combine(_environment.ContentRootPath, "data", "node-stats.json");
        if (!System.IO.File.Exists(statsFilePath))
        {
            return Ok(new NodeStatsResponse());
        }

        try
        {
            var json = System.IO.File.ReadAllText(statsFilePath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return Ok(new NodeStatsResponse
            {
                TotalNodes = GetValue(root, "totalNodes", "TotalNodes", "0"),
                Nodes30Min = GetValue(root, "nodes30Min", "Nodes30Min", "0"),
                Nodes2Hr = GetValue(root, "nodes2Hr", "Nodes2Hr", "0"),
                Nodes24Hr = GetValue(root, "nodes24Hr", "Nodes24Hr", "0"),
                UpdatedAtUtc = GetValue(root, "updatedAtUtc", "UpdatedAtUtc", "Not yet updated")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read node stats from {StatsFilePath}", statsFilePath);
            return Ok(new NodeStatsResponse());
        }
    }

    private static string GetValue(JsonElement root, string camelName, string pascalName, string fallback)
    {
        if (root.TryGetProperty(camelName, out var camelValue))
        {
            return camelValue.ValueKind == JsonValueKind.String ? camelValue.GetString() ?? fallback : camelValue.ToString();
        }

        if (root.TryGetProperty(pascalName, out var pascalValue))
        {
            return pascalValue.ValueKind == JsonValueKind.String ? pascalValue.GetString() ?? fallback : pascalValue.ToString();
        }

        return fallback;
    }
}
