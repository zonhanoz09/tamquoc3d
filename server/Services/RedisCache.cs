using System.Net.Sockets;
using System.Text;

namespace TamQuoc3D.Server.Services;

/// <summary>
/// Minimal Redis cache client over raw RESP (same approach as the health probe:
/// no external package). Used ONLY as a read-through cache for profile and
/// campaign progress - PostgreSQL remains the source of truth.
///
/// Every operation is best-effort with short timeouts: when Redis is slow or
/// unreachable the call returns null/false and the caller falls back to the
/// database, so a Redis outage can never break gameplay endpoints.
/// </summary>
public sealed class RedisCache(IConfiguration configuration)
{
    private const int TimeoutMs = 400;

    private (string Host, int Port)? _endpoint;

    private (string Host, int Port)? Endpoint()
    {
        if (_endpoint.HasValue) return _endpoint;
        var redisUrl = configuration["REDIS_URL"];
        if (string.IsNullOrWhiteSpace(redisUrl) ||
            !Uri.TryCreate(redisUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }
        _endpoint = (uri.Host, uri.Port > 0 ? uri.Port : 6379);
        return _endpoint;
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken)
    {
        var response = await SendAsync(
            BuildCommand("GET", key), cancellationToken);
        if (response is null || response.Length == 0 || response[0] == '-') return null;
        // Bulk string: $<len>\r\n<data>
        if (response[0] != '$') return null;
        var headerEnd = response.IndexOf("\r\n", StringComparison.Ordinal);
        if (headerEnd < 0) return null;
        if (!int.TryParse(response.AsSpan(1, headerEnd - 1), out var length) || length < 0) return null;
        var start = headerEnd + 2;
        if (response.Length < start + length) return null;
        return response.Substring(start, length);
    }

    public async Task SetAsync(string key, string value, int ttlSeconds, CancellationToken cancellationToken)
    {
        await SendAsync(
            BuildCommand("SET", key, value, "EX", ttlSeconds.ToString()),
            cancellationToken);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        await SendAsync(BuildCommand("DEL", key), cancellationToken);
    }

    private static byte[] BuildCommand(params string[] args)
    {
        var builder = new StringBuilder();
        builder.Append($"*{args.Length}\r\n");
        foreach (var arg in args)
        {
            var bytes = Encoding.UTF8.GetBytes(arg);
            builder.Append($"${bytes.Length}\r\n{arg}\r\n");
        }
        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private async Task<string?> SendAsync(byte[] command, CancellationToken cancellationToken)
    {
        var endpoint = Endpoint();
        if (endpoint is null) return null;

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeoutMs);
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(endpoint.Value.Host, endpoint.Value.Port, timeout.Token);
            await using var stream = client.GetStream();
            await stream.WriteAsync(command, timeout.Token);
            var buffer = new byte[16384];
            var read = await stream.ReadAsync(buffer, timeout.Token);
            if (read <= 0) return null;
            // Large cached payloads may arrive in several reads; keep reading
            // until the bulk-string length is satisfied or the stream idles.
            var text = Encoding.UTF8.GetString(buffer, 0, read);
            while (text.Length > 0 && text[0] == '$')
            {
                var headerEnd = text.IndexOf("\r\n", StringComparison.Ordinal);
                if (headerEnd < 0) break;
                if (!int.TryParse(text.AsSpan(1, headerEnd - 1), out var length) || length < 0) break;
                if (text.Length >= headerEnd + 2 + length) break;
                read = await stream.ReadAsync(buffer, timeout.Token);
                if (read <= 0) break;
                text += Encoding.UTF8.GetString(buffer, 0, read);
            }
            return text;
        }
        catch (Exception exception) when (exception is SocketException or IOException or OperationCanceledException)
        {
            return null;
        }
    }
}
