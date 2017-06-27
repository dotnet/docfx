// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json;

    public class PostProcessInfo
    {
        public const string FileName = "postprocess.info";
        private static readonly Encoding UTF8 = new UTF8Encoding(false, false);

        #region Properties

        /// <summary>
        /// The post processor information.
        /// </summary>
        public List<PostProcessorInfo> PostProcessorInfos { get; set; } = new List<PostProcessorInfo>();
        /// <summary>
        /// The file link for post process outputs (type is <see cref="PostProcessOutputs"/>).
        /// </summary>
        public string PostProcessOutputsFile { get; set; }
        /// <summary>
        /// The file link for the log message file.
        /// </summary>
        public string MessageInfoFile { get; set; }
        /// <summary>
        /// The file link for the manifest items file.
        /// </summary>
        public string ManifestItemsFile { get; set; }
        /// <summary>
        /// Deserialized post process outputs.
        /// </summary>
        [JsonIgnore]
        public PostProcessOutputs PostProcessOutputs { get; private set; } = new PostProcessOutputs();
        /// <summary>
        /// Deserialized log message information.
        /// </summary>
        [JsonIgnore]
        public BuildMessageInfo MessageInfo { get; private set; } = new BuildMessageInfo();
        /// <summary>
        /// Deserialized manifest items.
        /// </summary>
        [JsonIgnore]
        public List<ManifestItem> ManifestItems { get; set; } = new List<ManifestItem>();

        #endregion

        internal void Load(string baseDir)
        {
            if (PostProcessOutputsFile != null)
            {
                PostProcessOutputs = IncrementalUtility.LoadIntermediateFile<PostProcessOutputs>(Path.Combine(baseDir, PostProcessOutputsFile));
            }
            if (MessageInfoFile != null)
            {
                using (var sr = new StreamReader(Path.Combine(baseDir, MessageInfoFile), UTF8))
                {
                    MessageInfo = BuildMessageInfo.Load(sr);
                }
            }
            if (ManifestItemsFile != null)
            {
                ManifestItems = IncrementalUtility.LoadIntermediateFile<List<ManifestItem>>(Path.Combine(baseDir, ManifestItemsFile));
            }
        }

        internal void Save(string baseDir)
        {
            if (PostProcessOutputsFile == null)
            {
                PostProcessOutputsFile = IncrementalUtility.CreateRandomFileName(baseDir);
            }
            if (MessageInfoFile == null)
            {
                MessageInfoFile = IncrementalUtility.CreateRandomFileName(baseDir);
            }
            IncrementalUtility.SaveIntermediateFile(Path.Combine(baseDir, PostProcessOutputsFile), PostProcessOutputs);
            using (var sw = new StreamWriter(Path.Combine(baseDir, MessageInfoFile), false, UTF8))
            {
                MessageInfo.Save(sw);
            }
        }

        public void SaveManifest(string baseDir)
        {
            var expanded = Path.GetFullPath(Environment.ExpandEnvironmentVariables(baseDir));
            if (ManifestItemsFile == null)
            {
                ManifestItemsFile = IncrementalUtility.CreateRandomFileName(expanded);
            }
            IncrementalUtility.SaveIntermediateFile(Path.Combine(expanded, ManifestItemsFile), ManifestItems);
        }
    }
}
