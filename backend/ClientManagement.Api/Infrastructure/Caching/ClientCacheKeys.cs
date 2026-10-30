
using System.Security.Cryptography;
using System.Text;

namespace ClientManagement.Api.Infrastructure.Caching;

public static class ClientCacheKeys
{
    public const string Version = "clients:cache:version";

    public static string List(
        string version,
        string? search,
        bool? isActive)
    {
        var normalizedSearch = (search ?? "")
            .Trim()
            .ToLowerInvariant();

        var searchHash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(normalizedSearch)));

        var status = isActive switch
        {
            true => "active",
            false => "inactive",
            null => "all"
        };

        return $"clients:list:{version}:{searchHash}:{status}";
    }

    public static string ById(string version, int id)
    {
        return $"clients:by-id:{version}:{id}";
    }
}
