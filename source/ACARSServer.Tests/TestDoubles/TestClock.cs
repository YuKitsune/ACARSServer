using ACARSServer.Infrastructure;

namespace ACARSServer.Tests.TestDoubles;

public class TestClock : IClock
{
    private DateTimeOffset _utcNow = DateTimeOffset.UtcNow;

    public DateTimeOffset UtcNow() => _utcNow;

    public void SetUtcNow(DateTimeOffset dateTime)
    {
        _utcNow = dateTime;
    }
}
