using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace MontanaMesh.Web.Services;

/// <summary>
/// Limits contact-form sends per client network over a fixed window. Only submissions that
/// pass validation reach it. A failed delivery returns its send permit, so an SMTP outage
/// does not lock people out, while a separate attempt cap still bounds SMTP connections.
/// </summary>
public sealed class ContactSubmissionLimiter
{
    internal const int SendLimit = 3;
    internal const int AttemptLimit = 10;
    internal static readonly TimeSpan Window = TimeSpan.FromMinutes(10);
    private const int Ipv6PrefixBytes = 8;
    private const int PruneThreshold = 1024;

    private readonly ConcurrentDictionary<string, Counter> counters = new();

    /// <summary>Reserves a send for this client, or returns null when its window is used up.</summary>
    public Lease? TryAcquire(IPAddress? address)
    {
        var now = DateTimeOffset.UtcNow;
        if (counters.Count > PruneThreshold) Prune(now);

        var key = PartitionKey(address);
        var counter = counters.GetOrAdd(key, _ => new Counter(now));
        lock (counter)
        {
            if (now - counter.WindowStart >= Window) counter.Reset(now);
            if (counter.Sends >= SendLimit || counter.Attempts >= AttemptLimit) return null;
            counter.Sends++;
            counter.Attempts++;
            return new Lease(counter, counter.WindowStart);
        }
    }

    // IPv6 clients usually control a whole /64, so group them by that prefix.
    internal static string PartitionKey(IPAddress? address)
    {
        if (address is null) return "unknown";
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (address.AddressFamily != AddressFamily.InterNetworkV6) return address.ToString();

        var prefix = address.GetAddressBytes()[..Ipv6PrefixBytes];
        return $"{Convert.ToHexString(prefix)}::/64";
    }

    private void Prune(DateTimeOffset now)
    {
        foreach (var (key, counter) in counters)
        {
            bool expired;
            lock (counter) expired = now - counter.WindowStart >= Window;
            if (expired) counters.TryRemove(new KeyValuePair<string, Counter>(key, counter));
        }
    }

    internal sealed class Counter(DateTimeOffset windowStart)
    {
        public DateTimeOffset WindowStart { get; private set; } = windowStart;
        public int Sends { get; set; }
        public int Attempts { get; set; }

        public void Reset(DateTimeOffset now)
        {
            WindowStart = now;
            Sends = 0;
            Attempts = 0;
        }
    }

    /// <summary>A reserved send. Call <see cref="Release"/> when delivery fails.</summary>
    public sealed class Lease
    {
        private readonly Counter counter;
        private readonly DateTimeOffset windowStart;
        private bool released;

        internal Lease(Counter counter, DateTimeOffset windowStart)
        {
            this.counter = counter;
            this.windowStart = windowStart;
        }

        public void Release()
        {
            lock (counter)
            {
                if (released || counter.WindowStart != windowStart || counter.Sends == 0) return;
                counter.Sends--;
                released = true;
            }
        }
    }
}
