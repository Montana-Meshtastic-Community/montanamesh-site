namespace MontanaMesh.Web.Models;

public sealed record RadioProfile(string Role, int HopLimit, string PositionSetting)
{
    public const string Region = "US";
    public const string ModemPreset = "LONG_FAST";
    public const int NodeInfoSeconds = 10800;
    public const string QuietClientRole = "CLIENT_MUTE";
    public const string LowerPriorityRouterRole = "ROUTER_LATE";

    private const string FixedPosition = "position.fixed_position true";
    private const int FixedNodeHopLimit = 5;

    public static RadioProfile Portable { get; } = new("CLIENT", 3, "position.position_broadcast_smart_enabled true");
    public static RadioProfile BaseStation { get; } = new("CLIENT_BASE", FixedNodeHopLimit, FixedPosition);
    public static RadioProfile Router { get; } = new("ROUTER", FixedNodeHopLimit, FixedPosition);

    public string HopSetting => $"lora.hop_limit {HopLimit}";
    public string RoleSetting => $"device.role {Role}";

    public string[] SharedMeshSettings =>
    [
        $"lora.region {Region}",
        $"lora.modem_preset {ModemPreset}",
        HopSetting,
        RoleSetting,
        $"device.node_info_broadcast_secs {NodeInfoSeconds}"
    ];

    public string FullCommand => Command([.. SharedMeshSettings, PositionSetting]);

    public static string Command(params string[] settings) =>
        string.Join(" \\\n", new[] { "meshtastic" }.Concat(settings.Select(setting => $"  --set {setting}")));
}
