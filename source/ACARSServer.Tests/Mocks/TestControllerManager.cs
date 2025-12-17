using ACARSServer.Model;

namespace ACARSServer.Tests.Mocks;

public class TestControllerManager : IControllerManager
{
    private readonly Dictionary<string, ControllerInfo> _controllers = new();

    public List<ControllerInfo> Controllers => _controllers.Values.ToList();

    public void AddController(ControllerInfo controller)
    {
        _controllers[controller.ConnectionId] = controller;
    }

    public void RemoveController(string connectionId)
    {
        _controllers.Remove(connectionId);
    }

    public ControllerInfo? GetController(string connectionId)
    {
        _controllers.TryGetValue(connectionId, out var controller);
        return controller;
    }
}
