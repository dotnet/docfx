// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.IO;

    using Microsoft.DocAsCode.Plugins;

    public static class YamlMime
    {
        public const string YamlMimePrefix = nameof(YamlMime) + ":";
        public const string ManagedReference = YamlMimePrefix + nameof(ManagedReference);
        public const string TableOfContent = YamlMimePrefix + nameof(TableOfContent);
        public const string XRefMap = YamlMimePrefix + nameof(XRefMap);

        public static string ReadMime(TextReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            var line = reader.ReadLine();
            if (line == null || !line.StartsWith("#", StringComparison.Ordinal))
            {
                return null;
            }
            var content = line.TrimStart('#').Trim(' ');
            if (!content.StartsWith(YamlMimePrefix, StringComparison.Ordinal))
            {
                return null;
            }
            return content;
        }

        public static string ReadMime(string file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            using (var stream = EnvironmentContext.FileAbstractLayer.OpenRead(file))
            using (var reader = new StreamReader(stream))
            {
                return ReadMime(reader);
            }
        }
    }
}
