namespace ACARSServer.Services;

public interface IApiKeyValidator
{
    Task<ApiKeyValidationResult?> ValidateAsync(string apiKey);
}

public class ApiKeyValidationResult
{
    public required string VatsimCid { get; set; }
    public int ApiKeyId { get; set; }
}
