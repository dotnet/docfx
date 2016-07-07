// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System.IO;

    public static class YamlMime
    {
        public const string ManagedReference = nameof(ManagedReference);
        public const string TableOfContent = nameof(TableOfContent);
        public const string XRefMap = nameof(XRefMap);

        public static string ReadMime(TextReader reader)
        {
            var line = reader.ReadLine();
            if (!line.StartsWith("#"))
            {
                return null;
            }
            return line.TrimStart('#', ' ');
        }

#if !NetCore
        public static string ReadMime(string file)
        {
            using (var reader = File.OpenText(file))
            {
                return ReadMime(reader);
            }
        }
#endif
    }
}
