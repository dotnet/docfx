// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public static class FileAbstractLayerExtensions
    {
        public static bool Exists(this FileAbstractLayer fal, string file) =>
            fal.Exists((RelativePath)file);

        public static StreamReader OpenReadText(this FileAbstractLayer fal, RelativePath file) =>
            new StreamReader(fal.OpenRead(file));

        public static StreamReader OpenReadText(this FileAbstractLayer fal, string file) =>
            OpenReadText(fal, (RelativePath)file);

        public static string ReadAllText(this FileAbstractLayer fal, RelativePath file)
        {
            using (var sr = OpenReadText(fal, file))
            {
                return sr.ReadToEnd();
            }
        }

        public static string ReadAllText(this FileAbstractLayer fal, string file) =>
            ReadAllText(fal, (RelativePath)file);

        public static StreamWriter CreateText(this FileAbstractLayer fal, RelativePath file) =>
            new StreamWriter(fal.Create(file));

        public static StreamWriter CreateText(this FileAbstractLayer fal, string file) =>
            CreateText(fal, (RelativePath)file);

        public static void WriteAllText(this FileAbstractLayer fal, RelativePath file, string content)
        {
            using (var writer = CreateText(fal, file))
            {
                writer.Write(content);
            }
        }

        public static void WriteAllText(this FileAbstractLayer fal, string file, string content) =>
            WriteAllText(fal, (RelativePath)file, content);

        public static bool HasProperty(this FileAbstractLayer fal, RelativePath file, string propertyName)
        {
            var dict = fal.GetProperties(file);
            return dict.ContainsKey(propertyName);
        }

        public static bool HasProperty(this FileAbstractLayer fal, string file, string propertyName) =>
            HasProperty(fal, (RelativePath)file, propertyName);

        public static string GetProperty(this FileAbstractLayer fal, RelativePath file, string propertyName)
        {
            var dict = fal.GetProperties(file);
            dict.TryGetValue(propertyName, out string result);
            return result;
        }

        public static string GetProperty(this FileAbstractLayer fal, string file, string propertyName) =>
            GetProperty(fal, (RelativePath)file, propertyName);

        public static IEnumerable<KeyValuePair<RelativePath, string>> GetAllPhysicalPaths(this FileAbstractLayer fal) =>
            from r in fal.GetAllInputFiles()
            select new KeyValuePair<RelativePath, string>(r, fal.GetPhysicalPath(r));

        public static string GetOutputPhysicalPath(this FileAbstractLayer fal, string file) =>
            GetOutputPhysicalPath(fal, (RelativePath)file);

        public static string GetOutputPhysicalPath(this FileAbstractLayer fal, RelativePath file) =>
            FileAbstractLayerBuilder.Default
                .ReadFromOutput(fal)
                .Create()
                .GetPhysicalPath(file);
    }
}
