using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using ACARSServer.Data;
using ACARSServer.Infrastructure;
using ACARSServer.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ACARSServer.Pages.ApiKeys;

public class RequestModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public RequestModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    [Required(ErrorMessage = "VATSIM CID is required")]
    [RegularExpression(@"^\d{6,7}$", ErrorMessage = "VATSIM CID must be 6-7 digits")]
    public string VatsimCid { get; set; } = string.Empty;

    public string? GeneratedKey { get; set; }
    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            // Generate a cryptographically secure API key
            var apiKey = GenerateApiKey();

            // Hash the API key before storing
            var hashedKey = ApiKeyHasher.HashApiKey(apiKey);

            // Save to database
            var apiKeyEntity = new ApiKey
            {
                VatsimCid = VatsimCid,
                HashedKey = hashedKey,
                CreatedAt = DateTime.UtcNow
            };

            _context.ApiKeys.Add(apiKeyEntity);
            await _context.SaveChangesAsync();

            // Return the plain text key to the user (this is the only time they'll see it)
            GeneratedKey = apiKey;
            return Page();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occurred while generating the API key: {ex.Message}";
            return Page();
        }
    }

    private static string GenerateApiKey()
    {
        // Generate a 32-byte random key and convert to base64
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }

        // Convert to URL-safe base64 string
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
