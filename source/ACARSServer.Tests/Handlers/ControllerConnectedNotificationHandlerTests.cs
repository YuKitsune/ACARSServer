using ACARSServer.Handlers;
using ACARSServer.Messages;
using ACARSServer.Tests.Mocks;
using Microsoft.Extensions.Logging.Abstractions;

namespace ACARSServer.Tests.Handlers;

public class ControllerConnectedNotificationHandlerTests
{
    [Fact]
    public async Task Handle_EnsuresClientExists()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var handler = new ControllerConnectedNotificationHandler(
            clientManager,
            NullLogger<ControllerConnectedNotificationHandler>.Instance);

        var notification = new ControllerConnectedNotification(
            Guid.NewGuid(),
            "VATSIM",
            "BN-TSN_FSS",
            "YBBB");

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.True(clientManager.ClientExists("VATSIM", "YBBB"));
    }

    [Fact]
    public async Task Handle_DoesNotCreateDuplicateClient()
    {
        // Arrange
        var clientManager = new TestClientManager();
        await clientManager.EnsureClientExists("VATSIM", "YBBB", CancellationToken.None);

        var handler = new ControllerConnectedNotificationHandler(
            clientManager,
            NullLogger<ControllerConnectedNotificationHandler>.Instance);

        var notification = new ControllerConnectedNotification(
            Guid.NewGuid(),
            "VATSIM",
            "BN-TSN_FSS",
            "YBBB");

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.True(clientManager.ClientExists("VATSIM", "YBBB"));
    }
}
