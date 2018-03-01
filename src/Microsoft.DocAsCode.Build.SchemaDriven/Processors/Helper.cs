// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    internal static class Helper
    {
        private const string ContentOriginalFileKeyName = "ContentOriginalFile";

        public static void AddFileLinkSource(this Dictionary<string, List<LinkSourceInfo>> fileLinkSources, LinkSourceInfo source)
        {
            var file = source.Target;
            if (!fileLinkSources.TryGetValue(file, out List<LinkSourceInfo> sources))
            {
                sources = new List<LinkSourceInfo>();
                fileLinkSources[file] = sources;
            }
            sources.Add(source);
        }

        public static void SetOriginalContentFile(this IProcessContext context, string path, FileAndType file)
        {
            if (!context.PathProperties.TryGetValue(path, out var properties))
            {
                properties = context.PathProperties[path] = new Dictionary<string, object>();
            }

            properties[ContentOriginalFileKeyName] = file;
        }

        public static FileAndType GetOriginalContentFile(this IProcessContext context, string path)
        {
            FileAndType filePath = null;
            if (context.PathProperties.TryGetValue(path, out var properties) && properties.TryGetValue(ContentOriginalFileKeyName, out var file))
            {
                filePath = file as FileAndType;
                if (filePath == null)
                {
                    Logger.LogWarning($"{ContentOriginalFileKeyName} is expecting to be with type FileAndType, however its value is {file.GetType()}");
                }
            }

            return filePath ?? context.OriginalFileAndType;
        } 
    }
}
