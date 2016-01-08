// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Builders
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public sealed class DocumentBuildParameters : MarshalByRefObject
    {
        public FileCollection Files { get; set; }
        public string OutputBaseDir { get; set; }
        public ImmutableArray<string> ExternalReferencePackages { get; set; } = ImmutableArray<string>.Empty;
        public ImmutableDictionary<string, object> Metadata { get; set; } = ImmutableDictionary<string, object>.Empty;
        public FileMetadata FileMetadata { get; set; }
        public TemplateCollection TemplateCollection { get; set; }
        public bool ExportRawModel { get; set; }
        public bool ExportViewModel { get; set; }
    }

    public sealed class FileMetadata : Dictionary<string, ImmutableArray<FileMetadataItem>>
    {
        public string BaseDir { get; }
        public FileMetadata(string baseDir) : base()
        {
            BaseDir = baseDir;
        }
        public FileMetadata(string baseDir, IDictionary<string, ImmutableArray<FileMetadataItem>> dictionary) : base(dictionary)
        {
            BaseDir = baseDir;
        }
    }

    public sealed class FileMetadataItem
    {
        public Glob.GlobMatcher Glob { get; set; }
        public object Value { get; set; }
        public string Key { get; set; }
        public FileMetadataItem(Glob.GlobMatcher glob, string key, object value)
        {
            Glob = glob;
            Key = key;
            Value = value;
        }
    }
}
