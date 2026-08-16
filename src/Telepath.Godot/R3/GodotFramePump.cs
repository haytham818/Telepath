#nullable enable

using System.Runtime.CompilerServices;

namespace R3;

/// <summary>
/// Shared process/physics pump for the runtime Autoload child.
/// Keeping this off <see cref="Godot.GodotObject"/> avoids an extra C# node.
/// </summary>
internal sealed class GodotFramePump
{
    private readonly StrongBox<double> _processDelta = new();
    private readonly StrongBox<double> _physicsProcessDelta = new();
    private bool _running;

    public void Start()
    {
        GodotProviderInitializer.SetDefaultObservableSystem();
        GodotFrameProvider.Process.Delta = _processDelta;
        GodotFrameProvider.PhysicsProcess.Delta = _physicsProcessDelta;
        _running = true;
    }

    public void Process(double delta)
    {
        if (!_running)
        {
            return;
        }

        _processDelta.Value = delta;
        GodotTimeProvider.Process.time += delta;
        GodotFrameProvider.Process.Run(delta);
    }

    public void PhysicsProcess(double delta)
    {
        if (!_running)
        {
            return;
        }

        _physicsProcessDelta.Value = delta;
        GodotTimeProvider.PhysicsProcess.time += delta;
        GodotFrameProvider.PhysicsProcess.Run(delta);
    }

    public void Stop()
    {
        _running = false;
        GodotProviderInitializer.ResetObservableSystem();
    }
}
