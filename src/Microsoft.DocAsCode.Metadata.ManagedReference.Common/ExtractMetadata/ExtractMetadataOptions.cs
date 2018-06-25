// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Collections.Generic;

    using Microsoft.CodeAnalysis;

    using Newtonsoft.Json;

    public class ExtractMetadataOptions
    {
        public bool ShouldSkipMarkup { get; set; }

        public bool PreserveRawInlineComments { get; set; }

        public string FilterConfigFile { get; set; }

        public Dictionary<string, string> MSBuildProperties { get; set; }

        public string CodeSourceBasePath { get; set; }

        public bool DisableDefaultFilter { get; set; }

        [JsonIgnore]
        public IReadOnlyDictionary<Compilation, IEnumerable<IMethodSymbol>> RoslynExtensionMethods { get; set; }

        public bool HasChanged(IncrementalCheck check, bool careMSBuildProperties)
        {
            return check.BuildInfo.Options == null ||
                check.BuildInfo.Options.ShouldSkipMarkup != ShouldSkipMarkup ||
                check.BuildInfo.Options.PreserveRawInlineComments != PreserveRawInlineComments ||
                check.BuildInfo.Options.FilterConfigFile != FilterConfigFile ||
                check.IsFileModified(FilterConfigFile) ||
                (careMSBuildProperties && check.MSBuildPropertiesUpdated(MSBuildProperties)) ||
                check.BuildInfo.Options.CodeSourceBasePath != CodeSourceBasePath
                ;
        }
    }
}
