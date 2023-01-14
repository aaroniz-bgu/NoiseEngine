﻿using NoiseEngine.Rendering;
using NoiseEngine.Rendering.Exceptions;
using NoiseEngine.Rendering.Vulkan;
using System;

namespace NoiseEngine;

public class Camera {

    private CameraClearFlags clearFlags = CameraClearFlags.SolidColor;
    private Color clearColor = new Color(0.26666f, 0.45882f, 0.87058f);
    private ICameraRenderTarget? renderTarget;

    public ApplicationScene Scene { get; }
    public GraphicsDevice GraphicsDevice => Scene.GraphicsDevice;

    public CameraClearFlags ClearFlags {
        get => clearFlags;
        set {
            clearFlags = value;
            IsDirty = true;
        }
    }

    public Color ClearColor {
        get => clearColor;
        set {
            clearColor = value;
            Delegation.UpdateClearColor();
        }
    }

    /// <summary>
    /// Sets camera render target. If setted render target is not null, texture usage of this render target must have
    /// TextureUsage.ColorAttachment flag.
    /// </summary>
    public ICameraRenderTarget? RenderTarget {
        get => renderTarget;
        set {
            AssertRenderTarget(value);

            renderTarget = value;
            IsDirty = true;

            if (value is null)
                Delegation.ClearRenderTarget();
        }
    }

    internal CameraDelegation Delegation { get; }
    internal bool IsDirty { get; set; } = true;

    public Camera(ApplicationScene scene) {
        Scene = scene;

        Delegation = GraphicsDevice.Instance.Api switch {
            GraphicsApi.Vulkan => new VulkanCameraDelegation(this),
            _ => throw new GraphicsApiNotSupportedException(GraphicsDevice.Instance.Api),
        };

        Delegation.UpdateClearColor();
    }

    private void AssertRenderTarget(ICameraRenderTarget? renderTarget) {
        if (renderTarget is null)
            return;

        if (!renderTarget.Usage.HasFlag(TextureUsage.ColorAttachment))
            throw new InvalidOperationException("Camera render target must have TextureUsage.ColorAttachment flag.");

        if (renderTarget is Window) {
            if (!GraphicsDevice.Instance.PresentationEnabled) {
                if (!GraphicsDevice.Instance.SupportsPresentation) {
                    throw new PresentationNotSupportedException(
                        $"{nameof(GraphicsInstance)} used by {nameof(Camera)} is not support presentation."
                    );
                } else {
                    throw new PresentationNotSupportedException(
                        $"{nameof(GraphicsInstance)} used by {nameof(Camera)} has disabled presentation."
                    );
                }
            }

            if (!GraphicsDevice.SupportsPresentation) {
                throw new PresentationNotSupportedException(
                    $"{nameof(GraphicsDevice)} used by {nameof(Camera)} is not support presentation."
                );
            }
        }
    }

}
