using ACARSServer.Data;
using ACARSServer.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ACARSServer.Services;

public class ApiKeyValidator(IServiceScopeFactory scopeFactory) : IApiKeyValidator
{
    public async Task<ApiKeyValidationResult?> ValidateAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        var hashedKey = ApiKeyHasher.HashApiKey(apiKey);

        // Create a new scope to get the DbContext
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Compare the hashed key with stored hashes
        var key = await context.ApiKeys.FirstOrDefaultAsync(k => k.HashedKey == hashedKey);

        if (key == null)
            return null;

        return new ApiKeyValidationResult
        {
            VatsimCid = key.VatsimCid,
            ApiKeyId = key.Id
        };
    }
}
