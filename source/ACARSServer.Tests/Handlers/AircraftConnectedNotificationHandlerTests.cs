using ACARSServer.Handlers;
using ACARSServer.Hubs;
using ACARSServer.Messages;
using ACARSServer.Model;
using ACARSServer.Tests.Mocks;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Serilog.Core;

namespace ACARSServer.Tests.Handlers;

public class AircraftConnectedNotificationHandlerTests
{
    [Fact]
    public async Task Handle_NotifiesControllersOnSameNetworkAndStation()
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

        var handler = new AircraftConnectedNotificationHandler(
            controllerManager,
            hubContext,
            Logger.None);

        var notification = new AircraftConnected(
            "VATSIM",
            "YBBB",
            "UAL123",
            DataAuthorityState.NextDataAuthority);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        hubContext.Clients.Received(1).Clients(
            Arg.Is<IReadOnlyList<string>>(ids =>
                ids.Count == 2 &&
                ids.Contains("ConnectionId-1") &&
                ids.Contains("ConnectionId-2")));

        await clientProxy.Received(1).SendCoreAsync(
            "AircraftConnected",
            Arg.Is<object[]>(args =>
                args.Length == 2 &&
                args[0].ToString() == "UAL123" &&
                (DataAuthorityState)args[1] == DataAuthorityState.NextDataAuthority),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnlyNotifiesControllersOnMatchingNetwork()
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

        var handler = new AircraftConnectedNotificationHandler(
            controllerManager,
            hubContext,
            Logger.None);

        var notification = new AircraftConnected(
            "VATSIM",
            "YBBB",
            "UAL123",
            DataAuthorityState.NextDataAuthority);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - only VATSIM controller should be notified
        hubContext.Clients.Received(1).Clients(
            Arg.Is<IReadOnlyList<string>>(ids =>
                ids.Count == 1 &&
                ids.Contains("conn-vatsim") &&
                !ids.Contains("conn-ivao")));
    }

    [Fact]
    public async Task Handle_DoesNotNotifyWhenNoControllersConnected()
    {
        // Arrange
        var controllerManager = new TestControllerManager();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var handler = new AircraftConnectedNotificationHandler(
            controllerManager,
            hubContext,
            Logger.None);

        var notification = new AircraftConnected(
            "VATSIM",
            "YBBB",
            "UAL123",
            DataAuthorityState.NextDataAuthority);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - no SignalR notification should be sent
        hubContext.Clients.DidNotReceive().Clients(Arg.Any<IReadOnlyList<string>>());
        await clientProxy.DidNotReceive().SendCoreAsync(
            Arg.Any<string>(),
            Arg.Any<object[]>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IncludesDataAuthorityStateInNotification()
    {
        // Arrange
        var controllerManager = new TestControllerManager();
        var controller = new ControllerInfo(
            Guid.NewGuid(),
            "ConnectionId-1",
            "VATSIM",
            "YBBB",
            "BN-TSN_FSS",
            "1234567");
        controllerManager.AddController(controller);

        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var handler = new AircraftConnectedNotificationHandler(
            controllerManager,
            hubContext,
            Logger.None);

        var notification = new AircraftConnected(
            "VATSIM",
            "YBBB",
            "UAL123",
            DataAuthorityState.CurrentDataAuthority);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - notification includes both callsign and data authority state
        await clientProxy.Received(1).SendCoreAsync(
            "AircraftConnected",
            Arg.Is<object[]>(args =>
                args.Length == 2 &&
                args[0].ToString() == "UAL123" &&
                (DataAuthorityState)args[1] == DataAuthorityState.CurrentDataAuthority),
            Arg.Any<CancellationToken>());
    }
}
