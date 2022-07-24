﻿using NoiseEngine.Collections.Concurrent;
using NoiseEngine.Jobs;
using NoiseEngine.Logging;
using NoiseEngine.Rendering;
using NoiseEngine.Rendering.Presentation;
using NoiseEngine.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NoiseEngine;

public static class Application {

    private readonly static object exitLocker = new object();
    private readonly static ConcurrentList<ApplicationScene> loadedScenes = new ConcurrentList<ApplicationScene>();

    private static AtomicBool isInitialized;
    private static bool isExited;
    private static ApplicationSettings settings;

    public static string Name => Settings.Name!;
    public static EntitySchedule EntitySchedule => Settings.EntitySchedule!;

    public static IEnumerable<ApplicationScene> LoadedScenes => loadedScenes;
    public static IEnumerable<Window> Windows => LoadedScenes.SelectMany(x => x.Cameras).Select(x => x.RenderTarget);

    internal static ApplicationSettings Settings {
        get {
            if (!isInitialized)
                Initialize(new ApplicationSettings());
            return settings;
        }
    }

    public delegate void ApplicationExitHandler(int exitCode);

    public static event ApplicationExitHandler? ApplicationExit;

    /// <summary>
    /// Initializes <see cref="Application"/>.
    /// </summary>
    /// <remarks>
    /// This method is optional and will be called automatically with
    /// the default <see cref="ApplicationSettings"/> if not used.
    /// </remarks>
    /// <param name="settings">Application settings.</param>
    /// <exception cref="InvalidOperationException"><see cref="Application"/> has been already initialized.</exception>
    public static void Initialize(ApplicationSettings settings) {
        if (isInitialized.Exchange(true))
            throw new InvalidOperationException($"{nameof(Application)} has been already initialized.");

        AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnExit;

        if (settings.AddDefaultLoggerSinks) {
            if (!Log.Logger.Sinks.Any(x => typeof(ConsoleLogSink) == x.GetType()))
                Log.Logger.AddSink(new ConsoleLogSink(new ConsoleLogSinkSettings { ThreadNameLength = 20 }));
            if (!Log.Logger.Sinks.Any(x => typeof(FileLogSink) == x.GetType()))
                Log.Logger.AddSink(FileLogSink.CreateFromDirectory("logs"));
        }

        // Set default values.
        Application.settings = settings with {
            Name = settings.Name ?? Assembly.GetEntryAssembly()?.GetName().Name ?? Environment.ProcessId.ToString(),
            EntitySchedule = settings.EntitySchedule ?? new EntitySchedule()
        };
    }

    /// <summary>
    /// Disposes <see cref="Application"/> resources and when ProcessExitOnApplicationExit
    /// setting is <see langword="true"/> ends process with given <paramref name="exitCode"/>.
    /// </summary>
    /// <param name="exitCode">
    /// The exit code to return to the operating system. Use 0 (zero)
    /// to indicate that the process completed successfully.
    /// </param>
    public static void Exit(int exitCode = 0) {
        lock (exitLocker) {
            if (isExited)
                return;
            isExited = true;

            ApplicationExit?.Invoke(exitCode);

            foreach (ApplicationScene scene in LoadedScenes)
                scene.Dispose();

            EntitySchedule.Dispose();
            Graphics.Terminate();

            Log.Info($"{nameof(Application)} exited with code {exitCode}.");
            Log.Logger.Dispose();

            AppDomain.CurrentDomain.ProcessExit -= CurrentDomainOnExit;
            if (settings.ProcessExitOnApplicationExit)
                Environment.Exit(exitCode);
        }
    }

    internal static void AddSceneToLoaded(ApplicationScene scene) {
        loadedScenes.Add(scene);
    }

    internal static void RemoveSceneFromLoaded(ApplicationScene scene) {
        loadedScenes.Remove(scene);
    }

    private static void CurrentDomainOnExit(object? sender, EventArgs e) {
        string info = $"The process was closed without calling {nameof(Application.Exit)} method.";

        Log.Fatal(info);
        Log.Logger.Dispose();

        throw new ApplicationException(info);
    }

}
