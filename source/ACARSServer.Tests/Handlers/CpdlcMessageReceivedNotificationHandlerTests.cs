using ACARSServer.Contracts;
using ACARSServer.Handlers;
using ACARSServer.Hubs;
using ACARSServer.Messages;
using ACARSServer.Model;
using ACARSServer.Tests.Mocks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ACARSServer.Tests.Handlers;

public class CpdlcMessageReceivedNotificationHandlerTests
{
    [Fact]
    public async Task Handle_SendsMessageToMatchingControllers()
    {
        // Arrange
        var controllerManager = new TestControllerManager();
        var controller1 = new ControllerInfo(
            Guid.NewGuid(),
            "ConnectionId-1",
            "VATSIM",
            "YBBB",
            "BN-TSN_FSS",
            "1234567");
        var controller2 = new ControllerInfo(
            Guid.NewGuid(),
            "ConnectionId-2",
            "VATSIM",
            "YBBB",
            "BN-OCN_CTR",
            "7654321");
        controllerManager.AddController(controller1);
        controllerManager.AddController(controller2);

        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var handler = new CpdlcMessageReceivedNotificationHandler(
            controllerManager,
            hubContext,
            NullLogger<CpdlcMessageReceivedNotificationHandler>.Instance);

        var downlinkMessage = new CpdlcDownlink(
            1,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            "REQUEST DESCENT");

        var notification = new CpdlcDownlinkMessageReceivedNotification(
            "VATSIM",
            "YBBB",
            downlinkMessage);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        hubContext.Clients.Received(1).Clients(
            Arg.Is<IReadOnlyList<string>>(ids =>
                ids.Count == 2 &&
                ids.Contains("ConnectionId-1") &&
                ids.Contains("ConnectionId-1")));

        await clientProxy.Received(1).SendCoreAsync(
            "ReceiveCpdlcMessage",
            Arg.Is<object[]>(args => args.Length == 1 && args[0] == downlinkMessage),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotSendWhenNoControllersMatch()
    {
        // Arrange
        var controllerManager = new TestControllerManager();
        var controller = new ControllerInfo(
            Guid.NewGuid(),
            "ConnectionId-1",
            "VATSIM",
            "YMMM",
            "ML-IND_FSS",
            "1234567");
        controllerManager.AddController(controller);

        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var handler = new CpdlcMessageReceivedNotificationHandler(
            controllerManager,
            hubContext,
            NullLogger<CpdlcMessageReceivedNotificationHandler>.Instance);

        var downlinkMessage = new CpdlcDownlink(
            1,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            "REQUEST DESCENT");

        var notification = new CpdlcDownlinkMessageReceivedNotification(
            "VATSIM",
            "YBBB",
            downlinkMessage);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        hubContext.Clients.DidNotReceive().Clients(Arg.Any<IReadOnlyList<string>>());
        await clientProxy.DidNotReceive().SendCoreAsync(
            Arg.Any<string>(),
            Arg.Any<object[]>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnlySendsToControllersOnMatchingNetwork()
    {
        // Arrange
        var controllerManager = new TestControllerManager();
        var vatsimController = new ControllerInfo(
            Guid.NewGuid(),
            "conn-vatsim",
            "VATSIM",
            "YBBB",
            "BN-TSN_FSS",
            "1234567");
        var ivaoController = new ControllerInfo(
            Guid.NewGuid(),
            "conn-ivao",
            "IVAO",
            "YBBB",
            "BN-TSN_FSS",
            "7654321");
        controllerManager.AddController(vatsimController);
        controllerManager.AddController(ivaoController);

        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var handler = new CpdlcMessageReceivedNotificationHandler(
            controllerManager,
            hubContext,
            NullLogger<CpdlcMessageReceivedNotificationHandler>.Instance);

        var downlinkMessage = new CpdlcDownlink(
            1,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            "REQUEST DESCENT");

        var notification = new CpdlcDownlinkMessageReceivedNotification(
            "VATSIM",
            "YBBB",
            downlinkMessage);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        hubContext.Clients.Received(1).Clients(
            Arg.Is<IReadOnlyList<string>>(ids =>
                ids.Count == 1 &&
                ids.Contains("conn-vatsim") &&
                !ids.Contains("conn-ivao")));
    }
}
