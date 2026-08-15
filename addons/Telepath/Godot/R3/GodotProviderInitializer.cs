// SPDX-License-Identifier: MIT
// Copyright (c) 2024 Cysharp, Inc.
// Vendored from https://github.com/Cysharp/R3 (R3.Godot addon subset)

#nullable enable

using Godot;
using System;

namespace R3;

public static class GodotProviderInitializer
{
    public static void SetDefaultObservableSystem()
    {
        SetDefaultObservableSystem(ex => GD.PrintErr(ex));
    }

    public static void SetDefaultObservableSystem(Action<Exception> unhandledExceptionHandler)
    {
        ObservableSystem.RegisterUnhandledExceptionHandler(unhandledExceptionHandler);
        ObservableSystem.DefaultTimeProvider = GodotTimeProvider.Process;
        ObservableSystem.DefaultFrameProvider = GodotFrameProvider.Process;
    }
}
