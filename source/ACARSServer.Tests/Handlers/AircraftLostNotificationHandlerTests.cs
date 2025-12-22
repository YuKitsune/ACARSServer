using ACARSServer.Contracts;
using ACARSServer.Handlers;
using ACARSServer.Hubs;
using ACARSServer.Messages;
using ACARSServer.Model;
using ACARSServer.Tests.Mocks;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Serilog.Core;

namespace ACARSServer.Tests.Handlers;

public class AircraftLostNotificationHandlerTests
{
    [Fact]
    public async Task Handle_RemovesAircraftFromTracking()
    {
        // Arrange
        var aircraftManager = new TestAircraftManager();
        var aircraft = new AircraftConnection("UAL123", "YBBB", "VATSIM", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(DateTimeOffset.UtcNow);
        aircraft.AcceptLogon(DateTimeOffset.UtcNow);
        aircraftManager.Add(aircraft);

        var controllerManager = new TestControllerManager();
        var messageIdProvider = new TestMessageIdProvider();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var handler = new AircraftLostNotificationHandler(
            aircraftManager,
            controllerManager,
            hubContext,
            messageIdProvider,
            Logger.None);

        var notification = new AircraftLost("VATSIM", "YBBB", "UAL123");

        // Assert - aircraft exists before handling
        Assert.NotNull(aircraftManager.Get("VATSIM", "YBBB", "UAL123"));

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - aircraft is removed after handling
        Assert.Null(aircraftManager.Get("VATSIM", "YBBB", "UAL123"));
    }

    [Fact]
    public async Task Handle_SendsErrorDownlinkAndDisconnectionNotification()
    {
        // Arrange
        var aircraftManager = new TestAircraftManager();
        var aircraft = new AircraftConnection("UAL123", "YBBB", "VATSIM", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(DateTimeOffset.UtcNow);
        aircraft.AcceptLogon(DateTimeOffset.UtcNow);
        aircraftManager.Add(aircraft);

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

        var messageIdProvider = new TestMessageIdProvider();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var handler = new AircraftLostNotificationHandler(
            aircraftManager,
            controllerManager,
            hubContext,
            messageIdProvider,
            Logger.None);

        var notification = new AircraftLost("VATSIM", "YBBB", "UAL123");

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - controllers are notified twice (for both events)
        hubContext.Clients.Received(2).Clients(
            Arg.Is<IReadOnlyList<string>>(ids =>
                ids.Count == 2 &&
                ids.Contains("ConnectionId-1") &&
                ids.Contains("ConnectionId-2")));

        // Assert - error downlink is sent
        await clientProxy.Received(1).SendCoreAsync(
            "DownlinkReceived",
            Arg.Is<object[]>(args =>
                args.Length == 1 &&
                args[0] is CpdlcDownlink &&
                ((CpdlcDownlink)args[0]).Sender == "UAL123" &&
                ((CpdlcDownlink)args[0]).Content == "ERROR CONNECTION TIMED OUT" &&
                ((CpdlcDownlink)args[0]).ResponseType == CpdlcDownlinkResponseType.NoResponse),
            Arg.Any<CancellationToken>());

        // Assert - disconnection notification is sent
        await clientProxy.Received(1).SendCoreAsync(
            "AircraftDisconnected",
            Arg.Is<object[]>(args => args.Length == 1 && args[0].ToString() == "UAL123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotNotifyControllersOnDifferentNetwork()
    {
        // Arrange
        var aircraftManager = new TestAircraftManager();
        var aircraft = new AircraftConnection("UAL123", "YBBB", "VATSIM", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(DateTimeOffset.UtcNow);
        aircraft.AcceptLogon(DateTimeOffset.UtcNow);
        aircraftManager.Add(aircraft);

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

        var messageIdProvider = new TestMessageIdProvider();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var handler = new AircraftLostNotificationHandler(
            aircraftManager,
            controllerManager,
            hubContext,
            messageIdProvider,
            Logger.None);

        var notification = new AircraftLost("VATSIM", "YBBB", "UAL123");

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - only VATSIM controller should be notified (twice, for both events)
        hubContext.Clients.Received(2).Clients(
            Arg.Is<IReadOnlyList<string>>(ids =>
                ids.Count == 1 &&
                ids.Contains("conn-vatsim") &&
                !ids.Contains("conn-ivao")));
    }

    [Fact]
    public async Task Handle_DoesNotThrowWhenAircraftNotFound()
    {
        // Arrange
        var aircraftManager = new TestAircraftManager();
        var controllerManager = new TestControllerManager();
        var messageIdProvider = new TestMessageIdProvider();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();

        var handler = new AircraftLostNotificationHandler(
            aircraftManager,
            controllerManager,
            hubContext,
            messageIdProvider,
            Logger.None);

        var notification = new AircraftLost("VATSIM", "YBBB", "UAL123");

        // Act & Assert - should not throw
        await handler.Handle(notification, CancellationToken.None);

        // Assert - no SignalR calls should be made
        hubContext.Clients.DidNotReceive().Clients(Arg.Any<IReadOnlyList<string>>());
    }

    [Fact]
    public async Task Handle_DoesNotNotifyWhenNoControllersConnected()
    {
        // Arrange
        var aircraftManager = new TestAircraftManager();
        var aircraft = new AircraftConnection("UAL123", "YBBB", "VATSIM", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(DateTimeOffset.UtcNow);
        aircraft.AcceptLogon(DateTimeOffset.UtcNow);
        aircraftManager.Add(aircraft);

        var controllerManager = new TestControllerManager();
        var messageIdProvider = new TestMessageIdProvider();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var handler = new AircraftLostNotificationHandler(
            aircraftManager,
            controllerManager,
            hubContext,
            messageIdProvider,
            Logger.None);

        var notification = new AircraftLost("VATSIM", "YBBB", "UAL123");

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - aircraft should still be removed
        Assert.Null(aircraftManager.Get("VATSIM", "YBBB", "UAL123"));

        // Assert - no SignalR notification should be sent
        hubContext.Clients.DidNotReceive().Clients(Arg.Any<IReadOnlyList<string>>());
        await clientProxy.DidNotReceive().SendCoreAsync(
            Arg.Any<string>(),
            Arg.Any<object[]>(),
            Arg.Any<CancellationToken>());
    }
}
