using System.Net;

namespace MontanaMesh.Web.Services;

/// <summary>
/// Identifies connections that came through the published host port rather than straight
/// from another container on the shared Podman network.
/// </summary>
public static class TrustedProxy
{
    public static bool IsForwarder(ConnectionInfo connection) =>
        IsSameAddress(connection.RemoteIpAddress, connection.LocalIpAddress);

    internal static bool IsSameAddress(IPAddress? remote, IPAddress? local)
    {
        if (remote is null || local is null) return false;
        return Normalize(remote).Equals(Normalize(local));
    }

    private static IPAddress Normalize(IPAddress address) =>
        address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
}
