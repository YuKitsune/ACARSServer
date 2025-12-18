using ACARSServer.Clients;
using ACARSServer.Data;
using ACARSServer.Hubs;
using ACARSServer.Infrastructure;
using ACARSServer.Model;
using ACARSServer.Services;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;

// Load .env file in development only
if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
{
    // Search for .env file in current directory and parent directories up to git root or filesystem root
    var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (currentDir != null)
    {
        var envPath = Path.Combine(currentDir.FullName, ".env");
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
            break;
        }

        // Stop at git root
        if (Directory.Exists(Path.Combine(currentDir.FullName, ".git")))
        {
            break;
        }

        currentDir = currentDir.Parent;
    }
}

var builder = WebApplication.CreateBuilder(args);

// Add environment variables to configuration (loaded from .env)
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=acars.db"));

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IControllerManager, ControllerManager>();
builder.Services.AddSingleton<ClientManager>();
builder.Services.AddSingleton<IClientManager>(sp => sp.GetRequiredService<ClientManager>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<ClientManager>());
builder.Services.AddSingleton<IApiKeyValidator, ApiKeyValidator>();
builder.Services.AddSingleton<IStatisticsService, StatisticsService>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddSignalR();
builder.Services.AddRazorPages();
builder.Services.AddOpenApi();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapHub<ControllerHub>("/hubs/controller");
app.MapHub<PublicStatsHub>("/hubs/public-stats");

app.Run();