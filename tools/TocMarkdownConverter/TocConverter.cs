// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TocMarkdownConverter
{
    using System;
    using System.IO;

    using Microsoft.DocAsCode.Build.TableOfContents;
    using Microsoft.DocAsCode.Common;

    internal static class TocConverter
    {
        private static readonly string YmlExtension = ".yml";

        public static string Convert(string tocMarkdownFilePath, string tocYmlPath = null)
        {
            if (string.IsNullOrEmpty(tocMarkdownFilePath))
            {
                throw new ArgumentException($"{nameof(tocMarkdownFilePath)} can't be null or empty");
            }

            if (!File.Exists(tocMarkdownFilePath))
            {
                throw new FileNotFoundException($"{tocMarkdownFilePath} can't be found.");
            }

            if (string.IsNullOrEmpty(tocYmlPath))
            {
                var tocName = Path.GetFileNameWithoutExtension(tocMarkdownFilePath);
                var tocDir = Path.GetDirectoryName(tocMarkdownFilePath);

                tocYmlPath = Path.Combine(tocDir, tocName + YmlExtension);
            }

            ConvertCore(tocMarkdownFilePath, tocYmlPath);

            return tocYmlPath;
        }

        private static void ConvertCore(string tocMarkdownFilePath, string tocYmlPath)
        {
            using (var sr = new StreamReader(tocMarkdownFilePath))
            {
                var tocModel = MarkdownTocReader.LoadToc(sr.ReadToEnd(), tocMarkdownFilePath);
                YamlUtility.Serialize(tocYmlPath, tocModel);
            }
        }
    }
}
