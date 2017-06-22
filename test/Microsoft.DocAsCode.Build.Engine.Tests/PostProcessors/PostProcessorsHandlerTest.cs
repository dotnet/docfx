// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Engine.Incrementals;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    using Xunit;

    [Trait("Owner", "jehuan")]
    [Collection("docfx STA")]
    public class PostProcessorsHandlerTest : IncrementalTestBase
    {
        private const string MetaAppendContent = "-meta";
        private const string PrependIncrementalPhaseName = "TestIncrementalPostProcessing";
        private static readonly PostProcessorsHandler PostProcessorsHandler = new PostProcessorsHandler();
        private static readonly int MaxParallelism = Environment.ProcessorCount;

        [Fact]
        public void TestBasicScenario()
        {
            try
            {
                var manifest = JsonUtility.Deserialize<Manifest>(Path.GetFullPath("PostProcessors/Data/manifest_basic.json"));
                var outputFolder = GetRandomFolder();
                PrepareOutput(outputFolder, "index");
                SetDefaultFAL(manifest, outputFolder);
                PostProcessorsHandler.Handle(GetPostProcessors(typeof(AppendStringPostProcessor)), manifest, outputFolder);
                VerifyOutput(outputFolder, AppendStringPostProcessor.AppendString, "index");
            }
            finally
            {
                EnvironmentContext.Clean();
            }
        }

        [Fact]
        public void TestIncrementalBasicScenario()
        {
            try
            {
                const string intermediateFolderVariable = "%cache%";
                var intermediateFolder = GetRandomFolder();
                Environment.SetEnvironmentVariable("cache", intermediateFolder);
                var currentBuildInfo = new BuildInfo
                {
                    DirectoryName = IncrementalUtility.CreateRandomDirectory(intermediateFolderVariable)
                };
                var lastBuildInfo = new BuildInfo
                {
                    DirectoryName = IncrementalUtility.CreateRandomDirectory(intermediateFolderVariable),
                    PostProcessInfo = new PostProcessInfo()
                };
                lastBuildInfo.PostProcessInfo.PostProcessorInfos.Add(new PostProcessorInfo
                {
                    Name = typeof(AppendStringPostProcessor).Name
                });

                // Exclude c, which is not incremental
                var preparedManifest = JsonUtility.Deserialize<Manifest>(Path.GetFullPath("PostProcessors/Data/manifest_incremental.json"));
                PrepareCachedOutput(intermediateFolderVariable, lastBuildInfo, AppendStringPostProcessor.AppendString, preparedManifest.Files, AppendStringPostProcessor.AdditionalExtensionString, "a", "b");

                var postProcessors = GetPostProcessors(typeof(AppendStringPostProcessor));
                var increContext = new IncrementalPostProcessorsContext(intermediateFolderVariable, currentBuildInfo, lastBuildInfo, postProcessors, true, MaxParallelism);

                // Check context
                Assert.True(increContext.ShouldTraceIncrementalInfo);
                Assert.True(increContext.IsIncremental);

                var increPostProcessorHandler = new PostProcessorsHandlerWithIncremental(PostProcessorsHandler, increContext);
                var manifest = JsonUtility.Deserialize<Manifest>(Path.GetFullPath("PostProcessors/Data/manifest_incremental.json"));
                manifest.Files[1].SourceRelativePath = "B.md"; // Test file name case-insensitive
                var outputFolder = GetRandomFolder();
                PrepareOutput(outputFolder, "a", "b", "c");
                SetDefaultFAL(manifest, outputFolder);
                increPostProcessorHandler.Handle(postProcessors, manifest, outputFolder);

                // Check incremental flag
                Assert.Equal(3, manifest.Files.Count);
                Assert.True(manifest.Files.Single(i => i.SourceRelativePath == "a.md").IsIncremental);
                Assert.True(manifest.Files.Single(i => i.SourceRelativePath == "b.md").IsIncremental);
                Assert.False(manifest.Files.Single(i => i.SourceRelativePath == "c.md").IsIncremental);
                foreach (var file in manifest.Files)
                {
                    Assert.True(file.OutputFiles.ContainsKey(AppendStringPostProcessor.AdditionalExtensionString));
                }

                // Check output content
                VerifyOutput(outputFolder, AppendStringPostProcessor.AppendString, "a", "b", "c");

                // Check cached PostProcessInfo
                Assert.NotNull(currentBuildInfo.PostProcessInfo);

                var postProcessorInfos = currentBuildInfo.PostProcessInfo.PostProcessorInfos;
                Assert.Equal(1, currentBuildInfo.PostProcessInfo.PostProcessorInfos.Count);
                Assert.Equal($"{typeof(AppendStringPostProcessor).Name}", postProcessorInfos[0].Name);
                Assert.Null(postProcessorInfos[0].IncrementalContextHash);

                var postProcessOutputs = currentBuildInfo.PostProcessInfo.PostProcessOutputs;
                Assert.Equal(6, postProcessOutputs.Count); // no change for *.mta.json, so not in cache.
                VerifyCachedOutput(Path.Combine(intermediateFolder, currentBuildInfo.DirectoryName), postProcessOutputs, AppendStringPostProcessor.AppendString, AppendStringPostProcessor.AdditionalExtensionString, "a", "b", "c");

                // Check incremental info
                Assert.Equal(1, manifest.IncrementalInfo.Count);
                Assert.Equal(true, manifest.IncrementalInfo[0].Status.CanIncremental);
                Assert.Equal(IncrementalPhase.PostProcessing, manifest.IncrementalInfo[0].Status.IncrementalPhase);
                Assert.Equal("Can support incremental post processing.", manifest.IncrementalInfo[0].Status.Details);
            }
            finally
            {
                Environment.SetEnvironmentVariable("cache", null);
                EnvironmentContext.Clean();
            }
        }

        [Fact]
        public void TestIncrementalWithFirstCannotIncrementalButSecondCanIncremental()
        {
            //        | Should trace incremental info | Can incremental |
            // ---------------------------------------------------------
            // First  |               yes             |       no        |
            // Second |               yes             |       yes       |

            try
            {
                var intermediateFolder = GetRandomFolder();
                const string phaseName = PrependIncrementalPhaseName + "FirstCannotIncrementalButSecondCanIncremental";
                var postProcessors = GetPostProcessors(typeof(AppendStringPostProcessor), typeof(AppendIntegerPostProcessor));
                var appendString = $"{AppendStringPostProcessor.AppendString}{AppendIntegerPostProcessor.AppendInteger}";
                IncrementalPostProcessorsContext increContext = null;

                IncrementalActions
                    (phaseName, () =>
                    {
                        // Step 1: trace intermediate info
                        using (new LoggerPhaseScope(phaseName + "First"))
                        {
                            var currentBuildInfo = new BuildInfo
                            {
                                DirectoryName = IncrementalUtility.CreateRandomDirectory(intermediateFolder)
                            };
                            increContext = new IncrementalPostProcessorsContext(intermediateFolder, currentBuildInfo, null, postProcessors, true, MaxParallelism);

                            // Check context
                            Assert.True(increContext.ShouldTraceIncrementalInfo);
                            Assert.False(increContext.IsIncremental);

                            var increPostProcessorHandler = new PostProcessorsHandlerWithIncremental(PostProcessorsHandler, increContext);
                            var manifest = JsonUtility.Deserialize<Manifest>(Path.GetFullPath("PostProcessors/Data/manifest_incremental.json"));
                            var outputFolder = GetRandomFolder();
                            PrepareOutput(outputFolder, "a", "b", "c");
                            SetDefaultFAL(manifest, outputFolder);
                            increPostProcessorHandler.Handle(postProcessors, manifest, outputFolder);

                            // Check incremental flag
                            Assert.True(manifest.Files.All(f => f.IsIncremental == false));

                            // Check output content
                            VerifyOutput(outputFolder, appendString, "a", "b", "c");

                            // Check cached PostProcessInfo
                            Assert.NotNull(currentBuildInfo.PostProcessInfo);

                            var postProcessorInfos = currentBuildInfo.PostProcessInfo.PostProcessorInfos;
                            Assert.Equal(2, currentBuildInfo.PostProcessInfo.PostProcessorInfos.Count);
                            Assert.Equal($"{typeof(AppendStringPostProcessor).Name}", postProcessorInfos[0].Name);
                            Assert.Null(postProcessorInfos[0].IncrementalContextHash);
                            Assert.Equal($"{typeof(AppendIntegerPostProcessor).Name}", postProcessorInfos[1].Name);
                            Assert.Equal(AppendIntegerPostProcessor.HashValue, postProcessorInfos[1].IncrementalContextHash);
                            Assert.NotNull(postProcessorInfos[1].ContextInfoFile);
                            Assert.Equal(new List<string> { "a.html", "b.html", "c.html" },
                                JsonUtility.Deserialize<List<string>>(Path.Combine(Path.GetFullPath(intermediateFolder), currentBuildInfo.DirectoryName, postProcessorInfos[1].ContextInfoFile)));

                            Assert.Equal(3, currentBuildInfo.PostProcessInfo.ManifestItems.Count);
                            Assert.Equal<ManifestItem>(manifest.Files, currentBuildInfo.PostProcessInfo.ManifestItems);

                            var postProcessOutputs = currentBuildInfo.PostProcessInfo.PostProcessOutputs;
                            Assert.Equal(6, postProcessOutputs.Count);
                            VerifyCachedOutput(Path.Combine(intermediateFolder, currentBuildInfo.DirectoryName), postProcessOutputs, appendString, AppendStringPostProcessor.AdditionalExtensionString, "a", "b", "c");

                            // Check log messages
                            var logs = Listener.Items.Where(i => i.Phase.StartsWith(phaseName)).ToList();
                            Assert.Equal(3, logs.Count);
                            Assert.True(logs.All(l => l.Message.Contains("is not in html format.")));
                        }
                    }, () =>
                    {
                        // Step 2: incremental post process
                        using (new LoggerPhaseScope(phaseName + "Second"))
                        {
                            var secondBuildInfo = new BuildInfo
                            {
                                DirectoryName = IncrementalUtility.CreateRandomDirectory(intermediateFolder)
                            };
                            var dg = new DependencyGraph();
                            dg.ReportDependency(new[] {
                                new DependencyItem("~/a.md", "~/include.md", "~/a.md", DependencyTypeName.Include)
                            });
                            secondBuildInfo.Versions.Add(new BuildVersionInfo
                            {
                                Dependency = dg
                            });
                            var lastBuildInfo = new BuildInfo
                            {
                                DirectoryName = Path.GetFileName(increContext.CurrentBaseDir),
                                PostProcessInfo = increContext.CurrentInfo
                            };
                            // Add warning from include dependency.
                            lastBuildInfo.PostProcessInfo.MessageInfo.GetListener().WriteLine(new LogItem { File = "include.md", LogLevel = LogLevel.Warning, Message = "Invalid bookmark from include file.", Phase = phaseName });
                            increContext = new IncrementalPostProcessorsContext(intermediateFolder, secondBuildInfo, lastBuildInfo, postProcessors, true, MaxParallelism);

                            // Check context
                            Assert.True(increContext.ShouldTraceIncrementalInfo);
                            Assert.True(increContext.IsIncremental);

                            var increPostProcessorHandler = new PostProcessorsHandlerWithIncremental(PostProcessorsHandler, increContext);
                            var manifest = JsonUtility.Deserialize<Manifest>(Path.GetFullPath("PostProcessors/Data/manifest_incremental.json"));
                            var outputFolder = GetRandomFolder();
                            PrepareOutput(outputFolder, "a", "b", "c");
                            SetDefaultFAL(manifest, outputFolder);
                            increPostProcessorHandler.Handle(postProcessors, manifest, outputFolder);

                            // Check incremental flag
                            Assert.Equal(3, manifest.Files.Count);
                            Assert.True(manifest.Files.Single(i => i.SourceRelativePath == "a.md").IsIncremental);
                            Assert.True(manifest.Files.Single(i => i.SourceRelativePath == "b.md").IsIncremental);
                            Assert.False(manifest.Files.Single(i => i.SourceRelativePath == "c.md").IsIncremental);
                            foreach (var file in manifest.Files)
                            {
                                Assert.True(file.OutputFiles.ContainsKey(AppendStringPostProcessor.AdditionalExtensionString));
                            }

                            // Check output content
                            VerifyOutput(outputFolder, appendString, "a", "b", "c");

                            // Check cached PostProcessInfo
                            Assert.NotNull(secondBuildInfo.PostProcessInfo);

                            var postProcessorInfos = secondBuildInfo.PostProcessInfo.PostProcessorInfos;
                            Assert.Equal(2, secondBuildInfo.PostProcessInfo.PostProcessorInfos.Count);
                            Assert.Equal($"{typeof(AppendStringPostProcessor).Name}", postProcessorInfos[0].Name);
                            Assert.Null(postProcessorInfos[0].IncrementalContextHash);
                            Assert.Equal($"{typeof(AppendIntegerPostProcessor).Name}", postProcessorInfos[1].Name);
                            Assert.Equal(AppendIntegerPostProcessor.HashValue, postProcessorInfos[1].IncrementalContextHash);
                            Assert.NotNull(postProcessorInfos[1].ContextInfoFile);
                            Assert.Equal(new List<string> { "a.html", "b.html", "c.html" },
                                JsonUtility.Deserialize<List<string>>(Path.Combine(Path.GetFullPath(intermediateFolder), secondBuildInfo.DirectoryName, postProcessorInfos[1].ContextInfoFile)));

                            Assert.Equal(3, secondBuildInfo.PostProcessInfo.ManifestItems.Count);
                            Assert.Equal<ManifestItem>(manifest.Files, secondBuildInfo.PostProcessInfo.ManifestItems);

                            var postProcessOutputs = secondBuildInfo.PostProcessInfo.PostProcessOutputs;
                            Assert.Equal(6, postProcessOutputs.Count);
                            VerifyCachedOutput(Path.Combine(intermediateFolder, secondBuildInfo.DirectoryName), postProcessOutputs, appendString, AppendStringPostProcessor.AdditionalExtensionString, "a", "b", "c");

                            // Check log messages
                            var logs = Listener.Items.Where(i => i.Phase.StartsWith(phaseName)).ToList();
                            Assert.Equal(4, logs.Count);
                            Assert.Equal(3, logs.Count(l => l.Message.Contains("is not in html format.")));
                            Assert.Equal(1, logs.Count(l => l.Message.Contains("Invalid bookmark from include file."))); // Replay warning from include dependency.
                        }
                    });
            }
            finally
            {
                EnvironmentContext.Clean();
            }
        }

        [Fact]
        public void TestIncrementalWithFirstCanIncrementalButSecondCannotIncremental()
        {
            //        | Should trace incremental info | Can incremental |
            // ---------------------------------------------------------
            // First  |               yes             |       yes       |
            // Second |               yes             |       no        |

            try
            {
                // Step 1: trace intermediate info and post process incrementally
                var intermediateFolder = GetRandomFolder();
                var currentBuildInfo = new BuildInfo
                {
                    DirectoryName = IncrementalUtility.CreateRandomDirectory(intermediateFolder)
                };
                var lastBuildInfo = new BuildInfo
                {
                    DirectoryName = IncrementalUtility.CreateRandomDirectory(intermediateFolder),
                    PostProcessInfo = new PostProcessInfo()
                };
                lastBuildInfo.PostProcessInfo.PostProcessorInfos.Add(new PostProcessorInfo
                {
                    Name = typeof(AppendStringPostProcessor).Name
                });

                // Exclude c, which is not incremental
                var preparedManifest = JsonUtility.Deserialize<Manifest>(Path.GetFullPath("PostProcessors/Data/manifest_incremental.json"));
                PrepareCachedOutput(intermediateFolder, lastBuildInfo, AppendStringPostProcessor.AppendString, preparedManifest.Files, AppendStringPostProcessor.AdditionalExtensionString, "a", "b");

                var postProcessors = GetPostProcessors(typeof(AppendStringPostProcessor));
                var appendString = $"{AppendStringPostProcessor.AppendString}";
                var increContext = new IncrementalPostProcessorsContext(intermediateFolder, currentBuildInfo, lastBuildInfo, postProcessors, true, MaxParallelism);

                // Check context
                Assert.True(increContext.ShouldTraceIncrementalInfo);
                Assert.True(increContext.IsIncremental);

                var increPostProcessorHandler = new PostProcessorsHandlerWithIncremental(PostProcessorsHandler, increContext);
                var manifest = JsonUtility.Deserialize<Manifest>(Path.GetFullPath("PostProcessors/Data/manifest_incremental.json"));
                var outputFolder = GetRandomFolder();
                PrepareOutput(outputFolder, "a", "b", "c");
                SetDefaultFAL(manifest, outputFolder);
                increPostProcessorHandler.Handle(postProcessors, manifest, outputFolder);

                // Check incremental post processor host
                var host = ((ISupportIncrementalPostProcessor)postProcessors.Single().Processor).PostProcessorHost;
                Assert.NotNull(host);
                Assert.True(host.ShouldTraceIncrementalInfo);
                Assert.True(host.IsIncremental);
                Assert.Equal(3, host.SourceFileInfos.Count);
                Assert.Equal("Conceptual", host.SourceFileInfos.Select(f => f.DocumentType).Distinct().Single());
                Assert.NotNull(host.SourceFileInfos.Single(i => i.SourceRelativePath == "a.md"));
                Assert.NotNull(host.SourceFileInfos.Single(i => i.SourceRelativePath == "b.md"));
                Assert.NotNull(host.SourceFileInfos.Single(i => i.SourceRelativePath == "c.md"));

                // Check incremental flag
                Assert.Equal(3, manifest.Files.Count);
                Assert.True(manifest.Files.Single(i => i.SourceRelativePath == "a.md").IsIncremental);
                Assert.True(manifest.Files.Single(i => i.SourceRelativePath == "b.md").IsIncremental);
                Assert.False(manifest.Files.Single(i => i.SourceRelativePath == "c.md").IsIncremental);
                foreach (var file in manifest.Files)
                {
                    Assert.True(file.OutputFiles.ContainsKey(AppendStringPostProcessor.AdditionalExtensionString));
                }

                // Check output content
                VerifyOutput(outputFolder, appendString, "a", "b", "c");

                // Check cached PostProcessInfo
                Assert.NotNull(currentBuildInfo.PostProcessInfo);

                var postProcessorInfos = currentBuildInfo.PostProcessInfo.PostProcessorInfos;
                Assert.Equal(1, currentBuildInfo.PostProcessInfo.PostProcessorInfos.Count);
                Assert.Equal($"{typeof(AppendStringPostProcessor).Name}", postProcessorInfos[0].Name);
                Assert.Null(postProcessorInfos[0].IncrementalContextHash);

                Assert.Equal<ManifestItem>(manifest.Files, currentBuildInfo.PostProcessInfo.ManifestItems);

                var postProcessOutputs = currentBuildInfo.PostProcessInfo.PostProcessOutputs;
                Assert.Equal(6, postProcessOutputs.Count);
                VerifyCachedOutput(Path.Combine(intermediateFolder, currentBuildInfo.DirectoryName), postProcessOutputs, appendString, AppendStringPostProcessor.AdditionalExtensionString, "a", "b", "c");

                // Step 2: disable incremental post process
                const bool enableIncremental = false;
                currentBuildInfo = new BuildInfo
                {
                    DirectoryName = IncrementalUtility.CreateRandomDirectory(intermediateFolder)
                };
                lastBuildInfo = new BuildInfo
                {
                    DirectoryName = Path.GetFileName(increContext.CurrentBaseDir),
                    PostProcessInfo = increContext.CurrentInfo
                };
                increContext = new IncrementalPostProcessorsContext(intermediateFolder, currentBuildInfo, lastBuildInfo, postProcessors, enableIncremental, MaxParallelism);

                // Check context
                Assert.True(increContext.ShouldTraceIncrementalInfo);
                Assert.False(increContext.IsIncremental);

                increPostProcessorHandler = new PostProcessorsHandlerWithIncremental(PostProcessorsHandler, increContext);
                manifest = JsonUtility.Deserialize<Manifest>(Path.GetFullPath("PostProcessors/Data/manifest_incremental.json"));
                outputFolder = GetRandomFolder();
                PrepareOutput(outputFolder, "a", "b", "c");
                SetDefaultFAL(manifest, outputFolder);
                increPostProcessorHandler.Handle(postProcessors, manifest, outputFolder);

                // Check incremental post processor host
                host = ((ISupportIncrementalPostProcessor)postProcessors.Single().Processor).PostProcessorHost;
                Assert.NotNull(host);
                Assert.True(host.ShouldTraceIncrementalInfo);
                Assert.False(host.IsIncremental);
                Assert.Equal(3, host.SourceFileInfos.Count);
                Assert.Equal("Conceptual", host.SourceFileInfos.Select(f => f.DocumentType).Distinct().Single());
                Assert.NotNull(host.SourceFileInfos.Single(i => i.SourceRelativePath == "a.md"));
                Assert.NotNull(host.SourceFileInfos.Single(i => i.SourceRelativePath == "b.md"));
                Assert.NotNull(host.SourceFileInfos.Single(i => i.SourceRelativePath == "c.md"));

                // Check incremental flag
                Assert.True(manifest.Files.All(f => f.IsIncremental == false));

                // Check output content
                VerifyOutput(outputFolder, appendString, "a", "b", "c");

                // Check cached PostProcessInfo
                Assert.NotNull(currentBuildInfo.PostProcessInfo);

                postProcessorInfos = currentBuildInfo.PostProcessInfo.PostProcessorInfos;
                Assert.Equal(1, currentBuildInfo.PostProcessInfo.PostProcessorInfos.Count);
                Assert.Equal($"{typeof(AppendStringPostProcessor).Name}", postProcessorInfos[0].Name);
                Assert.Null(postProcessorInfos[0].IncrementalContextHash);

                Assert.Equal<ManifestItem>(manifest.Files, currentBuildInfo.PostProcessInfo.ManifestItems);

                postProcessOutputs = currentBuildInfo.PostProcessInfo.PostProcessOutputs;
                Assert.Equal(6, postProcessOutputs.Count);
                VerifyCachedOutput(Path.Combine(intermediateFolder, currentBuildInfo.DirectoryName), postProcessOutputs, appendString, AppendStringPostProcessor.AdditionalExtensionString, "a", "b", "c");
            }
            finally
            {
                EnvironmentContext.Clean();
            }
        }

        [Fact]
        public void TestIncrementalWithFirstCanIncrementalButSecondShouldnotTraceIncrementalInfo()
        {
            //        | Should trace incremental info | Can incremental |
            // ---------------------------------------------------------
            // First  |               yes             |       yes       |
            // Second |               no              |       no        |

            try
            {
                // Step 1: trace intermediate info and post process incrementally
                var intermediateFolder = GetRandomFolder();
                var currentBuildInfo = new BuildInfo
                {
                    DirectoryName = IncrementalUtility.CreateRandomDirectory(intermediateFolder)
                };
                var lastBuildInfo = new BuildInfo
                {
                    DirectoryName = IncrementalUtility.CreateRandomDirectory(intermediateFolder),
                    PostProcessInfo = new PostProcessInfo()
                };
                lastBuildInfo.PostProcessInfo.PostProcessorInfos.Add(new PostProcessorInfo
                {
                    Name = typeof(AppendStringPostProcessor).Name
                });

                // Exclude c, which is not incremental
                var preparedManifest = JsonUtility.Deserialize<Manifest>(Path.GetFullPath("PostProcessors/Data/manifest_incremental.json"));
                PrepareCachedOutput(intermediateFolder, lastBuildInfo, AppendStringPostProcessor.AppendString, preparedManifest.Files, AppendStringPostProcessor.AdditionalExtensionString, "a", "b");

                var postProcessors = GetPostProcessors(typeof(AppendStringPostProcessor));
                var appendString = $"{AppendStringPostProcessor.AppendString}";
                var increContext = new IncrementalPostProcessorsContext(intermediateFolder, currentBuildInfo, lastBuildInfo, postProcessors, true, MaxParallelism);

                // Check context
                Assert.True(increContext.ShouldTraceIncrementalInfo);
                Assert.True(increContext.IsIncremental);

                var increPostProcessorHandler = new PostProcessorsHandlerWithIncremental(PostProcessorsHandler, increContext);
                var manifest = JsonUtility.Deserialize<Manifest>(Path.GetFullPath("PostProcessors/Data/manifest_incremental.json"));
                var outputFolder = GetRandomFolder();
                PrepareOutput(outputFolder, "a", "b", "c");
                SetDefaultFAL(manifest, outputFolder);
                increPostProcessorHandler.Handle(postProcessors, manifest, outputFolder);

                // Check incremental flag
                Assert.Equal(3, manifest.Files.Count);
                Assert.True(manifest.Files.Single(i => i.SourceRelativePath == "a.md").IsIncremental);
                Assert.True(manifest.Files.Single(i => i.SourceRelativePath == "b.md").IsIncremental);
                Assert.False(manifest.Files.Single(i => i.SourceRelativePath == "c.md").IsIncremental);
                foreach (var file in manifest.Files)
                {
                    Assert.True(file.OutputFiles.ContainsKey(AppendStringPostProcessor.AdditionalExtensionString));
                }

                // Check output content
                VerifyOutput(outputFolder, appendString, "a", "b", "c");

                // Check cached PostProcessInfo
                Assert.NotNull(currentBuildInfo.PostProcessInfo);

                var postProcessorInfos = currentBuildInfo.PostProcessInfo.PostProcessorInfos;
                Assert.Equal(1, currentBuildInfo.PostProcessInfo.PostProcessorInfos.Count);
                Assert.Equal($"{typeof(AppendStringPostProcessor).Name}", postProcessorInfos[0].Name);
                Assert.Null(postProcessorInfos[0].IncrementalContextHash);

                Assert.Equal<ManifestItem>(manifest.Files, currentBuildInfo.PostProcessInfo.ManifestItems);

                var postProcessOutputs = currentBuildInfo.PostProcessInfo.PostProcessOutputs;
                Assert.Equal(6, postProcessOutputs.Count);
                VerifyCachedOutput(Path.Combine(intermediateFolder, currentBuildInfo.DirectoryName), postProcessOutputs, appendString, AppendStringPostProcessor.AdditionalExtensionString, "a", "b", "c");

                // Step 2: should not trace inter incremental post process
                currentBuildInfo = new BuildInfo
                {
                    DirectoryName = IncrementalUtility.CreateRandomDirectory(intermediateFolder)
                };
                lastBuildInfo = new BuildInfo
                {
                    DirectoryName = Path.GetFileName(increContext.CurrentBaseDir),
                    PostProcessInfo = increContext.CurrentInfo
                };

                // Add post processor which not supports incremental
                postProcessors.AddRange(GetPostProcessors(typeof(NonIncrementalPostProcessor)));
                increContext = new IncrementalPostProcessorsContext(intermediateFolder, currentBuildInfo, lastBuildInfo, postProcessors, true, MaxParallelism);

                // Check context
                Assert.False(increContext.ShouldTraceIncrementalInfo);
                Assert.False(increContext.IsIncremental);

                increPostProcessorHandler = new PostProcessorsHandlerWithIncremental(PostProcessorsHandler, increContext);
                manifest = JsonUtility.Deserialize<Manifest>(Path.GetFullPath("PostProcessors/Data/manifest_incremental.json"));
                outputFolder = GetRandomFolder();
                PrepareOutput(outputFolder, "a", "b", "c");
                SetDefaultFAL(manifest, outputFolder);
                increPostProcessorHandler.Handle(postProcessors, manifest, outputFolder);

                // Check incremental flag
                Assert.True(manifest.Files.All(f => f.IsIncremental == false));

                // Check output content
                VerifyOutput(outputFolder, appendString, "a", "b", "c");

                // Check cached PostProcessInfo should be null
                Assert.Null(currentBuildInfo.PostProcessInfo);
            }
            finally
            {
                EnvironmentContext.Clean();
            }
        }

        [Fact]
        public void TestIncrementalWithFileInDirectory()
        {
            try
            {
                var intermediateFolder = GetRandomFolder();
                var currentBuildInfo = new BuildInfo
                {
                    DirectoryName = IncrementalUtility.CreateRandomDirectory(intermediateFolder)
                };
                var lastBuildInfo = new BuildInfo
                {
                    DirectoryName = IncrementalUtility.CreateRandomDirectory(intermediateFolder),
                    PostProcessInfo = new PostProcessInfo()
                };
                lastBuildInfo.PostProcessInfo.PostProcessorInfos.Add(new PostProcessorInfo
                {
                    Name = typeof(AppendStringPostProcessor).Name
                });

                // Exclude c, which is not incremental
                var preparedManifest = JsonUtility.Deserialize<Manifest>(Path.GetFullPath("PostProcessors/Data/manifest_incremental_with_directory.json"));
                PrepareCachedOutput(intermediateFolder, lastBuildInfo, AppendStringPostProcessor.AppendString, preparedManifest.Files, AppendStringPostProcessor.AdditionalExtensionString, "a/b");

                var postProcessors = GetPostProcessors(typeof(AppendStringPostProcessor));
                var increContext = new IncrementalPostProcessorsContext(intermediateFolder, currentBuildInfo, lastBuildInfo, postProcessors, true, MaxParallelism);

                // Check context
                Assert.True(increContext.ShouldTraceIncrementalInfo);
                Assert.True(increContext.IsIncremental);

                var increPostProcessorHandler = new PostProcessorsHandlerWithIncremental(PostProcessorsHandler, increContext);
                var manifest = JsonUtility.Deserialize<Manifest>(Path.GetFullPath("PostProcessors/Data/manifest_incremental_with_directory.json"));
                var outputFolder = GetRandomFolder();
                PrepareOutput(outputFolder, "a/b", "c");
                CreateFile("breadcrumb.json", "breadcrumb", outputFolder);
                SetDefaultFAL(manifest, outputFolder);
                increPostProcessorHandler.Handle(postProcessors, manifest, outputFolder);

                // Check incremental flag
                Assert.Equal(3, manifest.Files.Count);
                Assert.True(manifest.Files.Single(i => i.SourceRelativePath == "a/b.md").IsIncremental);
                Assert.False(manifest.Files.Single(i => i.SourceRelativePath == "c.md").IsIncremental);
                Assert.False(manifest.Files.Single(i => i.SourceRelativePath == "breadcrumb.json").IsIncremental);

                // Check output content
                VerifyOutput(outputFolder, AppendStringPostProcessor.AppendString, "a/b", "c");
                Assert.Equal("breadcrumb", EnvironmentContext.FileAbstractLayer.ReadAllText("breadcrumb.json"));

                // Check cached PostProcessInfo
                Assert.NotNull(currentBuildInfo.PostProcessInfo);

                var postProcessorInfos = currentBuildInfo.PostProcessInfo.PostProcessorInfos;
                Assert.Equal(1, currentBuildInfo.PostProcessInfo.PostProcessorInfos.Count);
                Assert.Equal($"{typeof(AppendStringPostProcessor).Name}", postProcessorInfos[0].Name);
                Assert.Null(postProcessorInfos[0].IncrementalContextHash);

                Assert.Equal<ManifestItem>(manifest.Files, currentBuildInfo.PostProcessInfo.ManifestItems);

                var postProcessOutputs = currentBuildInfo.PostProcessInfo.PostProcessOutputs;
                Assert.Equal(4, postProcessOutputs.Count);
                VerifyCachedOutput(Path.Combine(intermediateFolder, currentBuildInfo.DirectoryName), postProcessOutputs, AppendStringPostProcessor.AppendString, AppendStringPostProcessor.AdditionalExtensionString, "a/b", "c");
            }
            finally
            {
                EnvironmentContext.Clean();
            }
        }

        [Fact]
        public void TestIncrementalWithContextChange()
        {
            try
            {
                var intermediateFolder = GetRandomFolder();
                var currentBuildInfo = new BuildInfo
                {
                    DirectoryName = IncrementalUtility.CreateRandomDirectory(intermediateFolder)
                };
                var lastBuildInfo = new BuildInfo
                {
                    DirectoryName = IncrementalUtility.CreateRandomDirectory(intermediateFolder),
                    PostProcessInfo = new PostProcessInfo()
                };
                lastBuildInfo.PostProcessInfo.PostProcessorInfos.Add(new PostProcessorInfo
                {
                    Name = typeof(AppendIntegerPostProcessor).Name
                });

                // Add post processor which has changed context hash
                var postProcessors = GetPostProcessors(typeof(AppendIntegerPostProcessor));
                var increContext = new IncrementalPostProcessorsContext(intermediateFolder, currentBuildInfo, lastBuildInfo, postProcessors, true, MaxParallelism);

                // Check context
                Assert.True(increContext.ShouldTraceIncrementalInfo);
                Assert.False(increContext.IsIncremental);

                var increPostProcessorHandler = new PostProcessorsHandlerWithIncremental(PostProcessorsHandler, increContext);
                var manifest = JsonUtility.Deserialize<Manifest>(Path.GetFullPath("PostProcessors/Data/manifest_incremental.json"));
                var outputFolder = GetRandomFolder();
                PrepareOutput(outputFolder, "a", "b", "c");
                SetDefaultFAL(manifest, outputFolder);
                increPostProcessorHandler.Handle(postProcessors, manifest, outputFolder);

                // Check incremental flag
                Assert.True(manifest.Files.All(f => f.IsIncremental == false));

                // Check output content
                VerifyOutput(outputFolder, AppendIntegerPostProcessor.AppendInteger, "a", "b", "c");

                // Check cached PostProcessInfo
                Assert.NotNull(currentBuildInfo.PostProcessInfo);

                var postProcessorInfos = currentBuildInfo.PostProcessInfo.PostProcessorInfos;
                Assert.Equal(1, currentBuildInfo.PostProcessInfo.PostProcessorInfos.Count);
                Assert.Equal($"{typeof(AppendIntegerPostProcessor).Name}", postProcessorInfos[0].Name);
                Assert.NotNull(postProcessorInfos[0].IncrementalContextHash);

                var postProcessOutputs = currentBuildInfo.PostProcessInfo.PostProcessOutputs;
                Assert.Equal(3, postProcessOutputs.Count);
                VerifyCachedOutput(Path.Combine(intermediateFolder, currentBuildInfo.DirectoryName), postProcessOutputs, AppendIntegerPostProcessor.AppendInteger, null, "a", "b", "c");

                Assert.Equal<ManifestItem>(manifest.Files, currentBuildInfo.PostProcessInfo.ManifestItems);

                // Check incremental info
                Assert.Equal(1, manifest.IncrementalInfo.Count);
                Assert.Equal(false, manifest.IncrementalInfo[0].Status.CanIncremental);
                Assert.Equal(IncrementalPhase.PostProcessing, manifest.IncrementalInfo[0].Status.IncrementalPhase);
                Assert.Equal(@"Cannot support incremental post processing, the reason is: post processor info changed from last {""Name"":""AppendIntegerPostProcessor""} to current {""Name"":""AppendIntegerPostProcessor"",""IncrementalContextHash"":""1024""}.",
                    manifest.IncrementalInfo[0].Status.Details);
            }
            finally
            {
                EnvironmentContext.Clean();
            }
        }

        [Fact]
        public void TestIncrementalWithDisableFlag()
        {
            try
            {
                var intermediateFolder = GetRandomFolder();
                var currentBuildInfo = new BuildInfo
                {
                    DirectoryName = IncrementalUtility.CreateRandomDirectory(intermediateFolder)
                };
                var lastBuildInfo = new BuildInfo
                {
                    DirectoryName = IncrementalUtility.CreateRandomDirectory(intermediateFolder),
                    PostProcessInfo = new PostProcessInfo()
                };
                lastBuildInfo.PostProcessInfo.PostProcessorInfos.Add(new PostProcessorInfo
                {
                    Name = typeof(AppendStringPostProcessor).Name
                });

                // Set enable incremental post process flag to false
                var postProcessors = GetPostProcessors(typeof(AppendStringPostProcessor));
                var increContext = new IncrementalPostProcessorsContext(intermediateFolder, currentBuildInfo, lastBuildInfo, postProcessors, false, MaxParallelism);

                // Check context
                Assert.True(increContext.ShouldTraceIncrementalInfo);
                Assert.False(increContext.IsIncremental);
                Assert.False(increContext.EnableIncremental);

                var increPostProcessorHandler = new PostProcessorsHandlerWithIncremental(PostProcessorsHandler, increContext);
                var manifest = JsonUtility.Deserialize<Manifest>(Path.GetFullPath("PostProcessors/Data/manifest_incremental.json"));
                var outputFolder = GetRandomFolder();
                PrepareOutput(outputFolder, "a", "b", "c");
                SetDefaultFAL(manifest, outputFolder);
                increPostProcessorHandler.Handle(postProcessors, manifest, outputFolder);

                // Check incremental flag
                Assert.True(manifest.Files.All(f => f.IsIncremental == false));

                // Check output content
                VerifyOutput(outputFolder, AppendStringPostProcessor.AppendString, "a", "b", "c");

                // Check cached PostProcessInfo
                Assert.NotNull(currentBuildInfo.PostProcessInfo);

                var postProcessorInfos = currentBuildInfo.PostProcessInfo.PostProcessorInfos;
                Assert.Equal(1, currentBuildInfo.PostProcessInfo.PostProcessorInfos.Count);
                Assert.Equal($"{typeof(AppendStringPostProcessor).Name}", postProcessorInfos[0].Name);
                Assert.Null(postProcessorInfos[0].IncrementalContextHash);

                var postProcessOutputs = currentBuildInfo.PostProcessInfo.PostProcessOutputs;
                Assert.Equal(6, postProcessOutputs.Count);
                VerifyCachedOutput(Path.Combine(intermediateFolder, currentBuildInfo.DirectoryName), postProcessOutputs, AppendStringPostProcessor.AppendString, AppendStringPostProcessor.AdditionalExtensionString, "a", "b", "c");

                Assert.Equal<ManifestItem>(manifest.Files, currentBuildInfo.PostProcessInfo.ManifestItems);

                // Check incremental info
                Assert.Equal(1, manifest.IncrementalInfo.Count);
                Assert.Equal(false, manifest.IncrementalInfo[0].Status.CanIncremental);
                Assert.Equal(IncrementalPhase.PostProcessing, manifest.IncrementalInfo[0].Status.IncrementalPhase);
                Assert.Equal("Cannot support incremental post processing, the reason is: it's disabled.", manifest.IncrementalInfo[0].Status.Details);
            }
            finally
            {
                EnvironmentContext.Clean();
            }
        }

        [Fact]
        public void TestIncrementalWithNullLastPostProcessInfo()
        {
            try
            {
                var intermediateFolder = GetRandomFolder();
                var currentBuildInfo = new BuildInfo
                {
                    DirectoryName = IncrementalUtility.CreateRandomDirectory(intermediateFolder)
                };

                // Pass null as last build info
                var postProcessors = GetPostProcessors(typeof(AppendStringPostProcessor));
                var increContext = new IncrementalPostProcessorsContext(intermediateFolder, currentBuildInfo, null, postProcessors, true, MaxParallelism);

                // Check context
                Assert.True(increContext.ShouldTraceIncrementalInfo);
                Assert.False(increContext.IsIncremental);

                var increPostProcessorHandler = new PostProcessorsHandlerWithIncremental(PostProcessorsHandler, increContext);
                var manifest = JsonUtility.Deserialize<Manifest>(Path.GetFullPath("PostProcessors/Data/manifest_incremental.json"));
                var outputFolder = GetRandomFolder();
                PrepareOutput(outputFolder, "a", "b", "c");
                SetDefaultFAL(manifest, outputFolder);
                increPostProcessorHandler.Handle(postProcessors, manifest, outputFolder);

                // Check incremental flag
                Assert.True(manifest.Files.All(f => f.IsIncremental == false));

                // Check output content
                VerifyOutput(outputFolder, AppendStringPostProcessor.AppendString, "a", "b", "c");

                // Check cached PostProcessInfo
                Assert.NotNull(currentBuildInfo.PostProcessInfo);

                var postProcessorInfos = currentBuildInfo.PostProcessInfo.PostProcessorInfos;
                Assert.Equal(1, currentBuildInfo.PostProcessInfo.PostProcessorInfos.Count);
                Assert.Equal($"{typeof(AppendStringPostProcessor).Name}", postProcessorInfos[0].Name);
                Assert.Null(postProcessorInfos[0].IncrementalContextHash);

                var postProcessOutputs = currentBuildInfo.PostProcessInfo.PostProcessOutputs;
                Assert.Equal(6, postProcessOutputs.Count);
                VerifyCachedOutput(Path.Combine(intermediateFolder, currentBuildInfo.DirectoryName), postProcessOutputs, AppendStringPostProcessor.AppendString, AppendStringPostProcessor.AdditionalExtensionString, "a", "b", "c");

                Assert.Equal<ManifestItem>(manifest.Files, currentBuildInfo.PostProcessInfo.ManifestItems);

                // Check incremental info
                Assert.Equal(1, manifest.IncrementalInfo.Count);
                Assert.Equal(false, manifest.IncrementalInfo[0].Status.CanIncremental);
                Assert.Equal(IncrementalPhase.PostProcessing, manifest.IncrementalInfo[0].Status.IncrementalPhase);
                Assert.Equal("Cannot support incremental post processing, the reason is: last post processor info is null.", manifest.IncrementalInfo[0].Status.Details);
            }
            finally
            {
                EnvironmentContext.Clean();
            }
        }

        [Fact]
        public void TestIncrementalWithNotSupportIncrementalPostProcessor()
        {
            try
            {
                var intermediateFolder = GetRandomFolder();
                var currentBuildInfo = new BuildInfo
                {
                    DirectoryName = IncrementalUtility.CreateRandomDirectory(intermediateFolder)
                };
                var lastBuildInfo = new BuildInfo
                {
                    DirectoryName = IncrementalUtility.CreateRandomDirectory(intermediateFolder),
                    PostProcessInfo = new PostProcessInfo()
                };
                lastBuildInfo.PostProcessInfo.PostProcessorInfos.Add(new PostProcessorInfo
                {
                    Name = typeof(NonIncrementalPostProcessor).Name
                });

                // Add not post processor which not support incremental
                var postProcessors = GetPostProcessors(typeof(NonIncrementalPostProcessor));
                var increContext = new IncrementalPostProcessorsContext(intermediateFolder, currentBuildInfo, lastBuildInfo, postProcessors, true, MaxParallelism);

                // Check context
                Assert.False(increContext.ShouldTraceIncrementalInfo);
                Assert.False(increContext.IsIncremental);

                var increPostProcessorHandler = new PostProcessorsHandlerWithIncremental(PostProcessorsHandler, increContext);
                var manifest = JsonUtility.Deserialize<Manifest>(Path.GetFullPath("PostProcessors/Data/manifest_incremental.json"));
                var outputFolder = GetRandomFolder();
                PrepareOutput(outputFolder, "a", "b", "c");
                SetDefaultFAL(manifest, outputFolder);
                increPostProcessorHandler.Handle(postProcessors, manifest, outputFolder);

                // Check incremental flag
                Assert.True(manifest.Files.All(f => f.IsIncremental == false));

                // Check output content should append nothing
                VerifyOutput(outputFolder, string.Empty, "a", "b", "c");

                // Check cached PostProcessInfo is null
                Assert.Null(currentBuildInfo.PostProcessInfo);

                // Check incremental info
                Assert.Equal(1, manifest.IncrementalInfo.Count);
                Assert.Equal(false, manifest.IncrementalInfo[0].Status.CanIncremental);
                Assert.Equal(IncrementalPhase.PostProcessing, manifest.IncrementalInfo[0].Status.IncrementalPhase);
                Assert.Equal("Cannot support incremental post processing, the reason is: should not trace intermediate info.", manifest.IncrementalInfo[0].Status.Details);
            }
            finally
            {
                EnvironmentContext.Clean();
            }
        }

        [Fact]
        public void TestIncrementalSplitScenario()
        {
            try
            {
                const string intermediateFolderVariable = "%cache%";
                var intermediateFolder = GetRandomFolder();
                Environment.SetEnvironmentVariable("cache", intermediateFolder);
                string phaseName = "TestIncrementalSplitScenario";
                var currentBuildInfo = new BuildInfo
                {
                    DirectoryName = IncrementalUtility.CreateRandomDirectory(intermediateFolderVariable)
                };
                var lastBuildInfo = new BuildInfo
                {
                    DirectoryName = IncrementalUtility.CreateRandomDirectory(intermediateFolderVariable),
                    PostProcessInfo = new PostProcessInfo()
                };
                lastBuildInfo.PostProcessInfo.PostProcessorInfos.Add(new PostProcessorInfo
                {
                    Name = typeof(AppendStringPostProcessor).Name
                });
                lastBuildInfo.PostProcessInfo.MessageInfo.GetListener().WriteLine(new LogItem { File = "CatLibrary.Cat-2.yml", LogLevel = LogLevel.Warning, Message = "Invalid bookmark.", Phase = phaseName });

                // Exclude c, which is not incremental
                var preparedManifest = JsonUtility.Deserialize<Manifest>(Path.GetFullPath("PostProcessors/Data/manifest_incremental_split_case.json"));
                PrepareCachedOutput(intermediateFolderVariable, lastBuildInfo, AppendStringPostProcessor.AppendString, preparedManifest.Files, AppendStringPostProcessor.AdditionalExtensionString, "CatLibrary.Cat-2", "CatLibrary.Cat-2.Name");

                var postProcessors = GetPostProcessors(typeof(AppendStringPostProcessor));
                var increContext = new IncrementalPostProcessorsContext(intermediateFolderVariable, currentBuildInfo, lastBuildInfo, postProcessors, true, MaxParallelism);

                // Check context
                Assert.True(increContext.ShouldTraceIncrementalInfo);
                Assert.True(increContext.IsIncremental);

                var increPostProcessorHandler = new PostProcessorsHandlerWithIncremental(PostProcessorsHandler, increContext);
                var manifest = JsonUtility.Deserialize<Manifest>(Path.GetFullPath("PostProcessors/Data/manifest_incremental_split_case.json"));
                var outputFolder = GetRandomFolder();
                PrepareOutput(outputFolder, "CatLibrary.Cat-2", "CatLibrary.Cat-2.Name", "c");
                SetDefaultFAL(manifest, outputFolder);
                IncrementalActions
                    (phaseName, () =>
                    {
                        using (new LoggerPhaseScope(phaseName))
                        {
                            increPostProcessorHandler.Handle(postProcessors, manifest, outputFolder);
                        }

                        // Check incremental flag
                        Assert.Equal(3, manifest.Files.Count);
                        Assert.True(manifest.Files.Single(i => i.OutputFiles[".html"].RelativePath == "CatLibrary.Cat-2.html").IsIncremental);
                        Assert.True(manifest.Files.Single(i => i.OutputFiles[".html"].RelativePath == "CatLibrary.Cat-2.Name.html").IsIncremental);
                        Assert.False(manifest.Files.Single(i => i.SourceRelativePath == "c.md").IsIncremental);
                        foreach (var file in manifest.Files)
                        {
                            Assert.True(file.OutputFiles.ContainsKey(AppendStringPostProcessor.AdditionalExtensionString));
                        }

                        // Check output content
                        VerifyOutput(outputFolder, AppendStringPostProcessor.AppendString, "CatLibrary.Cat-2", "CatLibrary.Cat-2.Name", "c");

                        // Check cached PostProcessInfo
                        Assert.NotNull(currentBuildInfo.PostProcessInfo);

                        var postProcessorInfos = currentBuildInfo.PostProcessInfo.PostProcessorInfos;
                        Assert.Equal(1, currentBuildInfo.PostProcessInfo.PostProcessorInfos.Count);
                        Assert.Equal($"{typeof(AppendStringPostProcessor).Name}", postProcessorInfos[0].Name);
                        Assert.Null(postProcessorInfos[0].IncrementalContextHash);

                        var postProcessOutputs = currentBuildInfo.PostProcessInfo.PostProcessOutputs;
                        Assert.Equal(6, postProcessOutputs.Count); // no change for *.mta.json, so not in cache.
                        VerifyCachedOutput(Path.Combine(intermediateFolder, currentBuildInfo.DirectoryName), postProcessOutputs, AppendStringPostProcessor.AppendString, AppendStringPostProcessor.AdditionalExtensionString, "CatLibrary.Cat-2", "CatLibrary.Cat-2.Name", "c");

                        // Check incremental info
                        Assert.Equal(1, manifest.IncrementalInfo.Count);
                        Assert.Equal(true, manifest.IncrementalInfo[0].Status.CanIncremental);
                        Assert.Equal(IncrementalPhase.PostProcessing, manifest.IncrementalInfo[0].Status.IncrementalPhase);
                        Assert.Equal("Can support incremental post processing.", manifest.IncrementalInfo[0].Status.Details);

                        // Check log messages
                        var logs = Listener.Items.Where(i => i.Phase.StartsWith(phaseName)).ToList();
                        Assert.Equal(2, logs.Count);
                        Assert.True(logs.Count(l => l.Message.Contains("Invalid bookmark.")) == 1);
                    });
            }
            finally
            {
                Environment.SetEnvironmentVariable("cache", null);
                EnvironmentContext.Clean();
            }
        }

        #region Private methods

        private static void PrepareOutput(string outputFolder, params string[] fileNames)
        {
            foreach (var fileName in fileNames)
            {
                CreateFile($"{fileName}.html", $"{fileName}", outputFolder);
                CreateFile($"{fileName}.mta.json", $"{fileName}{MetaAppendContent}", outputFolder);
            }
        }

        private static void VerifyOutput(string outputFolder, string appendContent, params string[] fileNames)
        {
            foreach (var fileName in fileNames)
            {
                Assert.Equal($"{fileName}{appendContent}", EnvironmentContext.FileAbstractLayer.ReadAllText($"{fileName}.html"));
                Assert.Equal($"{fileName}{MetaAppendContent}", EnvironmentContext.FileAbstractLayer.ReadAllText($"{fileName}.mta.json"));
            }
        }

        private static void PrepareCachedOutput(
            string intermediateFolder,
            BuildInfo lastBuildInfo,
            string appendContent,
            ManifestItemCollection manifestItems,
            string additionalFileExtension,
            params string[] fileNames)
        {
            var baseFolder = Path.Combine(Environment.ExpandEnvironmentVariables(intermediateFolder), lastBuildInfo.DirectoryName);
            var postProcessOutputs = lastBuildInfo.PostProcessInfo.PostProcessOutputs;
            foreach (var fileName in fileNames)
            {
                var cachedHtmlName = IncrementalUtility.CreateRandomFileName(baseFolder);
                var htmlContent = $"{fileName}{appendContent}";
                CreateFile($"{cachedHtmlName}", htmlContent, baseFolder);
                postProcessOutputs.Add($"{fileName}.html", cachedHtmlName);
                manifestItems.First(i => Path.ChangeExtension(i.OutputFiles[".html"].RelativePath, null) == fileName).OutputFiles[".html"].LinkToPath = $@"{intermediateFolder}\{lastBuildInfo.DirectoryName}\test";

                var cachedMetaName = IncrementalUtility.CreateRandomFileName(baseFolder);
                CreateFile($"{cachedMetaName}", $"{fileName}{MetaAppendContent}", baseFolder);
                postProcessOutputs.Add($"{fileName}.mta.json", cachedMetaName);

                if (!string.IsNullOrEmpty(additionalFileExtension))
                {
                    var relativePath = $"{fileName}{additionalFileExtension}";
                    var cachedManifestItemsFileName = IncrementalUtility.CreateRandomFileName(baseFolder);
                    CreateFile($"{cachedManifestItemsFileName}", htmlContent, baseFolder);
                    postProcessOutputs.Add(relativePath, cachedManifestItemsFileName);

                    var item = manifestItems.FirstOrDefault(i => Path.ChangeExtension(i.OutputFiles[".html"].RelativePath, null) == fileName);
                    if (item != null)
                    {
                        item.OutputFiles.Add($"{additionalFileExtension}", new OutputFileInfo { RelativePath = relativePath, LinkToPath = $@"{intermediateFolder}\{lastBuildInfo.DirectoryName}\test" });
                        lastBuildInfo.PostProcessInfo.ManifestItems.Add(item);
                    }
                }
            }
        }

        private static void VerifyCachedOutput(string baseDirectory, PostProcessOutputs postProcessOutputs, string appendContent, string additionalFileExtension, params string[] fileNames)
        {
            foreach (var fileName in fileNames)
            {
                var htmlContent = $"{fileName}{appendContent}";
                Assert.Equal(htmlContent, File.ReadAllText(Path.Combine(baseDirectory, postProcessOutputs[$"{fileName}.html"])));
                Assert.False(postProcessOutputs.ContainsKey($"{fileName}.mta.json"));
                if (!string.IsNullOrEmpty(additionalFileExtension))
                {
                    Assert.True(File.Exists(Path.Combine(baseDirectory, postProcessOutputs[$"{fileName}{additionalFileExtension}"])));
                }
            }
        }

        private static List<PostProcessor> GetPostProcessors(params Type[] types)
        {
            var result = new List<PostProcessor>();
            foreach (var type in types)
            {
                var instance = Activator.CreateInstance(type);
                var postProcessor = instance as IPostProcessor;
                if (postProcessor == null)
                {
                    throw new InvalidOperationException($"{type} should implement {nameof(IPostProcessor)}.");
                }

                result.Add(new PostProcessor
                {
                    ContractName = type.Name,
                    Processor = postProcessor
                });
            }
            return result;
        }

        private static void SetDefaultFAL(Manifest manifest, string outputFolder)
        {
            EnvironmentContext.FileAbstractLayerImpl =
                FileAbstractLayerBuilder.Default
                .ReadFromManifest(manifest, outputFolder)
                .WriteToManifest(manifest, outputFolder)
                .Create();
        }
        #endregion

        private class LogItem : ILogItem
        {
            public string File { get; set; }

            public string Line { get; set; }

            public LogLevel LogLevel { get; set; }

            public string Message { get; set; }

            public string Phase { get; set; }

            public string Code { get; set; }

            public string CorrelationId { get; set; }
        }
    }
}
