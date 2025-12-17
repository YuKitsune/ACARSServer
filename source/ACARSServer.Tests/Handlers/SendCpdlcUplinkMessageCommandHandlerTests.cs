using ACARSServer.Contracts;
using ACARSServer.Handlers;
using ACARSServer.Messages;
using ACARSServer.Tests.Mocks;
using Microsoft.Extensions.Logging.Abstractions;

namespace ACARSServer.Tests.Handlers;

public class SendCpdlcUplinkMessageCommandHandlerTests
{
    [Fact]
    public async Task Handle_SendsMessageToAcarsClient()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var handler = new SendCpdlcUplinkMessageCommandHandler(
            clientManager,
            NullLogger<SendCpdlcUplinkMessageCommandHandler>.Instance);

        var context = new UserContext(
            Guid.NewGuid(),
            "conn-123",
            "VATSIM",
            "YBBB",
            "BN-TSN_FSS");

        var message = new CpdlcUplinkMessage(
            1,
            "UAL123",
            new CpdlcMessage("CLIMB TO @FL410@", CpdlcResponseType.WilcoUnable));

        var command = new SendCpdlcUplinkMessageCommand(context, message);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var client = await clientManager.GetOrCreateAcarsClient("VATSIM", "YBBB", CancellationToken.None);
        var testClient = (TestAcarsClient)client;
        Assert.Single(testClient.SentMessages);
        Assert.Equal(message, testClient.SentMessages[0]);
    }

    [Fact]
    public async Task Handle_CreatesClientIfNotExists()
    {
        // Arrange
        var clientManager = new TestClientManager();
        var handler = new SendCpdlcUplinkMessageCommandHandler(
            clientManager,
            NullLogger<SendCpdlcUplinkMessageCommandHandler>.Instance);

        var context = new UserContext(
            Guid.NewGuid(),
            "conn-123",
            "VATSIM",
            "YBBB",
            "BN-TSN_FSS");

        var message = new CpdlcUplinkMessage(
            1,
            "UAL123",
            new CpdlcMessage("CONTACT BRISBANE CENTRE ON 128.6", CpdlcResponseType.Roger));

        var command = new SendCpdlcUplinkMessageCommand(context, message);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(clientManager.ClientExists("VATSIM", "YBBB"));
    }
}
