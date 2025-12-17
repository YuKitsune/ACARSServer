using ACARSServer.Contracts;
using ACARSServer.Handlers;
using ACARSServer.Messages;
using ACARSServer.Tests.Mocks;
using Microsoft.Extensions.Logging.Abstractions;

namespace ACARSServer.Tests.Handlers;

public class SendCpdlcUplinkReplyCommandHandlerTests
{
    [Fact]
    public async Task Handle_SendsReplyToAcarsClient()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var handler = new SendCpdlcUplinkReplyCommandHandler(
            clientManager,
            NullLogger<SendCpdlcUplinkReplyCommandHandler>.Instance);

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
            new CpdlcMessage("ROGER", CpdlcResponseType.NoResponse));

        var command = new SendCpdlcUplinkReplyCommand(context, reply);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var client = await clientManager.GetOrCreateAcarsClient("VATSIM", "YBBB", CancellationToken.None);
        var testClient = (TestAcarsClient)client;
        Assert.Single(testClient.SentMessages);
        Assert.Equal(reply, testClient.SentMessages[0]);
    }

    [Fact]
    public async Task Handle_CreatesClientIfNotExists()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var handler = new SendCpdlcUplinkReplyCommandHandler(
            clientManager,
            NullLogger<SendCpdlcUplinkReplyCommandHandler>.Instance);

        var context = new UserContext(
            Guid.NewGuid(),
            "conn-123",
            "VATSIM",
            "YBBB",
            "BN-TSN_FSS");

        var reply = new CpdlcUplinkReply(
            3,
            "DAL456",
            2,
            new CpdlcMessage("WILCO", CpdlcResponseType.NoResponse));

        var command = new SendCpdlcUplinkReplyCommand(context, reply);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(clientManager.ClientExists("VATSIM", "YBBB"));
    }
}
