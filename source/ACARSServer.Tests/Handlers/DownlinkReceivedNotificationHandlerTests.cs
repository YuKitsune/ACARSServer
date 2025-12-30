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

        var dialogueRepository = new TestDialogueRepository();

        var handler = new DownlinkReceivedNotificationHandler(
            aircraftManager,
            mediator,
            clock,
            controllerManager,
            dialogueRepository,
            hubContext,
            Logger.None);

        var downlinkMessage = new DownlinkMessage(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST DESCENT",
            clock.UtcNow());

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

        var dialogueRepository = new TestDialogueRepository();

        var handler = new DownlinkReceivedNotificationHandler(
            aircraftManager,
            mediator,
            clock,
            controllerManager,
            dialogueRepository,
            hubContext,
            Logger.None);

        var downlinkMessage = new DownlinkMessage(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST DESCENT",
            clock.UtcNow());

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

        var dialogueRepository = new TestDialogueRepository();

        var handler = new DownlinkReceivedNotificationHandler(
            aircraftManager,
            mediator,
            clock,
            controllerManager,
            dialogueRepository,
            hubContext,
            Logger.None);

        var downlinkMessage = new DownlinkMessage(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST DESCENT",
            clock.UtcNow());

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

        var dialogueRepository = new TestDialogueRepository();

        var handler = new DownlinkReceivedNotificationHandler(
            aircraftManager,
            mediator,
            clock,
            controllerManager,
            dialogueRepository,
            hubContext,
            Logger.None);

        var downlinkMessage = new DownlinkMessage(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST DESCENT",
            clock.UtcNow());

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

        var dialogueRepository = new TestDialogueRepository();

        var handler = new DownlinkReceivedNotificationHandler(
            aircraftManager,
            mediator,
            clock,
            controllerManager,
            dialogueRepository,
            hubContext,
            Logger.None);

        var downlinkMessage = new DownlinkMessage(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST DESCENT",
            clock.UtcNow());

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

    [Fact]
    public async Task Handle_CreatesNewDialogue_ForDownlinkWithNoReference()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();
        var aircraft = new AircraftConnection("UAL123", "YBBB", "VATSIM", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(aircraft, CancellationToken.None);

        var controllerRepository = new TestControllerRepository();
        var dialogueRepository = new TestDialogueRepository();
        var mediator = Substitute.For<IMediator>();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();

        var handler = new DownlinkReceivedNotificationHandler(
            aircraftRepository,
            mediator,
            clock,
            controllerRepository,
            dialogueRepository,
            hubContext,
            Logger.None);

        var downlink = new DownlinkMessage(
            1,
            null,
            "UAL123",
            CpdlcDownlinkResponseType.ResponseRequired,
            AlertType.None,
            "REQUEST CLIMB FL410",
            clock.UtcNow());

        var notification = new DownlinkReceivedNotification("VATSIM", "YBBB", downlink);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        var dialogue = await dialogueRepository.FindDialogueForMessage(
            "VATSIM",
            "YBBB",
            "UAL123",
            1,
            CancellationToken.None);

        Assert.NotNull(dialogue);
        Assert.Single(dialogue.Messages);
        Assert.Equal(downlink, dialogue.Messages[0]);
    }

    [Fact]
    public async Task Handle_AppendsToExistingDialogue_ForDownlinkWithReference()
    {
        // Arrange
        var clock = new TestClock();
        var aircraftRepository = new TestAircraftRepository();
        var aircraft = new AircraftConnection("UAL123", "YBBB", "VATSIM", DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(clock.UtcNow());
        aircraft.AcceptLogon(clock.UtcNow());
        await aircraftRepository.Add(aircraft, CancellationToken.None);

        var controllerRepository = new TestControllerRepository();
        var dialogueRepository = new TestDialogueRepository();
        var mediator = Substitute.For<IMediator>();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();

        // Create existing dialogue with an uplink
        var uplink = new UplinkMessage(
            5,
            null,
            "UAL123",
            CpdlcUplinkResponseType.WilcoUnable,
            AlertType.None,
            "CLIMB TO FL410",
            clock.UtcNow());

        var existingDialogue = new Dialogue("VATSIM", "YBBB", "UAL123", uplink);
        await dialogueRepository.Add(existingDialogue, CancellationToken.None);

        var handler = new DownlinkReceivedNotificationHandler(
            aircraftRepository,
            mediator,
            clock,
            controllerRepository,
            dialogueRepository,
            hubContext,
            Logger.None);

        var downlink = new DownlinkMessage(
            10,
            5,
            "UAL123",
            CpdlcDownlinkResponseType.NoResponse,
            AlertType.None,
            "WILCO",
            clock.UtcNow().AddSeconds(10));

        var notification = new DownlinkReceivedNotification("VATSIM", "YBBB", downlink);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        var dialogue = await dialogueRepository.FindDialogueForMessage(
            "VATSIM",
            "YBBB",
            "UAL123",
            5,
            CancellationToken.None);

        Assert.NotNull(dialogue);
        Assert.Equal(2, dialogue.Messages.Count);
        Assert.Contains(downlink, dialogue.Messages);
    }
}
