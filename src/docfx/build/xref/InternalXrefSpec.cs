// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class InternalXrefSpec : IXrefSpec
    {
        public string Uid { get; set; }

        public string Href { get; set; }

        public Document DeclairingFile { get; set; }

        public HashSet<string> Monikers { get; set; } = new HashSet<string>();

        public Dictionary<string, Lazy<JToken>> ExtensionData { get; } = new Dictionary<string, Lazy<JToken>>();

        public string GetXrefPropertyValue(string propertyName)
        {
            if (propertyName is null)
                return null;

            return ExtensionData.TryGetValue(propertyName, out var property) && property.Value is JValue propertyValue && propertyValue.Value is string internalStr ? internalStr : null;
        }

        public string GetName() => GetXrefPropertyValue("name");

        public ExternalXrefSpec ToExternalXrefSpec(Context context, Document file, bool forOutput = true)
        {
            var spec = new ExternalXrefSpec
            {
                Uid = Uid,
                Monikers = Monikers,
                Href = Href,
            };

            // DHS appends branch infomation from cookie cache to URL, which is wrong for UID resolved URL
            // output xref map with URL appending "?branch=master" for master branch
            if (forOutput)
            {
                var (_, query, fragment) = UrlUtility.SplitUrl(Href);
                var path = DeclairingFile.Docset.Repository.Branch == "master"
                                    ? DeclairingFile.CanonicalUrlWithoutLocale + "?branch=master"
                                    : DeclairingFile.CanonicalUrlWithoutLocale;
                spec.Href = UrlUtility.MergeUrl(path, query?.Length > 0 ? query.Substring(1) : query, fragment?.Length > 0 ? fragment.Substring(1) : fragment);
            }
            else
            {
                // relative path for internal UID resolving
                spec.Href = PathUtility.GetRelativePathToFile(file.SiteUrl, Href);
            }

            foreach (var (key, value) in ExtensionData)
            {
                try
                {
                    spec.ExtensionData[key] = value.Value;
                }
                catch (DocfxException ex)
                {
                    context.ErrorLog.Write(file.FilePath, ex.Error);
                }
            }
            return spec;
        }
    }
}
