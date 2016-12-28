// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System.Collections.Generic;
    using System.IO;

    using Newtonsoft.Json;

    public class PostProcessInfo
    {
        public const string FileName = "postprocess.info";

        #region Properties

        /// <summary>
        /// The post processor information
        /// </summary>
        public List<PostProcessorInfo> PostProcessorInfos { get; set; }
        /// <summary>
        /// The file link for post process outputs (type is <see cref="PostProcessOutputs"/>).
        /// </summary>
        public string PostProcessOutputsFile { get; set; }
        /// <summary>
        /// Deserialized post process outputs
        /// </summary>
        [JsonIgnore]
        public PostProcessOutputs PostProcessOutputs { get; private set; } = new PostProcessOutputs();

        #endregion

        internal void Load(string baseDir)
        {
            if (PostProcessOutputsFile != null)
            {
                PostProcessOutputs = IncrementalUtility.LoadIntermediateFile<PostProcessOutputs>(Path.Combine(baseDir, PostProcessOutputsFile));
            }
        }

        internal void Save(string baseDir)
        {
            if (PostProcessOutputsFile == null)
            {
                PostProcessOutputsFile = IncrementalUtility.CreateRandomFileName(baseDir);
            }
            IncrementalUtility.SaveIntermediateFile(Path.Combine(baseDir, PostProcessOutputsFile), PostProcessOutputs);
        }
    }
}
