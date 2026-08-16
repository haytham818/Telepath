// SPDX-License-Identifier: MIT
// Copyright (c) 2024 Cysharp, Inc.
// Vendored from https://github.com/Cysharp/R3 (R3.Godot addon subset)

#nullable enable

namespace R3;

/// <summary>
/// Runtime frame pump. The Autoload script is the GDScript shim
/// <c>FrameProviderDispatcher.gd</c>, which instantiates this node only outside
/// the editor so C# rebuilds are not pinned by a live Autoload script instance.
/// </summary>
public partial class FrameProviderDispatcher : global::Godot.Node
{
    private readonly GodotFramePump _pump = new();

    public override void _Ready()
    {
        _pump.Start();
    }

    public override void _Process(double delta)
    {
        _pump.Process(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        _pump.PhysicsProcess(delta);
    }

    public override void _ExitTree()
    {
        _pump.Stop();
    }
}
