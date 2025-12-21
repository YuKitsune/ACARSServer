using ACARSServer.Contracts;
using ACARSServer.Handlers;
using ACARSServer.Messages;
using ACARSServer.Tests.Mocks;
using Serilog.Core;

namespace ACARSServer.Tests.Handlers;

public class SendUplinkCommandHandlerTests
{
    [Fact]
    public async Task Handle_SendsMessageToAcarsClient()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var messageIdProvider = new TestMessageIdProvider();
        var handler = new SendUplinkCommandHandler(
            clientManager,
            messageIdProvider,
            Logger.None);

        var context = new UserContext(
            Guid.NewGuid(),
            "conn-123",
            "VATSIM",
            "YBBB",
            "BN-TSN_FSS");

        var command = new SendUplinkCommand(
            context,
            "UAL123",
            null,
            CpdlcUplinkResponseType.WilcoUnable,
            "CLIMB TO @FL410@");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.UplinkMessage.Id);

        var client = await clientManager.GetAcarsClient("VATSIM", "YBBB", CancellationToken.None);
        var testClient = (TestAcarsClient)client;
        Assert.Single(testClient.SentMessages);

        var sentMessage = Assert.IsType<CpdlcUplink>(testClient.SentMessages[0]);
        Assert.Equal(1, sentMessage.Id);
        Assert.Equal("UAL123", sentMessage.Recipient);
        Assert.Null(sentMessage.ReplyToDownlinkId);
        Assert.Equal(CpdlcUplinkResponseType.WilcoUnable, sentMessage.ResponseType);
        Assert.Equal("CLIMB TO @FL410@", sentMessage.Content);
    }

    [Fact]
    public async Task Handle_SendsReplyToAcarsClient()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var messageIdProvider = new TestMessageIdProvider();
        var handler = new SendUplinkCommandHandler(
            clientManager,
            messageIdProvider,
            Logger.None);

        var context = new UserContext(
            Guid.NewGuid(),
            "conn-123",
            "VATSIM",
            "YBBB",
            "BN-TSN_FSS");

        var command = new SendUplinkCommand(
            context,
            "UAL123",
            5,
            CpdlcUplinkResponseType.NoResponse,
            "ROGER");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.UplinkMessage.Id);

        var client = await clientManager.GetAcarsClient("VATSIM", "YBBB", CancellationToken.None);
        var testClient = (TestAcarsClient)client;
        Assert.Single(testClient.SentMessages);

        var sentMessage = Assert.IsType<CpdlcUplink>(testClient.SentMessages[0]);
        Assert.Equal(1, sentMessage.Id);
        Assert.Equal("UAL123", sentMessage.Recipient);
        Assert.Equal(5, sentMessage.ReplyToDownlinkId);
        Assert.Equal(CpdlcUplinkResponseType.NoResponse, sentMessage.ResponseType);
        Assert.Equal("ROGER", sentMessage.Content);
    }
}
