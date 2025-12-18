using ACARSServer.Contracts;
using ACARSServer.Handlers;
using ACARSServer.Messages;
using ACARSServer.Tests.Mocks;
using Microsoft.Extensions.Logging.Abstractions;

namespace ACARSServer.Tests.Handlers;

public class SendCpdlcUplinkCommandHandlerTests
{
    [Fact]
    public async Task Handle_SendsMessageToAcarsClient()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var handler = new SendCpdlcUplinkCommandHandler(
            clientManager,
            NullLogger<SendCpdlcUplinkCommandHandler>.Instance);

        var context = new UserContext(
            Guid.NewGuid(),
            "conn-123",
            "VATSIM",
            "YBBB",
            "BN-TSN_FSS");

        var message = new CpdlcUplink(
            1,
            "UAL123",
            CpdlcUplinkResponseType.WilcoUnable,
            "CLIMB TO @FL410@");

        var command = new SendCpdlcUplinkCommand(context, message);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var client = await clientManager.GetAcarsClient("VATSIM", "YBBB", CancellationToken.None);
        var testClient = (TestAcarsClient)client;
        Assert.Single(testClient.SentMessages);
        Assert.Equal(message, testClient.SentMessages[0]);
    }

    [Fact]
    public async Task Handle_SendsReplyToAcarsClient()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var handler = new SendCpdlcUplinkCommandHandler(
            clientManager,
            NullLogger<SendCpdlcUplinkCommandHandler>.Instance);

        var context = new UserContext(
            Guid.NewGuid(),
            "conn-123",
            "VATSIM",
            "YBBB",
            "BN-TSN_FSS");

        var reply = new CpdlcUplinkReply(
            2,
            "UAL123",
            1,
            CpdlcUplinkResponseType.NoResponse,
            "ROGER");

        var command = new SendCpdlcUplinkCommand(context, reply);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var client = await clientManager.GetAcarsClient("VATSIM", "YBBB", CancellationToken.None);
        var testClient = (TestAcarsClient)client;
        Assert.Single(testClient.SentMessages);
        Assert.Equal(reply, testClient.SentMessages[0]);
    }
}
