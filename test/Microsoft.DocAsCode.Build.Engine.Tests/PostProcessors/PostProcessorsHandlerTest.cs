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
    using Microsoft.DocAsCode.Tests.Common;

    using Xunit;

    [Trait("Owner", "jehuan")]
    [Collection("docfx STA")]
    public class PostProcessorsHandlerTest : TestBase
    {
        private const string MetaAppendContent = "-meta";
        private static readonly PostProcessorsHandler PostProcessorsHandler = new PostProcessorsHandler();

        [Fact]
        public void TestBasicScenario()
        {
            var manifest = JsonUtility.Deserialize<Manifest>("PostProcessors/Data/manifest_basic.json");
            var outputFolder = GetRandomFolder();
            PrepareOutput(outputFolder, "index");
            PostProcessorsHandler.Handle(GetPostProcessors(typeof(AppendStringPostProcessor)), manifest, outputFolder);
            VerifyOutput(outputFolder, AppendStringPostProcessor.AppendString, "index");
        }

        [Fact]
        public void TestIncrementalBasicScenario()
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
            PrepareCachedOutput(Path.Combine(intermediateFolder, lastBuildInfo.DirectoryName), lastBuildInfo.PostProcessInfo.PostProcessOutputs,
                AppendStringPostProcessor.AppendString, "a", "b");

            var postProcessors = GetPostProcessors(typeof(AppendStringPostProcessor));
            var increContext = new IncrementalPostProcessorsContext(intermediateFolder, currentBuildInfo, lastBuildInfo, postProcessors, true);

            // Check context
            Assert.True(increContext.ShouldTraceIncrementalInfo);
            Assert.True(increContext.CanIncremental);

            var increPostProcessorHandler = new PostProcessorsHandlerWithIncremental(PostProcessorsHandler, increContext);
            var manifest = JsonUtility.Deserialize<Manifest>("PostProcessors/Data/manifest_incremental.json");
            var outputFolder = GetRandomFolder();
            PrepareOutput(outputFolder, "a", "b", "c");
            increPostProcessorHandler.Handle(postProcessors, manifest, outputFolder);

            // Check incremental flag
            Assert.Equal(3, manifest.Files.Count);
            Assert.True(manifest.Files.Single(i => i.SourceRelativePath == "a.md").IsIncremental);
            Assert.True(manifest.Files.Single(i => i.SourceRelativePath == "b.md").IsIncremental);
            Assert.False(manifest.Files.Single(i => i.SourceRelativePath == "c.md").IsIncremental);

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
            VerifyCachedOutput(Path.Combine(intermediateFolder, currentBuildInfo.DirectoryName), postProcessOutputs, AppendStringPostProcessor.AppendString, "a", "b", "c");
        }

        [Fact]
        public void TestIncrementalWithFirstNonIncrementalButSecondIncremental()
        {
            // Step 1: trace intermediate info
            var intermediateFolder = GetRandomFolder();
            var currentBuildInfo = new BuildInfo
            {
                DirectoryName = IncrementalUtility.CreateRandomDirectory(intermediateFolder)
            };

            var postProcessors = GetPostProcessors(typeof(AppendStringPostProcessor), typeof(AppendIntegerPostProcessor));
            var appendString = $"{AppendStringPostProcessor.AppendString}{AppendIntegerPostProcessor.AppendInteger}";
            var increContext = new IncrementalPostProcessorsContext(intermediateFolder, currentBuildInfo, null, postProcessors, true);

            // Check context
            Assert.True(increContext.ShouldTraceIncrementalInfo);
            Assert.False(increContext.CanIncremental);

            var increPostProcessorHandler = new PostProcessorsHandlerWithIncremental(PostProcessorsHandler, increContext);
            var manifest = JsonUtility.Deserialize<Manifest>("PostProcessors/Data/manifest_incremental.json");
            var outputFolder = GetRandomFolder();
            PrepareOutput(outputFolder, "a", "b", "c");
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

            var postProcessOutputs = currentBuildInfo.PostProcessInfo.PostProcessOutputs;
            Assert.Equal(6, postProcessOutputs.Count);
            VerifyCachedOutput(Path.Combine(intermediateFolder, currentBuildInfo.DirectoryName), postProcessOutputs, appendString, "a", "b", "c");

            // Step 2: incremental post process
            currentBuildInfo = new BuildInfo
            {
                DirectoryName = IncrementalUtility.CreateRandomDirectory(intermediateFolder)
            };
            var lastBuildInfo = new BuildInfo
            {
                DirectoryName = Path.GetFileName(increContext.CurrentBaseDir),
                PostProcessInfo = increContext.CurrentInfo
            };
            increContext = new IncrementalPostProcessorsContext(intermediateFolder, currentBuildInfo, lastBuildInfo, postProcessors, true);

            // Check context
            Assert.True(increContext.ShouldTraceIncrementalInfo);
            Assert.True(increContext.CanIncremental);

            increPostProcessorHandler = new PostProcessorsHandlerWithIncremental(PostProcessorsHandler, increContext);
            manifest = JsonUtility.Deserialize<Manifest>("PostProcessors/Data/manifest_incremental.json");
            outputFolder = GetRandomFolder();
            PrepareOutput(outputFolder, "a", "b", "c");
            increPostProcessorHandler.Handle(postProcessors, manifest, outputFolder);

            // Check incremental flag
            Assert.Equal(3, manifest.Files.Count);
            Assert.True(manifest.Files.Single(i => i.SourceRelativePath == "a.md").IsIncremental);
            Assert.True(manifest.Files.Single(i => i.SourceRelativePath == "b.md").IsIncremental);
            Assert.False(manifest.Files.Single(i => i.SourceRelativePath == "c.md").IsIncremental);

            // Check output content
            VerifyOutput(outputFolder, appendString, "a", "b", "c");

            // Check cached PostProcessInfo
            Assert.NotNull(currentBuildInfo.PostProcessInfo);

            postProcessorInfos = currentBuildInfo.PostProcessInfo.PostProcessorInfos;
            Assert.Equal(2, currentBuildInfo.PostProcessInfo.PostProcessorInfos.Count);
            Assert.Equal($"{typeof(AppendStringPostProcessor).Name}", postProcessorInfos[0].Name);
            Assert.Null(postProcessorInfos[0].IncrementalContextHash);
            Assert.Equal($"{typeof(AppendIntegerPostProcessor).Name}", postProcessorInfos[1].Name);
            Assert.Equal(AppendIntegerPostProcessor.HashValue, postProcessorInfos[1].IncrementalContextHash);

            postProcessOutputs = currentBuildInfo.PostProcessInfo.PostProcessOutputs;
            Assert.Equal(6, postProcessOutputs.Count);
            VerifyCachedOutput(Path.Combine(intermediateFolder, currentBuildInfo.DirectoryName), postProcessOutputs, appendString, "a", "b", "c");
        }

        [Fact]
        public void TestIncrementalWithFileInDirectory()
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
            PrepareCachedOutput(Path.Combine(intermediateFolder, lastBuildInfo.DirectoryName), lastBuildInfo.PostProcessInfo.PostProcessOutputs,
                AppendStringPostProcessor.AppendString, "a/b");

            var postProcessors = GetPostProcessors(typeof(AppendStringPostProcessor));
            var increContext = new IncrementalPostProcessorsContext(intermediateFolder, currentBuildInfo, lastBuildInfo, postProcessors, true);

            // Check context
            Assert.True(increContext.ShouldTraceIncrementalInfo);
            Assert.True(increContext.CanIncremental);

            var increPostProcessorHandler = new PostProcessorsHandlerWithIncremental(PostProcessorsHandler, increContext);
            var manifest = JsonUtility.Deserialize<Manifest>("PostProcessors/Data/manifest_incremental_with_directory.json");
            var outputFolder = GetRandomFolder();
            PrepareOutput(outputFolder, "a/b", "c");
            increPostProcessorHandler.Handle(postProcessors, manifest, outputFolder);

            // Check incremental flag
            Assert.Equal(2, manifest.Files.Count);
            Assert.True(manifest.Files.Single(i => i.SourceRelativePath == "a/b.md").IsIncremental);
            Assert.False(manifest.Files.Single(i => i.SourceRelativePath == "c.md").IsIncremental);

            // Check output content
            VerifyOutput(outputFolder, AppendStringPostProcessor.AppendString, "a/b", "c");

            // Check cached PostProcessInfo
            Assert.NotNull(currentBuildInfo.PostProcessInfo);

            var postProcessorInfos = currentBuildInfo.PostProcessInfo.PostProcessorInfos;
            Assert.Equal(1, currentBuildInfo.PostProcessInfo.PostProcessorInfos.Count);
            Assert.Equal($"{typeof(AppendStringPostProcessor).Name}", postProcessorInfos[0].Name);
            Assert.Null(postProcessorInfos[0].IncrementalContextHash);

            var postProcessOutputs = currentBuildInfo.PostProcessInfo.PostProcessOutputs;
            Assert.Equal(4, postProcessOutputs.Count);
            VerifyCachedOutput(Path.Combine(intermediateFolder, currentBuildInfo.DirectoryName), postProcessOutputs, AppendStringPostProcessor.AppendString, "a/b", "c");
        }

        [Fact]
        public void TestIncrementalWithContextChange()
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
            var increContext = new IncrementalPostProcessorsContext(intermediateFolder, currentBuildInfo, lastBuildInfo, postProcessors, true);

            // Check context
            Assert.True(increContext.ShouldTraceIncrementalInfo);
            Assert.False(increContext.CanIncremental);

            var increPostProcessorHandler = new PostProcessorsHandlerWithIncremental(PostProcessorsHandler, increContext);
            var manifest = JsonUtility.Deserialize<Manifest>("PostProcessors/Data/manifest_incremental.json");
            var outputFolder = GetRandomFolder();
            PrepareOutput(outputFolder, "a", "b", "c");
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
            Assert.Equal(6, postProcessOutputs.Count);
            VerifyCachedOutput(Path.Combine(intermediateFolder, currentBuildInfo.DirectoryName), postProcessOutputs, AppendIntegerPostProcessor.AppendInteger, "a", "b", "c");
        }

        [Fact]
        public void TestIncrementalWithDisableFlag()
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
            var increContext = new IncrementalPostProcessorsContext(intermediateFolder, currentBuildInfo, lastBuildInfo, postProcessors, false);

            // Check context
            Assert.True(increContext.ShouldTraceIncrementalInfo);
            Assert.False(increContext.CanIncremental);
            Assert.False(increContext.EnableIncremental);

            var increPostProcessorHandler = new PostProcessorsHandlerWithIncremental(PostProcessorsHandler, increContext);
            var manifest = JsonUtility.Deserialize<Manifest>("PostProcessors/Data/manifest_incremental.json");
            var outputFolder = GetRandomFolder();
            PrepareOutput(outputFolder, "a", "b", "c");
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
            VerifyCachedOutput(Path.Combine(intermediateFolder, currentBuildInfo.DirectoryName), postProcessOutputs, AppendStringPostProcessor.AppendString, "a", "b", "c");
        }

        [Fact]
        public void TestIncrementalWithNullLastPostProcessInfo()
        {
            var intermediateFolder = GetRandomFolder();
            var currentBuildInfo = new BuildInfo
            {
                DirectoryName = IncrementalUtility.CreateRandomDirectory(intermediateFolder)
            };

            // Pass null as last build info
            var postProcessors = GetPostProcessors(typeof(AppendStringPostProcessor));
            var increContext = new IncrementalPostProcessorsContext(intermediateFolder, currentBuildInfo, null, postProcessors, true);

            // Check context
            Assert.True(increContext.ShouldTraceIncrementalInfo);
            Assert.False(increContext.CanIncremental);

            var increPostProcessorHandler = new PostProcessorsHandlerWithIncremental(PostProcessorsHandler, increContext);
            var manifest = JsonUtility.Deserialize<Manifest>("PostProcessors/Data/manifest_incremental.json");
            var outputFolder = GetRandomFolder();
            PrepareOutput(outputFolder, "a", "b", "c");
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
            VerifyCachedOutput(Path.Combine(intermediateFolder, currentBuildInfo.DirectoryName), postProcessOutputs, AppendStringPostProcessor.AppendString, "a", "b", "c");
        }

        [Fact]
        public void TestIncrementalWithNotSupportIncrementalPostProcessor()
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
            var increContext = new IncrementalPostProcessorsContext(intermediateFolder, currentBuildInfo, lastBuildInfo, postProcessors, true);

            // Check context
            Assert.False(increContext.ShouldTraceIncrementalInfo);
            Assert.False(increContext.CanIncremental);

            var increPostProcessorHandler = new PostProcessorsHandlerWithIncremental(PostProcessorsHandler, increContext);
            var manifest = JsonUtility.Deserialize<Manifest>("PostProcessors/Data/manifest_incremental.json");
            var outputFolder = GetRandomFolder();
            PrepareOutput(outputFolder, "a", "b", "c");
            increPostProcessorHandler.Handle(postProcessors, manifest, outputFolder);

            // Check incremental flag
            Assert.True(manifest.Files.All(f => f.IsIncremental == false));

            // Check output content should append nothing
            VerifyOutput(outputFolder, string.Empty, "a", "b", "c");

            // Check cached PostProcessInfo is null
            Assert.Null(currentBuildInfo.PostProcessInfo);
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
                Assert.Equal($"{fileName}{appendContent}", File.ReadAllText(Path.Combine(outputFolder, $"{fileName}.html")));
                Assert.Equal($"{fileName}{MetaAppendContent}", File.ReadAllText(Path.Combine(outputFolder, $"{fileName}.mta.json")));
            }
        }

        private static void PrepareCachedOutput(string baseFolder, PostProcessOutputs postProcessOutputs, string appendContent, params string[] fileNames)
        {
            foreach (var fileName in fileNames)
            {
                var cachedHtmlName = IncrementalUtility.CreateRandomFileName(baseFolder);
                CreateFile($"{cachedHtmlName}", $"{fileName}{appendContent}", baseFolder);
                postProcessOutputs.Add($"{fileName}.html", cachedHtmlName);

                var cachedMetaName = IncrementalUtility.CreateRandomFileName(baseFolder);
                CreateFile($"{cachedMetaName}", $"{fileName}{MetaAppendContent}", baseFolder);
                postProcessOutputs.Add($"{fileName}.mta.json", cachedMetaName);
            }
        }

        private static void VerifyCachedOutput(string baseDirectory, PostProcessOutputs postProcessOutputs, string appendContent, params string[] fileNames)
        {
            foreach (var fileName in fileNames)
            {
                Assert.Equal($"{fileName}{appendContent}", File.ReadAllText(Path.Combine(baseDirectory, postProcessOutputs.Single(o => o.Key == $"{fileName}.html").Value)));
                Assert.Equal($"{fileName}{MetaAppendContent}", File.ReadAllText(Path.Combine(baseDirectory, postProcessOutputs.Single(o => o.Key == $"{fileName}.mta.json").Value)));
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

        #endregion
    }
}
