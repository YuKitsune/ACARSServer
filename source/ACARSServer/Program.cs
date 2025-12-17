using ACARSServer.Clients;
using ACARSServer.Handlers;
using ACARSServer.Hubs;
using ACARSServer.Infrastructure;
using ACARSServer.Model;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IControllerManager, ControllerManager>();
builder.Services.AddSingleton<IClientManager, ClientManager>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddSignalR();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapHub<ControllerHub>("/hubs/controller");

app.Run();