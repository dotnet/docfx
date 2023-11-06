// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

using Docfx.Common;
using Docfx.Plugins;
using Spectre.Console;

namespace Docfx.Build.Engine;

static class CompilePhaseHandler
{
    public static void Handle(List<HostService> hostServices, DocumentBuildContext context)
    {
        if (context is not null)
        {
            foreach (var hostService in hostServices)
            {
                hostService.SourceFiles = context.AllSourceFiles;
                foreach (var m in hostService.Models)
                    m.LocalPathFromRoot ??= StringExtension.ToDisplayPath(Path.Combine(m.BaseDir, m.File));
            }
        }

        AnsiConsole.Progress().Start(progress =>
        {
            // Prebuild
            Parallel.ForEach(hostServices, Prebuild);

            // Update TOC
            var tocRestructions = hostServices.Where(h => h.TableOfContentRestructions is not null).SelectMany(h => h.TableOfContentRestructions).ToImmutableList();
            foreach (var hostService in hostServices)
            {
                hostService.TableOfContentRestructions = tocRestructions;
            }

            // Build
            var task = progress.AddTask("Build");
            task.MaxValue = hostServices.Sum(s => s.Models.Count);
            foreach (var hostService in hostServices)
            {
                Parallel.ForEach(hostService.Models, m =>
                {
                    using var _ = new LoggerFileScope(m.LocalPathFromRoot);
                    RunBuildSteps(hostService.Processor.BuildSteps, step => step.Build(m, hostService));
                    task.Increment(1);
                });
            }

            // Postbuild
            Parallel.ForEach(hostServices, Postbuild);
        });
    }

    private static void Prebuild(HostService hostService)
    {
        RunBuildSteps(
            hostService.Processor.BuildSteps,
            buildStep =>
            {
                using (new LoggerPhaseScope(buildStep.Name))
                {
                    var models = buildStep.Prebuild(hostService.Models, hostService);
                    if (!ReferenceEquals(models, hostService.Models))
                    {
                        Logger.LogVerbose($"Processor {hostService.Processor.Name}, step {buildStep.Name}: Reloading models...");
                        hostService.Reload(models);
                    }
                }
            });
    }

    private static void Postbuild(HostService hostService)
    {
        RunBuildSteps(
            hostService.Processor.BuildSteps,
            buildStep =>
            {
                using (new LoggerPhaseScope(buildStep.Name))
                {
                    buildStep.Postbuild(hostService.Models, hostService);
                }
            });
    }

    private static void RunBuildSteps(IEnumerable<IDocumentBuildStep> buildSteps, Action<IDocumentBuildStep> action)
    {
        if (buildSteps != null)
        {
            foreach (var buildStep in buildSteps.OrderBy(step => step.BuildOrder))
            {
                action(buildStep);
            }
        }
    }
}
