// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json;

    public class BuildVersionInfo
    {
        /// <summary>
        /// The version name of documents.
        /// </summary>
        public string VersionName { get; set; }

        /// <summary>
        /// The information for processors.
        /// </summary>
        public List<ProcessorInfo> Processors { get; } = new List<ProcessorInfo>();

        /// <summary>
        /// The hash info for configs.
        /// Include global metadata, exclude file metadata.
        /// </summary>
        public string ConfigHash { get; set; }

        /// <summary>
        /// The hash info for the whole FileMetadata section in configs
        /// </summary>
        public string FileMetadataHash { get; set; }

        /// <summary>
        /// The file link for dependency (type is <see cref="DependencyGraph.Load(System.IO.TextReader)"/>).
        /// </summary>
        public string DependencyFile { get; set; }

        /// <summary>
        /// The file link for <see cref="FileMetadata"/>
        /// </summary>
        public string FileMetadataFile { get; set; }

        /// <summary>
        /// The file link for file attributes.(type is <see cref="FileAttributes"/>).
        /// e.g. last modified time, md5.
        /// </summary>
        public string AttributesFile { get; set; }

        /// <summary>
        /// The file link for build outputs (type is <see cref="BuildOutputs"/>).
        /// </summary>
        public string OutputFile { get; set; }

        /// <summary>
        /// The file link for the manifest file(type is <see cref="T:Microsoft.DocAsCode.Plugins.Manifest"/>).
        /// </summary>
        public string ManifestFile { get; set; }

        /// <summary>
        /// The file link for the XRefMap file(type is <see cref="XRefMap"/>).
        /// </summary>
        public string XRefSpecMapFile { get; set; }

        /// <summary>
        /// The file link for the ExternalXRefSpec file.
        /// </summary>
        public string ExternalXRefSpecFile { get; set; }

        /// <summary>
        /// The file link for the FileMap file.
        /// </summary>
        public string FileMapFile { get; set; }

        /// <summary>
        /// The file link for the build message file (type is <see cref="BuildMessage"/>).
        /// </summary>
        public string BuildMessageFile { get; set; }

        /// <summary>
        /// The file link for the TocRestructions file.
        /// </summary>
        public string TocRestructionsFile { get; set; }

        #region Deserialized content
        [JsonIgnore]
        public string BaseDir { get; internal set; }

        /// <summary>
        /// deserialized dependency graph
        /// </summary>
        [JsonIgnore]
        public DependencyGraph Dependency { get; set; }

        /// <summary>
        /// deserialized file metadata
        /// </summary>
        [JsonIgnore]
        public FileMetadata FileMetadata { get; set; }

        /// <summary>
        /// deserialized attributes
        /// </summary>
        [JsonIgnore]
        public IDictionary<string, FileAttributeItem> Attributes { get; set; } = new OSPlatformSensitiveDictionary<FileAttributeItem>();

        /// <summary>
        /// deserialized manifestitems
        /// </summary>
        [JsonIgnore]
        public IEnumerable<ManifestItem> Manifest { get; set; }

        /// <summary>
        /// deserialized outputs
        /// </summary>
        [JsonIgnore]
        public BuildOutputs BuildOutputs { get; private set; } = new BuildOutputs();

        /// <summary>
        /// deserialized xrefspecmap. Key is original file path from root. Value is XrefSpecs reported by the file.
        /// </summary>
        [JsonIgnore]
        public IDictionary<string, List<XRefSpec>> XRefSpecMap { get; private set; } = new OSPlatformSensitiveDictionary<List<XRefSpec>>();

        /// <summary>
        /// deserialized filemap.
        /// </summary>
        [JsonIgnore]
        public IDictionary<string, FileMapItem> FileMap { get; private set; } = new OSPlatformSensitiveDictionary<FileMapItem>();

        /// <summary>
        /// deserialized build messages.
        /// </summary>
        [JsonIgnore]
        public BuildMessage BuildMessage { get; private set; } = new BuildMessage();

        /// <summary>
        /// deserialized Toc Restructions.
        /// </summary>
        [JsonIgnore]
        public IDictionary<string, List<TreeItemRestructure>> TocRestructions { get; private set; } = new OSPlatformSensitiveDictionary<List<TreeItemRestructure>>();
        #endregion

        public void SaveManifest()
        {
            IncrementalUtility.SaveIntermediateFile(Path.Combine(BaseDir, ManifestFile), Manifest);
        }

        internal void Load(string baseDir)
        {
            ActionWhenNotNull(baseDir, DependencyFile, f => { Dependency = IncrementalUtility.LoadDependency(f); });
            ActionWhenNotNull(
                baseDir,
                FileMetadataFile,
                f =>
                {
                    FileMetadata = IncrementalUtility.LoadIntermediateFile<FileMetadata>(f, IncrementalUtility.FileMetadataJsonSerializationSettings);
                });
            ActionWhenNotNull(baseDir, AttributesFile, f => { Attributes = IncrementalUtility.LoadIntermediateFile<OSPlatformSensitiveDictionary<FileAttributeItem>>(f); });
            ActionWhenNotNull(baseDir, OutputFile, f => { BuildOutputs = IncrementalUtility.LoadIntermediateFile<BuildOutputs>(f); });
            ActionWhenNotNull(baseDir, ManifestFile, f => { Manifest = IncrementalUtility.LoadIntermediateFile<IEnumerable<ManifestItem>>(f); });
            ActionWhenNotNull(baseDir, XRefSpecMapFile, f => { XRefSpecMap = IncrementalUtility.LoadIntermediateFile<OSPlatformSensitiveDictionary<List<XRefSpec>>>(f); });
            ActionWhenNotNull(baseDir, FileMapFile, f => { FileMap = IncrementalUtility.LoadIntermediateFile<OSPlatformSensitiveDictionary<FileMapItem>>(f); });
            ActionWhenNotNull(baseDir, BuildMessageFile, f => { BuildMessage = IncrementalUtility.LoadBuildMessage(f); });
            ActionWhenNotNull(baseDir, TocRestructionsFile, f => { TocRestructions = IncrementalUtility.LoadIntermediateFile<OSPlatformSensitiveDictionary<List<TreeItemRestructure>>>(f); });
            foreach (var processor in Processors)
            {
                processor.Load(baseDir);
            }
        }

        internal void Save(string baseDir)
        {
            IncrementalUtility.SaveDependency(Path.Combine(baseDir, DependencyFile), Dependency);
            if (FileMetadataFile != null)
            {
                IncrementalUtility.SaveIntermediateFile(
                    Path.Combine(baseDir, FileMetadataFile),
                    FileMetadata,
                    IncrementalUtility.FileMetadataJsonSerializationSettings);
            }
            IncrementalUtility.SaveIntermediateFile(Path.Combine(baseDir, AttributesFile), Attributes);
            IncrementalUtility.SaveIntermediateFile(Path.Combine(baseDir, OutputFile), BuildOutputs);
            IncrementalUtility.SaveIntermediateFile(Path.Combine(baseDir, XRefSpecMapFile), XRefSpecMap);
            IncrementalUtility.SaveIntermediateFile(Path.Combine(baseDir, FileMapFile), FileMap);
            IncrementalUtility.SaveBuildMessage(Path.Combine(baseDir, BuildMessageFile), BuildMessage);
            IncrementalUtility.SaveIntermediateFile(Path.Combine(baseDir, TocRestructionsFile), TocRestructions);
            foreach (var processor in Processors)
            {
                processor.Save(baseDir);
            }
        }

        private void ActionWhenNotNull(string baseDir, string file, Action<string> action)
        {
            if (file != null)
            {
                action(Path.Combine(baseDir, file));
            }
        }
    }
}
