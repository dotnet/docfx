// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

using Docfx.Common;
using Docfx.Plugins;

namespace Docfx.Build.Engine;

internal class CompilePhaseHandler
{
    private readonly List<TreeItemRestructure> _restructions = [];

    public DocumentBuildContext Context { get; }

    public List<TreeItemRestructure> Restructions => _restructions;

    public CompilePhaseHandler(DocumentBuildContext context)
    {
        Context = context;
    }

    public void Handle(List<HostService> hostServices, int maxParallelism)
    {
        Prepare(hostServices, maxParallelism);
        hostServices.RunAll(hostService =>
        {
            var steps = string.Join("=>", hostService.Processor.BuildSteps.OrderBy(static step => step.BuildOrder).Select(static s => s.Name));
            Logger.LogInfo($"Building {hostService.Models.Count} file(s) in {hostService.Processor.Name}({steps})...");
            Logger.LogVerbose($"Processor {hostService.Processor.Name}: Prebuilding...");
            Prebuild(hostService);

            // Register all the delegates to handler
            if (hostService.TableOfContentRestructions != null)
            {
                lock (_restructions)
                {
                    _restructions.AddRange(hostService.TableOfContentRestructions);
                }
            }
        }, maxParallelism, Context.CancellationToken);

        DistributeTocRestructions(hostServices);

        foreach (var hostService in hostServices)
        {
            Logger.LogVerbose($"Processor {hostService.Processor.Name}: Building...");
            Build(hostService, maxParallelism);
        }

        hostServices.RunAll(Postbuild, maxParallelism, Context.CancellationToken);
    }

    private void Prepare(List<HostService> hostServices, int maxParallelism)
    {
        if (Context == null)
        {
            return;
        }
        foreach (var hostService in hostServices)
        {
            hostService.SourceFiles = Context.AllSourceFiles;
            hostService.Models.RunAll(
                m =>
                {
                    m.LocalPathFromRoot ??= StringExtension.ToDisplayPath(Path.Combine(m.BaseDir, m.File));
                },
                maxParallelism,
                Context.CancellationToken);
        }
    }

    private void DistributeTocRestructions(List<HostService> hostServices)
    {
        if (_restructions.Count > 0)
        {
            var restructions = _restructions.ToImmutableList();
            // Distribute delegates to all the hostServices
            foreach (var hostService in hostServices)
            {
                hostService.TableOfContentRestructions = restructions;
            }
        }
    }

    private void Prebuild(HostService hostService)
    {
        RunBuildSteps(
            hostService.Processor.BuildSteps,
            buildStep =>
            {
                Logger.LogVerbose($"Processor {hostService.Processor.Name}, step {buildStep.Name}: Prebuilding...");
                var models = buildStep.Prebuild(hostService.Models, hostService);
                if (!ReferenceEquals(models, hostService.Models))
                {
                    Logger.LogVerbose($"Processor {hostService.Processor.Name}, step {buildStep.Name}: Reloading models...");
                    hostService.Reload(models);
                }
            });
    }

    private void Build(HostService hostService, int maxParallelism)
    {
        hostService.Models.RunAll(
            m =>
            {
                using (new LoggerFileScope(m.LocalPathFromRoot))
                {
                    Logger.LogDiagnostic($"Processor {hostService.Processor.Name}: Building...");
                    RunBuildSteps(
                        hostService.Processor.BuildSteps,
                        buildStep =>
                        {
                            Logger.LogDiagnostic($"Processor {hostService.Processor.Name}, step {buildStep.Name}: Building...");
                            try
                            {
                                buildStep.Build(m, hostService);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError($"Trouble processing file - {m.FileAndType.FullPath}, with error - {ex.Message}");
                                throw;
                            }
                        });
                }
            },
            maxParallelism,
            Context.CancellationToken);
    }

    private void Postbuild(HostService hostService)
    {
        hostService.Reload(hostService.Models);

        RunBuildSteps(
            hostService.Processor.BuildSteps,
            buildStep =>
            {
                Logger.LogVerbose($"Processor {hostService.Processor.Name}, step {buildStep.Name}: Postbuilding...");
                buildStep.Postbuild(hostService.Models, hostService);
            });
    }

    private void RunBuildSteps(IEnumerable<IDocumentBuildStep> buildSteps, Action<IDocumentBuildStep> action)
    {
        if (buildSteps != null)
        {
            foreach (var buildStep in buildSteps.OrderBy(static step => step.BuildOrder))
            {
                Context.CancellationToken.ThrowIfCancellationRequested();

                action(buildStep);
            }
        }
    }
}
