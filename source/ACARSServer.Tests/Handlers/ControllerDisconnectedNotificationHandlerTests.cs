using ACARSServer.Handlers;
using ACARSServer.Messages;
using ACARSServer.Model;
using ACARSServer.Tests.Mocks;
using Microsoft.Extensions.Logging.Abstractions;

namespace ACARSServer.Tests.Handlers;

public class ControllerDisconnectedNotificationHandlerTests
{
    [Fact]
    public async Task Handle_RemovesClientWhenNoControllersRemain()
    {
        // Arrange
        var controllerManager = new TestControllerManager();
        var clientManager = new TestClientManager();
        await clientManager.EnsureClientExists("VATSIM", "YBBB", CancellationToken.None);

        var handler = new ControllerDisconnectedNotificationHandler(
            controllerManager,
            clientManager,
            NullLogger<ControllerDisconnectedNotificationHandler>.Instance);

        var notification = new ControllerDisconnectedNotification(
            Guid.NewGuid(),
            "VATSIM",
            "YBBB",
            "BN-TSN_FSS");

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.False(clientManager.ClientExists("VATSIM", "YBBB"));
    }

    [Fact]
    public async Task Handle_KeepsClientWhenControllersRemain()
    {
        // Arrange
        var controllerManager = new TestControllerManager();
        var clientManager = new TestClientManager();
        await clientManager.EnsureClientExists("VATSIM", "YBBB", CancellationToken.None);

        var remainingController = new ControllerInfo(
            Guid.NewGuid(),
            "ConnectionId",
            "VATSIM",
            "YBBB",
            "BN-ISA_CTR");
        controllerManager.AddController(remainingController);

        var handler = new ControllerDisconnectedNotificationHandler(
            controllerManager,
            clientManager,
            NullLogger<ControllerDisconnectedNotificationHandler>.Instance);

        var notification = new ControllerDisconnectedNotification(
            Guid.NewGuid(),
            "VATSIM",
            "YBBB",
            "BN-TSN_FSS");

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.True(clientManager.ClientExists("VATSIM", "YBBB"));
    }

    [Fact]
    public async Task Handle_OnlyRemovesClientForMatchingNetworkAndStation()
    {
        // Arrange
        var controllerManager = new TestControllerManager();
        var clientManager = new TestClientManager();
        await clientManager.EnsureClientExists("VATSIM", "YBBB", CancellationToken.None);
        await clientManager.EnsureClientExists("IVAO", "YBBB", CancellationToken.None);

        var ivaoController = new ControllerInfo(
            Guid.NewGuid(),
            "ConnectionId",
            "IVAO",
            "YBBB",
            "BN-ISA_CTR");
        controllerManager.AddController(ivaoController);

        var handler = new ControllerDisconnectedNotificationHandler(
            controllerManager,
            clientManager,
            NullLogger<ControllerDisconnectedNotificationHandler>.Instance);

        var notification = new ControllerDisconnectedNotification(
            Guid.NewGuid(),
            "VATSIM",
            "YBBB",
            "BN-ISA_CTR");

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.False(clientManager.ClientExists("VATSIM", "YBBB"));
        Assert.True(clientManager.ClientExists("IVAO", "YBBB"));
    }
}
