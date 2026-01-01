using ACARSServer.Clients;
using ACARSServer.Hubs;
using ACARSServer.Infrastructure;
using ACARSServer.Persistence;
using ACARSServer.Services;
using DotNetEnv;
using Serilog;
using Serilog.Events;

// TODO:
// - Introduce a Contracts project
// - Create a plugin branch with the same contracts
// - Local testing
// - Value types for IDs
// - Re-do dashboard
// - Authentication
// - Remove FlightSimulationNetwork variable

TryLoadEnvFile();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

ConfigureSerilog(builder);

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IControllerRepository, InMemoryControllerRepository>();
builder.Services.AddSingleton<ClientManager>();
builder.Services.AddSingleton<IClientManager>(sp => sp.GetRequiredService<ClientManager>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<ClientManager>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<MessageMonitorService>());
builder.Services.AddSingleton<IMessageIdProvider, MessageIdProvider>();
builder.Services.AddSingleton<IAircraftRepository, InMemoryAircraftRepository>();
builder.Services.AddSingleton<IDialogueRepository, InMemoryDialogueRepository>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddSignalR()
    .AddNewtonsoftJsonProtocol(options =>
    {
        options.PayloadSerializerSettings.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto;
    });
builder.Services.AddRazorPages();

builder.Services.AddHostedService<AircraftConnectionWatchdog>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapHub<ControllerHub>("/hubs/controller");

app.Run();
return;

void TryLoadEnvFile()
{
    // Only load .env file in development
    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Development")
        return;
    
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

void ConfigureSerilog(WebApplicationBuilder builder)
{
    if (!Enum.TryParse<LogEventLevel>(builder.Configuration["Logging:Level"], out var logLevel))
    {
        logLevel = LogEventLevel.Information;
    }

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Is(logLevel)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .CreateLogger();

    builder.Host.UseSerilog();

    builder.Services.AddSingleton(Log.Logger);
}