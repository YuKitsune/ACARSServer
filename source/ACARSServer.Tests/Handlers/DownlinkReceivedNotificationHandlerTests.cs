using ACARSServer.Contracts;
using ACARSServer.Handlers;
using ACARSServer.Hubs;
using ACARSServer.Messages;
using ACARSServer.Model;
using ACARSServer.Tests.Mocks;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Serilog.Core;

namespace ACARSServer.Tests.Handlers;

public class DownlinkReceivedNotificationHandlerTests
{
    [Fact]
    public async Task Handle_SendsMessageToMatchingControllers()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftManager = new TestAircraftRepository();
        var aircraft = new AircraftConnection("UAL123", "YBBB", "VATSIM", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftManager.Add(aircraft, CancellationToken.None);

        var controllerManager = new TestControllerRepository();
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
        await controllerManager.Add(controller1, CancellationToken.None);
        await controllerManager.Add(controller2, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var handler = new DownlinkReceivedNotificationHandler(
            aircraftManager,
            mediator,
            clock,
            controllerManager,
            hubContext,
            Logger.None);

        var downlinkMessage = new CpdlcDownlink(
            1,
            "UAL123",
            null,
            CpdlcDownlinkResponseType.ResponseRequired,
            "REQUEST DESCENT");

        var notification = new DownlinkReceivedNotification(
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
            "DownlinkReceived",
            Arg.Is<object[]>(args => args.Length == 1 && args[0] == downlinkMessage),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotSendWhenNoControllersMatch()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftManager = new TestAircraftRepository();
        var aircraft = new AircraftConnection("UAL123", "YBBB", "VATSIM", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftManager.Add(aircraft, CancellationToken.None);

        var controllerManager = new TestControllerRepository();
        var controller = new ControllerInfo(
            Guid.NewGuid(),
            "ConnectionId-1",
            "VATSIM",
            "YMMM",
            "ML-IND_FSS",
            "1234567");
        await controllerManager.Add(controller, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var handler = new DownlinkReceivedNotificationHandler(
            aircraftManager,
            mediator,
            clock,
            controllerManager,
            hubContext,
            Logger.None);

        var downlinkMessage = new CpdlcDownlink(
            1,
            "UAL123",
            null,
            CpdlcDownlinkResponseType.ResponseRequired,
            "REQUEST DESCENT");

        var notification = new DownlinkReceivedNotification(
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
        var clock = new TestClock();
        var aircraftManager = new TestAircraftRepository();
        var aircraft = new AircraftConnection("UAL123", "YBBB", "VATSIM", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftManager.Add(aircraft, CancellationToken.None);

        var controllerManager = new TestControllerRepository();
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
        await controllerManager.Add(vatsimController, CancellationToken.None);
        await controllerManager.Add(ivaoController, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var handler = new DownlinkReceivedNotificationHandler(
            aircraftManager,
            mediator,
            clock,
            controllerManager,
            hubContext,
            Logger.None);

        var downlinkMessage = new CpdlcDownlink(
            1,
            "UAL123",
            null,
            CpdlcDownlinkResponseType.ResponseRequired,
            "REQUEST DESCENT");

        var notification = new DownlinkReceivedNotification(
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

    [Fact]
    public async Task Handle_PromotesAircraftToCurrentDataAuthorityOnFirstDownlink()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftManager = new TestAircraftRepository();
        var aircraft = new AircraftConnection("UAL123", "YBBB", "VATSIM", DataAuthorityState.NextDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftManager.Add(aircraft, CancellationToken.None);

        var controllerManager = new TestControllerRepository();
        var controller = new ControllerInfo(
            Guid.NewGuid(),
            "ConnectionId-1",
            "VATSIM",
            "YBBB",
            "BN-TSN_FSS",
            "1234567");
        await controllerManager.Add(controller, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var handler = new DownlinkReceivedNotificationHandler(
            aircraftManager,
            mediator,
            clock,
            controllerManager,
            hubContext,
            Logger.None);

        var downlinkMessage = new CpdlcDownlink(
            1,
            "UAL123",
            null,
            CpdlcDownlinkResponseType.ResponseRequired,
            "REQUEST DESCENT");

        var notification = new DownlinkReceivedNotification(
            "VATSIM",
            "YBBB",
            downlinkMessage);

        // Assert - aircraft starts as NextDataAuthority
        Assert.Equal(DataAuthorityState.NextDataAuthority, aircraft.DataAuthorityState);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - aircraft is promoted to CurrentDataAuthority
        Assert.Equal(DataAuthorityState.CurrentDataAuthority, aircraft.DataAuthorityState);
    }

    [Fact]
    public async Task Handle_UpdatesLastSeen()
    {
        // Arrange
        var logonTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var clock = new TestClock();
        clock.SetUtcNow(logonTime);
        
        var aircraftManager = new TestAircraftRepository();
        var aircraft = new AircraftConnection("UAL123", "YBBB", "VATSIM", DataAuthorityState.NextDataAuthority);
        aircraft.RequestLogon(logonTime);
        aircraft.AcceptLogon(logonTime);
        await aircraftManager.Add(aircraft, CancellationToken.None);

        var controllerManager = new TestControllerRepository();
        var controller = new ControllerInfo(
            Guid.NewGuid(),
            "ConnectionId-1",
            "VATSIM",
            "YBBB",
            "BN-TSN_FSS",
            "1234567");
        await controllerManager.Add(controller, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var expectedLastSeen = new DateTimeOffset(2026, 1, 1, 1, 0, 0, TimeSpan.Zero);
        clock.SetUtcNow(expectedLastSeen);
        
        var handler = new DownlinkReceivedNotificationHandler(
            aircraftManager,
            mediator,
            clock,
            controllerManager,
            hubContext,
            Logger.None);

        var downlinkMessage = new CpdlcDownlink(
            1,
            "UAL123",
            null,
            CpdlcDownlinkResponseType.ResponseRequired,
            "REQUEST DESCENT");

        var notification = new DownlinkReceivedNotification(
            "VATSIM",
            "YBBB",
            downlinkMessage);

        // Assert
        Assert.Equal(logonTime, aircraft.LastSeen);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.Equal(expectedLastSeen, aircraft.LastSeen);
    }
}
