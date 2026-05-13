namespace MontanaMesh.Web.Models;

public sealed class NodeStatsResponse
{
    public string TotalNodes { get; init; } = "0";
    public string Nodes30Min { get; init; } = "0";
    public string Nodes2Hr { get; init; } = "0";
    public string Nodes24Hr { get; init; } = "0";
    public string UpdatedAtUtc { get; init; } = "Not yet updated";
}
