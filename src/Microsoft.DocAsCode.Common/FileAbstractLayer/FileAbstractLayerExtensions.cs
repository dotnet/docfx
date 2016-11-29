// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System.IO;

    public static class FileAbstractLayerExtensions
    {
        public static bool Exists(this IFileAbstractLayer fal, string file) =>
            fal.Exists((RelativePath)file);

        public static FileStream OpenRead(this IFileAbstractLayer fal, string file) =>
            fal.OpenRead((RelativePath)file);

        public static StreamReader OpenReadText(this IFileAbstractLayer fal, RelativePath file) =>
            new StreamReader(fal.OpenRead(file));

        public static StreamReader OpenReadText(this IFileAbstractLayer fal, string file) =>
            OpenReadText(fal, (RelativePath)file);

        public static string ReadAllText(this IFileAbstractLayer fal, RelativePath file)
        {
            using (var sr = OpenReadText(fal, file))
            {
                return sr.ReadToEnd();
            }
        }

        public static string ReadAllText(this IFileAbstractLayer fal, string file) =>
            ReadAllText(fal, (RelativePath)file);

        public static FileStream Create(this IFileAbstractLayer fal, string file) =>
            fal.Create((RelativePath)file);

        public static StreamWriter CreateText(this IFileAbstractLayer fal, RelativePath file) =>
            new StreamWriter(fal.Create(file));

        public static StreamWriter CreateText(this IFileAbstractLayer fal, string file) =>
            CreateText(fal, (RelativePath)file);

        public static void WriteAllText(this IFileAbstractLayer fal, RelativePath file, string content)
        {
            using (var writer = CreateText(fal, file))
            {
                writer.Write(content);
            }
        }

        public static void WriteAllText(this IFileAbstractLayer fal, string file, string content) =>
            WriteAllText(fal, (RelativePath)file, content);

        public static void Copy(this IFileAbstractLayer fal, string sourceFileName, string destFileName) =>
            fal.Copy((RelativePath)sourceFileName, (RelativePath)destFileName);
    }
}
