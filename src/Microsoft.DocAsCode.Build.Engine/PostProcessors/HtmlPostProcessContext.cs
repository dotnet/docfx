// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Engine.Incrementals;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json;

    public class HtmlPostProcessContext
    {
        [JsonIgnore]
        public IPostProcessorHost PostProcessorHost { get; private set; }

        // bookmarks mapping from output file -> bookmarks
        public OSPlatformSensitiveDictionary<HashSet<string>> Bookmarks { get; private set; } = new OSPlatformSensitiveDictionary<HashSet<string>>();

        // file mapping from output file -> src file
        public OSPlatformSensitiveDictionary<string> FileMapping { get; private set; } = new OSPlatformSensitiveDictionary<string>();

        public static HtmlPostProcessContext Load(IPostProcessorHost host)
        {
            var stream = host?.LoadContextInfo();
            using (stream)
            {
                if (stream == null || host?.IsIncremental == false)
                {
                    var context = new HtmlPostProcessContext();
                    context.PostProcessorHost = host;
                    return context;
                }
                using (var sr = new StreamReader(stream))
                {
                    var context = JsonUtility.Deserialize<HtmlPostProcessContext>(sr);
                    context.PostProcessorHost = host;
                    var totalSrcFileSet = new HashSet<string>(host.SourceFileInfos.Select(s => s.SourceRelativePath));
                    context.FileMapping = new OSPlatformSensitiveDictionary<string>(
                        context.FileMapping
                        .Where(p => totalSrcFileSet.Contains(p.Value))
                        .ToDictionary(p => p.Key, p => p.Value));
                    context.Bookmarks = new OSPlatformSensitiveDictionary<HashSet<string>>(
                        context.Bookmarks
                        .Where(p => context.FileMapping.ContainsKey(p.Key))
                        .ToDictionary(p => p.Key, p => p.Value));
                    return context;
                }
            }
        }

        public void Save()
        {
            var stream = PostProcessorHost?.SaveContextInfo();
            using (stream)
            {
                if (stream == null || PostProcessorHost?.ShouldTraceIncrementalInfo == false)
                {
                    return;
                }
                using (var sw = new StreamWriter(stream))
                {
                    JsonUtility.Serialize(sw, this);
                }
            }
        }
    }
}
