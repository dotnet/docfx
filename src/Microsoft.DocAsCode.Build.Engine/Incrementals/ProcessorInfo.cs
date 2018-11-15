// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;

    using Newtonsoft.Json;

    public class ProcessorInfo : ProcessorInfoBase
    {
        /// <summary>
        /// The information for steps.
        /// </summary>
        public List<ProcessorStepInfo> Steps { get; } = new List<ProcessorStepInfo>();

        /// <summary>
        /// The file link for the BuildModel manifest file(type is <see cref="ModelManifest"/>).
        /// </summary>
        public string IntermediateModelManifestFile { get; set; }

        /// <summary>
        /// Get the list of invalid source files that fail to load.
        /// </summary>
        public ImmutableList<string> InvalidSourceFiles { get; set; } = ImmutableList.Create<string>();

        /// <summary>
        /// Deserialized build intermediate model manifest.
        /// </summary>
        [JsonIgnore]
        public ModelManifest IntermediateModelManifest { get; private set; } = new ModelManifest();

        internal void Load(string baseDir)
        {
            if (IntermediateModelManifestFile != null)
            {
                IntermediateModelManifest = IncrementalUtility.LoadIntermediateFile<ModelManifest>(Path.Combine(baseDir, IntermediateModelManifestFile));
            }
        }

        internal void Save(string baseDir)
        {
            if (IntermediateModelManifestFile == null)
            {
                IntermediateModelManifestFile = IncrementalUtility.CreateRandomFileName(baseDir);
            }
            IncrementalUtility.SaveIntermediateFile(Path.Combine(baseDir, IntermediateModelManifestFile), IntermediateModelManifest);
        }
    }
}
