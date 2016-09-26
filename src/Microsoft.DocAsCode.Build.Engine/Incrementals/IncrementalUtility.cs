// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;
    using System.IO;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    public static class IncrementalUtility
    {
        public static T LoadIntermediateFile<T>(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return default(T);
            }
            using (var reader = new StreamReader(fileName))
            {
                return JsonUtility.Deserialize<T>(reader);
            }
        }

        public static DependencyGraph LoadDependency(string dependencyFile)
        {
            if (string.IsNullOrEmpty(dependencyFile))
            {
                return null;
            }
            using (var reader = new StreamReader(dependencyFile))
            {
                return DependencyGraph.Load(reader);
            }
        }

        public static void SaveDependency(string fileName, DependencyGraph dg)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException("fileName");
            }
            using (var writer = new StreamWriter(fileName))
            {
                dg.Save(writer);
            }
        }

        public static void SaveIntermediateFile<T>(string fileName, T content)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException("fileName");
            }
            using (var writer = new StreamWriter(fileName))
            {
                JsonUtility.Serialize(writer, content);
            }
        }

        public static string GetDependencyKey(FileAndType file)
        {
            if (file == null)
            {
                return null;
            }
            return ((RelativePath)file.File).GetPathFromWorkingFolder().ToString();
        }

        public static string GetRandomEntry(string baseDir)
        {
            string name;
            do
            {
                name = Path.GetRandomFileName();
            } while (Directory.Exists(Path.Combine(baseDir, name)) || File.Exists(Path.Combine(baseDir, name)));
            return name;
        }

        public static string CreateRandomDir(string baseDir)
        {
            string folderName = GetRandomEntry(baseDir);
            Directory.CreateDirectory(Path.Combine(baseDir, folderName));
            return folderName;
        }

    }
}
