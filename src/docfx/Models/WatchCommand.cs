// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Spectre.Console.Cli;

namespace Docfx;

internal class WatchCommand : Command<WatchCommandOptions>
{
    public override int Execute(CommandContext context, WatchCommandOptions settings)
    {
        return CommandHelper.Run(settings, () =>
        {
            var (config, baseDirectory) = Docset.GetConfig(settings.ConfigFile);
            BuildCommand.MergeOptionsToConfig(settings, config.build, baseDirectory);
            var conf = new BuildOptions();
            var serveDirectory = RunBuild.Exec(config.build, conf, baseDirectory, settings.OutputFolder);

            void onChange()
            {
                RunBuild.Exec(config.build, conf, baseDirectory, settings.OutputFolder);
            }

            if (settings is { Serve: true, Watch: true })
            {
                using var watcher = Watch(baseDirectory, config.build, onChange);
                Serve(serveDirectory, settings.Host, settings.Port, settings.OpenBrowser, settings.OpenFile);
            }
            else if (settings.Watch)
            {
                using var watcher = Watch(baseDirectory, config.build, onChange);

                // just block but here we can't use the host mecanism
                // since we didn't start the server so use console one
                using var canceller = new CancellationTokenSource();
                Console.CancelKeyPress += (sender, args) => canceller.Cancel();
                Task.Delay(Timeout.Infinite, canceller.Token).Wait();
            }
            else if (settings.Serve)
            {
                RunServe.Exec(serveDirectory, settings.Host, settings.Port, settings.OpenBrowser, settings.OpenFile);
            }
            else
            {
                onChange();
            }
        });
    }

    internal void Serve(string serveDirectory, string host, int? port, bool openBrowser, string openFile) {
        if (CommandHelper.IsTcpPortAlreadyUsed(host, port))
        {
            Logger.LogError($"Serve option specified. But TCP port {port ?? 8080} is already being in use.");
            return;
        }
        RunServe.Exec(serveDirectory, host, port, openBrowser, openFile);
    }

    // For now it is a simplistic implementation, in particular on the glob to filter mappping
    // but it should be sufficient for most cases.
    internal static IDisposable Watch(string baseDir, BuildJsonConfig config, Action onChange)
    {
        FileSystemWatcher watcher = new(baseDir)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.Attributes | NotifyFilters.Size | NotifyFilters.FileName |
                           NotifyFilters.DirectoryName | NotifyFilters.LastWrite
        };

        if (WatchAll(config))
        {
            watcher.Filters.Add("*.*");
        }
        else
        {
            RegisterFiles(watcher, config.Content);
            RegisterFiles(watcher, config.Resource);

            IEnumerable<string> forcedFiles = ["docfx.json", "*.md", "toc.yml"];
            foreach (var forcedFile in forcedFiles)
            {
                if (!watcher.Filters.Any(f => f == forcedFile))
                {
                    watcher.Filters.Add(forcedFile);
                }
            }
        }

        // avoid to call onChange() in chain so await "last" event before re-rendering
        var cancellation = new CancellationTokenSource[] { null };
        async void debounce()
        {
            var token = new CancellationTokenSource();
            lock (cancellation)
            {
                ResetToken(cancellation);
                cancellation[0] = token;
            }

            await Task.Delay(100, token.Token);
            if (!token.IsCancellationRequested)
            {
                onChange();
            }
        }

        watcher.Changed += (_, _) => debounce();
        watcher.Created += (_, _) => debounce();
        watcher.Deleted += (_, _) => debounce();
        watcher.Renamed += (_, _) => debounce();
        watcher.EnableRaisingEvents = true;

        return new DisposableAction(() =>
        {
            watcher.Dispose();
            lock (cancellation)
            {
                ResetToken(cancellation);
            }
        });
    }

    private static void ResetToken(CancellationTokenSource[] cancellation)
    {
        var token = cancellation[0];
        if (token is not null && !token.IsCancellationRequested)
        {
            token.Cancel();
            token.Dispose();
        }
    }

    internal static bool WatchAll(BuildJsonConfig config)
    {
        return ((IEnumerable<FileMapping>)[config.Resource, config.Content])
            .Where(it => it is not null)
            .SelectMany(it => it.Items)
            .SelectMany(it => it.Files)
            .Any(it => it.EndsWith("**"));
    }

    internal static void RegisterFiles(FileSystemWatcher watcher, FileMapping content)
    {
        foreach (var pattern in content?
            .Items?
            .SelectMany(it => it.Files)
            .SelectMany(SanitizePatternForWatcher)
            .Distinct()
            .ToList())
        {
            watcher.Filters.Add(pattern);
        }
    }

    // as of now it can list too much files but will less hurt to render more often with deboucning
    // than not rendering when needed.
    internal static IEnumerable<string> SanitizePatternForWatcher(string file)
    {
        var name = file[(file.LastIndexOf('.') + 1)..]; // "**/images/**/*.png" => "*.png"
        if (name.EndsWith('}')) // "**/*.{md,yml}" => "*.md" and "*.yml"
        {
            var start = name.IndexOf('{');
            if (start > 0)
            {
                var prefix = file[0..start];
                return file[(start + 1)..^1]
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(extension => $"{prefix}{extension}");
            }
        }
        return [name];
    }

    internal class DisposableAction(Action action) : IDisposable
    {
        public void Dispose() => action();
    }
}
