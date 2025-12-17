using System.Collections.Concurrent;

namespace ACARSServer.Model;

public interface IControllerManager
{
    List<ControllerInfo> Controllers { get; }
    void AddController(ControllerInfo controller);
    void RemoveController(string connectionId);
    ControllerInfo? GetController(string connectionId);
}

public class ControllerManager : IControllerManager
{
    private readonly ConcurrentDictionary<string, ControllerInfo> _controllers = new();

    public List<ControllerInfo> Controllers => _controllers.Values.ToList();

    public void AddController(ControllerInfo controller)
    {
        _controllers.TryAdd(controller.ConnectionId, controller);
    }

    public void RemoveController(string connectionId)
    {
        _controllers.TryRemove(connectionId, out _);
    }

    public ControllerInfo? GetController(string connectionId)
    {
        _controllers.TryGetValue(connectionId, out var controller);
        return controller;
    }
}
