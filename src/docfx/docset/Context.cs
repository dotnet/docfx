// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class Context
    {
        private readonly string _outputPath;
        private readonly Report _report;
        private readonly Cache _cache;

        public Context(Report report, string outputPath)
        {
            _report = report;
            _outputPath = Path.GetFullPath(outputPath);
            _cache = new Cache();
        }

        public (List<Error> errors, JToken token) LoadYamlFile(Document file) => _cache.LoadYamlFile(file);

        public (List<Error> errors, JToken token) LoadJsonFile(Document file) => _cache.LoadJsonFile(file);

        public (List<Error> errors, JObject metadata) ExtractMetadata(Document file) => _cache.ExtractMetadata(file);

        public bool Report(string file, IEnumerable<Error> errors)
        {
            var hasErrors = false;
            foreach (var error in errors)
            {
                if (Report(file, error))
                {
                    hasErrors = true;
                }
            }
            return hasErrors;
        }

        public bool Report(string file, Error error)
        {
            return Report(file == error.File || !string.IsNullOrEmpty(error.File)
                    ? error
                    : new Error(error.Level, error.Code, error.Message, file, error.Range, error.JsonPath));
        }

        public bool Report(Error error)
        {
            return _report.Write(error);
        }

        /// <summary>
        /// Opens a write stream to write to an output file.
        /// Throws if multiple threads trying to write to the same destination concurrently.
        /// </summary>
        public Stream WriteStream(string destRelativePath)
        {
            Debug.Assert(!Path.IsPathRooted(destRelativePath));

            var destinationPath = Path.Combine(_outputPath, destRelativePath);

            PathUtility.CreateDirectoryFromFilePath(destinationPath);

            return File.Create(destinationPath);
        }

        /// <summary>
        /// Writes the input object as json to an output file.
        /// Throws if multiple threads trying to write to the same destination concurrently.
        /// </summary>
        public void WriteJson(object graph, string destRelativePath)
        {
            using (var writer = new StreamWriter(WriteStream(destRelativePath)))
            {
                JsonUtility.Serialize(writer, graph);
            }
        }

        /// <summary>
        /// Writes the input text to an output file.
        /// Throws if multiple threads trying to write to the same destination concurrently.
        /// </summary>
        public void WriteText(string contents, string destRelativePath)
        {
            Debug.Assert(!Path.IsPathRooted(destRelativePath));

            var destinationPath = Path.Combine(_outputPath, destRelativePath);

            PathUtility.CreateDirectoryFromFilePath(destinationPath);

            File.WriteAllText(destinationPath, contents);
        }

        /// <summary>
        /// Copies a file from source to destination, throws if source does not exists.
        /// Throws if multiple threads trying to write to the same destination concurrently.
        /// </summary>
        public void Copy(Document file, string destRelativePath)
        {
            Debug.Assert(!Path.IsPathRooted(destRelativePath));

            var sourcePath = Path.Combine(file.Docset.DocsetPath, file.FilePath);
            var destinationPath = Path.Combine(_outputPath, destRelativePath);

            PathUtility.CreateDirectoryFromFilePath(destinationPath);

            File.Copy(sourcePath, destinationPath, overwrite: true);
        }

        public void Delete(string destRelativePath)
        {
            Debug.Assert(!Path.IsPathRooted(destRelativePath));

            var destinationPath = Path.Combine(_outputPath, destRelativePath);

            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }
        }

        private sealed class Cache
        {
            private readonly ConcurrentDictionary<string, Lazy<(List<Error>, JToken)>> _tokenCache = new ConcurrentDictionary<string, Lazy<(List<Error>, JToken)>>();
            private readonly ConcurrentDictionary<string, Lazy<(List<Error>, JObject)>> _metadataCache = new ConcurrentDictionary<string, Lazy<(List<Error>, JObject)>>();

            public (List<Error> errors, JToken token) LoadYamlFile(Document file)
                => _tokenCache.GetOrAdd(GetKeyFromFile(file), new Lazy<(List<Error>, JToken)>(() =>
                {
                    var content = file.ReadText();
                    GitUtility.CheckMergeConflictMarker(content, file.FilePath);
                    return YamlUtility.Deserialize(content);
                })).Value;

            public (List<Error> errors, JToken token) LoadJsonFile(Document file)
                => _tokenCache.GetOrAdd(GetKeyFromFile(file), new Lazy<(List<Error>, JToken)>(() =>
                {
                    var content = file.ReadText();
                    GitUtility.CheckMergeConflictMarker(content, file.FilePath);
                    return JsonUtility.Deserialize(content);
                })).Value;

            public (List<Error> errors, JObject metadata) ExtractMetadata(Document file)
                => _metadataCache.GetOrAdd(GetKeyFromFile(file), new Lazy<(List<Error>, JObject)>(() =>
                {
                    using (var reader = new StreamReader(file.ReadStream()))
                    {
                        return ExtractYamlHeader.Extract(reader);
                    }
                })).Value;

            private string GetKeyFromFile(Document file)
            {
                var filePath = Path.Combine(file.Docset.DocsetPath, file.FilePath);
                return filePath + new FileInfo(filePath).LastWriteTime;
            }
        }
    }
}
