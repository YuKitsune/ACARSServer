using System.Security.Cryptography;
using System.Text;

namespace ACARSServer.Infrastructure;

public static class ApiKeyHasher
{
    public static string HashApiKey(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToBase64String(hashBytes);
    }
}
