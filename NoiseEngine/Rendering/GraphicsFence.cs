﻿using NoiseEngine.Interop;
using NoiseEngine.Interop.InteropMarshalling;
using NoiseEngine.Interop.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NoiseEngine.Rendering;

public class GraphicsFence {

    private readonly InteropHandle<GraphicsFence> handle;

    public GraphicsDevice Device { get; }

    public bool IsSignaled {
        get {
            if (!GraphicsFenceInterop.IsSignaled(handle).TryGetValue(out InteropBool value, out ResultError error))
                error.ThrowAndDispose();
            return value;
        }
    }

    internal GraphicsFence(GraphicsDevice device, InteropHandle<GraphicsFence> handle) {
        Device = device;
        this.handle = handle;
    }

    ~GraphicsFence() {
        GraphicsFenceInterop.Destroy(handle);
    }

    /// <summary>
    /// Waits until everyone <paramref name="fences"/> to become signaled.
    /// </summary>
    /// <param name="fences">
    /// <see cref="GraphicsFence"/> to wait. They must belong to the same <see cref="GraphicsDevice"/>.
    /// </param>
    /// <param name="timeout">
    /// The timeout period in units of nanoseconds. timeout is adjusted to the closest value allowed by the
    /// implementation-dependent timeout accuracy, which may be substantially longer than one nanosecond,
    /// and may be longer than the requested period.
    /// </param>
    public static void WaitAll(IEnumerable<GraphicsFence> fences, ulong timeout) {
        WaitMultiple(fences, true, timeout);
    }

    /// <summary>
    /// Waits until anyone <paramref name="fences"/> to become signaled.
    /// </summary>
    /// <param name="fences">
    /// <see cref="GraphicsFence"/> to wait. They must belong to the same <see cref="GraphicsDevice"/>.
    /// </param>
    /// <param name="timeout">
    /// The timeout period in units of nanoseconds. timeout is adjusted to the closest value allowed by the
    /// implementation-dependent timeout accuracy, which may be substantially longer than one nanosecond,
    /// and may be longer than the requested period.
    /// </param>
    public static void WaitAny(IEnumerable<GraphicsFence> fences, ulong timeout) {
        WaitMultiple(fences, true, timeout);
    }

    private static void WaitMultiple(IEnumerable<GraphicsFence> fences, bool waitAll, ulong timeout) {
        int count = fences.Count();
        if (count == 0)
            return;

        Span<InteropHandle<GraphicsFence>> handles = count <= 1024 ?
            stackalloc InteropHandle<GraphicsFence>[count] : new InteropHandle<GraphicsFence>[count];

        GraphicsDevice device = fences.First().Device;

        int i = 0;
        foreach (GraphicsFence fence in fences) {
            if (device != fence.Device)
                throw new ArgumentException($"Fences are not from the same {nameof(GraphicsDevice)}.", nameof(fences));

            handles[i++] = fence.handle;
        }

        if (!GraphicsFenceInterop.WaitMultiple(handles, waitAll, timeout).TryGetValue(out _, out ResultError error))
            error.ThrowAndDispose();
    }

    /// <summary>
    /// Waits for this <see cref="GraphicsFence"/> to become signaled.
    /// </summary>
    /// <param name="timeout">
    /// The timeout period in units of nanoseconds. timeout is adjusted to the closest value allowed by the
    /// implementation-dependent timeout accuracy, which may be substantially longer than one nanosecond,
    /// and may be longer than the requested period.
    /// </param>
    public void Wait(ulong timeout = ulong.MaxValue) {
        if (!GraphicsFenceInterop.Wait(handle, timeout).TryGetValue(out _, out ResultError error))
            error.ThrowAndDispose();
    }

}
