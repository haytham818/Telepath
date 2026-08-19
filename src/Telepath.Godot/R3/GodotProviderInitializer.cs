// SPDX-License-Identifier: MIT
// Copyright (c) 2024 Cysharp, Inc.
// Vendored from https://github.com/Cysharp/R3 (R3.Godot addon subset)

// SPDX-License-Identifier: MIT
// Copyright (c) 2024 Cysharp, Inc.
// Vendored from https://github.com/Cysharp/R3 (R3.Godot addon subset)

#nullable enable

using System.Runtime.Loader;
using Godot;

namespace R3;

public static class GodotProviderInitializer
{
    private static int _alcHook;

    public static void SetDefaultObservableSystem()
    {
        SetDefaultObservableSystem(ex => GD.PrintErr(ex));
    }

    public static void SetDefaultObservableSystem(Action<Exception> unhandledExceptionHandler)
    {
        EnsureAlcUnloadRegistered();
        ObservableSystem.RegisterUnhandledExceptionHandler(unhandledExceptionHandler);
        ObservableSystem.DefaultTimeProvider = GodotTimeProvider.Process;
        ObservableSystem.DefaultFrameProvider = GodotFrameProvider.Process;
    }

    public static void ResetObservableSystem()
    {
        try
        {
            GodotFrameProvider.Process.Reset();
            GodotFrameProvider.PhysicsProcess.Reset();
            ObservableSystem.DefaultTimeProvider = TimeProvider.System;
            ObservableSystem.DefaultFrameProvider = new DetachedFrameProvider();
            ObservableSystem.RegisterUnhandledExceptionHandler(static exception => Console.WriteLine(exception));
        }
        catch
        {
        }
    }

    private static void EnsureAlcUnloadRegistered()
    {
        if (Interlocked.Exchange(ref _alcHook, 1) != 0)
        {
            return;
        }

        var context = AssemblyLoadContext.GetLoadContext(typeof(GodotProviderInitializer).Assembly);
        if (context is null || ReferenceEquals(context, AssemblyLoadContext.Default))
        {
            return;
        }

        context.Unloading += static _ => ResetObservableSystem();
    }

    private sealed class DetachedFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;

        public override void Register(IFrameRunnerWorkItem callback)
        {
        }
    }
}
