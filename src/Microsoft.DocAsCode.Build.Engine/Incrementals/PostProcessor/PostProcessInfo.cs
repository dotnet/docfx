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
        /// Deserialized post process outputs
        /// </summary>
        [JsonIgnore]
        public PostProcessOutputs PostProcessOutputs { get; private set; } = new PostProcessOutputs();
        /// <summary>
        /// Deserialized log message information
        /// </summary>
        [JsonIgnore]
        public BuildMessageInfo MessageInfo { get; private set; } = new BuildMessageInfo();

        #endregion

        internal void Load(string baseDir)
        {
            if (PostProcessOutputsFile != null)
            {
                PostProcessOutputs = IncrementalUtility.LoadIntermediateFile<PostProcessOutputs>(Path.Combine(baseDir, PostProcessOutputsFile));
            }
            if (MessageInfoFile != null)
            {
                using (var sr = new StreamReader(Path.Combine(baseDir, MessageInfoFile)))
                {
                    MessageInfo = BuildMessageInfo.Load(sr);
                }
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
            using (var sw = new StreamWriter(Path.Combine(baseDir, MessageInfoFile)))
            {
                MessageInfo.Save(sw);
            }
        }
    }
}
