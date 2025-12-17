using System.Net;
using ACARSServer.Clients;
using ACARSServer.Contracts;
using ACARSServer.Tests.Mocks;
using Microsoft.Extensions.Logging.Abstractions;

namespace ACARSServer.Tests.Clients;

public class HoppieAcarsClientTests : IDisposable
{
    private readonly HoppiesConfiguration _configuration = new()
    {
        Url = new Uri("http://test.hoppie.nl/acars"),
        AuthenticationCode = "TEST123",
        FlightSimulationNetwork = "VATSIM",
        StationIdentifier = "YBBB"
    };

    private readonly List<HoppieAcarsClient> _clients = new();

    public void Dispose()
    {
        foreach (var client in _clients)
        {
            try
            {
                client.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(1));
            }
            catch (ObjectDisposedException)
            {
                // Already disposed, ignore
            }
            catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is ObjectDisposedException))
            {
                // Already disposed, ignore
            }
        }
    }

    private HoppieAcarsClient CreateClient(TestHttpMessageHandler httpHandler, TestClock? clock = null)
    {
        var httpClient = new HttpClient(httpHandler) { BaseAddress = _configuration.Url };
        var client = new HoppieAcarsClient(
            _configuration,
            httpClient,
            clock ?? new TestClock(),
            NullLogger<HoppieAcarsClient>.Instance);
        _clients.Add(client);
        return client;
    }

    [Fact]
    public async Task Send_TelexMessage_FormatsRequestCorrectly()
    {
        // Arrange
        var httpHandler = new TestHttpMessageHandler();
        httpHandler.SetResponse(HttpStatusCode.OK, "ok");
        var client = CreateClient(httpHandler);

        await client.Connect(CancellationToken.None);

        var message = new TelexUplinkMessage("UAL123", "THIS IS A TEST MESSAGE");

        // Act
        await client.Send(message, CancellationToken.None);

        // Assert
        var sendRequest = httpHandler.Requests.FirstOrDefault(r =>
        {
            var formDataString = r.Content?.ReadAsStringAsync().Result ?? "";
            return formDataString.Contains("type=telex");
        });

        Assert.NotNull(sendRequest);
        Assert.Equal(HttpMethod.Post, sendRequest.Method);

        var formData = await ParseFormDataFromRequest(sendRequest);
        Assert.Equal("TEST123", formData["logon"]);
        Assert.Equal("VATSIM", formData["network"]);
        Assert.Equal("YBBB", formData["from"]);
        Assert.Equal("UAL123", formData["to"]);
        Assert.Equal("telex", formData["type"]);
        Assert.Equal("THIS%20IS%20A%20TEST%20MESSAGE", formData["packet"]);
    }

    [Fact]
    public async Task Send_CpdlcMessage_FormatsPayloadCorrectly()
    {
        // Arrange
        var httpHandler = new TestHttpMessageHandler();
        httpHandler.SetResponse(HttpStatusCode.OK, "ok");
        var client = CreateClient(httpHandler);

        await client.Connect(CancellationToken.None);

        var message = new CpdlcUplinkMessage(
            1,
            "UAL123",
            new CpdlcMessage("CLIMB TO @FL350@", CpdlcResponseType.WilcoUnable));

        // Act
        await client.Send(message, CancellationToken.None);

        // Assert
        var sendRequest = httpHandler.Requests.FirstOrDefault(r =>
        {
            var formDataString = r.Content?.ReadAsStringAsync().Result ?? "";
            return formDataString.Contains("type=cpdlc");
        });

        Assert.NotNull(sendRequest);
        var formData = await ParseFormDataFromRequest(sendRequest);

        Assert.Equal("cpdlc", formData["type"]);
        Assert.Equal("/data/1//WU/CLIMB%20TO%20@FL350@", formData["packet"]);
    }

    [Fact]
    public async Task Send_CpdlcReply_IncludesReplyToId()
    {
        // Arrange
        var httpHandler = new TestHttpMessageHandler();
        httpHandler.SetResponse(HttpStatusCode.OK, "ok");
        var client = CreateClient(httpHandler);

        await client.Connect(CancellationToken.None);

        var reply = new CpdlcUplinkReply(
            2,
            "UAL123",
            5,
            new CpdlcMessage("ROGER", CpdlcResponseType.NoResponse));

        // Act
        await client.Send(reply, CancellationToken.None);

        // Assert
        var sendRequest = httpHandler.Requests.FirstOrDefault(r =>
        {
            var formDataString = r.Content?.ReadAsStringAsync().Result ?? "";
            return formDataString.Contains("type=cpdlc");
        });

        Assert.NotNull(sendRequest);
        var formData = await ParseFormDataFromRequest(sendRequest);
        Assert.Equal("/data/1/5/NE/ROGER", formData["packet"]);
    }

    [Theory]
    [InlineData(CpdlcResponseType.NoResponse, "NE")]
    [InlineData(CpdlcResponseType.WilcoUnable, "WU")]
    [InlineData(CpdlcResponseType.AffirmativeNegative, "AN")]
    [InlineData(CpdlcResponseType.Roger, "R")]
    public async Task Send_CpdlcMessage_MapsResponseTypesCorrectly(CpdlcResponseType responseType, string expectedCode)
    {
        // Arrange
        var httpHandler = new TestHttpMessageHandler();
        httpHandler.SetResponse(HttpStatusCode.OK, "ok");
        var client = CreateClient(httpHandler);

        await client.Connect(CancellationToken.None);

        var message = new CpdlcUplinkMessage(
            1,
            "UAL123",
            new CpdlcMessage("TEST", responseType));

        // Act
        await client.Send(message, CancellationToken.None);

        // Assert
        var sendRequest = httpHandler.Requests.FirstOrDefault(r =>
        {
            var formDataString = r.Content?.ReadAsStringAsync().Result ?? "";
            return formDataString.Contains("type=cpdlc");
        });

        Assert.NotNull(sendRequest);
        var formData = await ParseFormDataFromRequest(sendRequest);
        Assert.Equal($"/data/1//{expectedCode}/TEST", formData["packet"]);

        await client.DisposeAsync();
        _clients.Remove(client);
    }

    [Fact]
    public async Task Send_CpdlcMessage_UrlEncodesContent()
    {
        // Arrange
        var httpHandler = new TestHttpMessageHandler();
        httpHandler.SetResponse(HttpStatusCode.OK, "ok");
        var client = CreateClient(httpHandler);

        await client.Connect(CancellationToken.None);

        var message = new CpdlcUplinkMessage(
            1,
            "UAL123",
            new CpdlcMessage("CLIMB TO @FL350@", CpdlcResponseType.WilcoUnable));

        // Act
        await client.Send(message, CancellationToken.None);

        // Assert
        var sendRequest = httpHandler.Requests.FirstOrDefault(r =>
        {
            var formDataString = r.Content?.ReadAsStringAsync().Result ?? "";
            return formDataString.Contains("type=cpdlc");
        });

        Assert.NotNull(sendRequest);
        var formData = await ParseFormDataFromRequest(sendRequest);
        Assert.Equal("/data/1//WU/CLIMB%20TO%20@FL350@", formData["packet"]);
    }

    [Fact]
    public async Task Poll_TelexMessage_ParsesCorrectly()
    {
        // Arrange
        var httpHandler = new TestHttpMessageHandler();
        httpHandler.QueueResponse(HttpStatusCode.OK, "UAL123 YBBB telex THIS IS A TEST");
        httpHandler.SetResponse(HttpStatusCode.OK, "ok"); // Subsequent polls return empty
        var client = CreateClient(httpHandler);

        // Act
        await client.Connect(CancellationToken.None);
        await Task.Delay(100); // Give polling task time to start

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var message = await client.MessageReader.ReadAsync(cts.Token);

        // Assert
        var telexMessage = Assert.IsType<TelexDownlinkMessage>(message);
        Assert.Equal("UAL123", telexMessage.Sender);
        Assert.Equal("THIS IS A TEST", telexMessage.Content);
    }

    [Fact]
    public async Task Poll_CpdlcMessage_ParsesCorrectly()
    {
        // Arrange
        var httpHandler = new TestHttpMessageHandler();
        httpHandler.QueueResponse(HttpStatusCode.OK, "UAL123 YBBB cpdlc /data/5//WU/REQUEST DESCENT");
        httpHandler.SetResponse(HttpStatusCode.OK, "ok");
        var client = CreateClient(httpHandler);

        // Act
        await client.Connect(CancellationToken.None);
        await Task.Delay(100); // Give polling task time to start

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var message = await client.MessageReader.ReadAsync(cts.Token);

        // Assert
        var cpdlcMessage = Assert.IsType<CpdlcDownlinkMessage>(message);
        Assert.Equal("UAL123", cpdlcMessage.Sender);
        Assert.Equal(5, cpdlcMessage.MessageId);
        Assert.Equal("REQUEST DESCENT", cpdlcMessage.Message.Content);
        Assert.Equal(CpdlcResponseType.WilcoUnable, cpdlcMessage.Message.ResponseType);
    }

    [Fact]
    public async Task Poll_CpdlcReply_ParsesCorrectly()
    {
        // Arrange
        var httpHandler = new TestHttpMessageHandler();
        httpHandler.QueueResponse(HttpStatusCode.OK, "UAL123 YBBB cpdlc /data/7/3/R/WILCO");
        httpHandler.SetResponse(HttpStatusCode.OK, "ok");
        var client = CreateClient(httpHandler);

        // Act
        await client.Connect(CancellationToken.None);
        await Task.Delay(100); // Give polling task time to start

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var message = await client.MessageReader.ReadAsync(cts.Token);

        // Assert
        var cpdlcReply = Assert.IsType<CpdlcDownlinkReply>(message);
        Assert.Equal("UAL123", cpdlcReply.Sender);
        Assert.Equal(7, cpdlcReply.MessageId);
        Assert.Equal(3, cpdlcReply.ReplyToMessageId);
        Assert.Equal("WILCO", cpdlcReply.Message.Content);
        Assert.Equal(CpdlcResponseType.Roger, cpdlcReply.Message.ResponseType);
    }

    [Fact]
    public async Task Poll_MultipleMessages_ParsesAllCorrectly()
    {
        // Arrange
        var httpHandler = new TestHttpMessageHandler();
        var response = "UAL123 YBBB telex HELLO\nDAL456 YBBB cpdlc /data/1//WU/REQUEST CLIMB";
        httpHandler.QueueResponse(HttpStatusCode.OK, response);
        httpHandler.SetResponse(HttpStatusCode.OK, "ok");
        var client = CreateClient(httpHandler);

        // Act
        await client.Connect(CancellationToken.None);
        await Task.Delay(100); // Give polling task time to start

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var message1 = await client.MessageReader.ReadAsync(cts.Token);
        var message2 = await client.MessageReader.ReadAsync(cts.Token);

        // Assert
        var telexMessage = Assert.IsType<TelexDownlinkMessage>(message1);
        Assert.Equal("UAL123", telexMessage.Sender);
        Assert.Equal("HELLO", telexMessage.Content);

        var cpdlcMessage = Assert.IsType<CpdlcDownlinkMessage>(message2);
        Assert.Equal("DAL456", cpdlcMessage.Sender);
        Assert.Equal("REQUEST CLIMB", cpdlcMessage.Message.Content);
    }

    [Fact]
    public async Task Poll_ResponseTypeCodes_MapCorrectly()
    {
        var testCases = new[]
        {
            ("NE", CpdlcResponseType.NoResponse),
            ("WU", CpdlcResponseType.WilcoUnable),
            ("AN", CpdlcResponseType.AffirmativeNegative),
            ("R", CpdlcResponseType.Roger)
        };

        foreach (var (code, expectedType) in testCases)
        {
            // Arrange
            var httpHandler = new TestHttpMessageHandler();
            httpHandler.QueueResponse(HttpStatusCode.OK, $"UAL123 YBBB cpdlc /data/1//{code}/TEST");
            httpHandler.SetResponse(HttpStatusCode.OK, "ok");
            var client = CreateClient(httpHandler);

            // Act
            await client.Connect(CancellationToken.None);
            await Task.Delay(100); // Give polling task time to start

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var message = await client.MessageReader.ReadAsync(cts.Token);

            // Assert
            var cpdlcMessage = Assert.IsType<CpdlcDownlinkMessage>(message);
            Assert.Equal(expectedType, cpdlcMessage.Message.ResponseType);

            await client.DisposeAsync();
            _clients.Remove(client);
        }
    }

    private async Task<Dictionary<string, string>> ParseFormDataFromRequest(HttpRequestMessage request)
    {
        var formDataString = await request.Content!.ReadAsStringAsync();
        var result = new Dictionary<string, string>();
        var pairs = formDataString.Split('&');

        foreach (var pair in pairs)
        {
            var keyValue = pair.Split('=', 2);
            if (keyValue.Length == 2)
            {
                result[Uri.UnescapeDataString(keyValue[0])] = Uri.UnescapeDataString(keyValue[1]);
            }
        }

        return result;
    }
}
