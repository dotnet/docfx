// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.IO;

    public static class YamlMime
    {
        public const string ManagedReference = nameof(YamlMime) + ":" + nameof(ManagedReference);
        public const string TableOfContent = nameof(YamlMime) + ":" + nameof(TableOfContent);
        public const string XRefMap = nameof(YamlMime) + ":" + nameof(XRefMap);

        public static string ReadMime(TextReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            var line = reader.ReadLine();
            if (!line.StartsWith("#"))
            {
                return null;
            }
            return line.TrimStart('#').TrimStart(' ');
        }

#if !NetCore
        public static string ReadMime(string file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            using (var reader = File.OpenText(file))
            {
                return ReadMime(reader);
            }
        }
#endif
    }
}
