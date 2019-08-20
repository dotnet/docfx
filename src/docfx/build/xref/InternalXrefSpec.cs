// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class InternalXrefSpec : IXrefSpec
    {
        private static ThreadLocal<Stack<(string uid, Document parent)>> t_recursionDetector
            = new ThreadLocal<Stack<(string, Document)>>(() => new Stack<(string, Document)>());

        public SourceInfo Source { get; set; }

        public string Uid { get; set; }

        public string Href { get; set; }

        public Document DeclaringFile { get; set; }

        public HashSet<string> Monikers { get; set; } = new HashSet<string>();

        public Dictionary<string, Lazy<JToken>> ExtensionData { get; } = new Dictionary<string, Lazy<JToken>>();

        public Dictionary<string, JsonSchemaContentType> PropertyContentTypeMapping { get; } = new Dictionary<string, JsonSchemaContentType>();

        public string GetXrefPropertyValueAsString(string propertyName)
        {
            if (propertyName is null)
                return null;

            // for internal UID, the display property should only be plain text
            // because the display text should be plain text
            var contentType = GetXrefPropertyContentType(propertyName);
            if (contentType == JsonSchemaContentType.None)
            {
                return ExtensionData.TryGetValue(propertyName, out var property) && property.Value is JValue propertyValue && propertyValue.Value is string internalStr ? internalStr : null;
            }
            return null;
        }

        public string GetName() => GetXrefPropertyValueAsString("name");

        public ExternalXrefSpec ToExternalXrefSpec(Context context, bool forXrefMapOutput)
        {
            if (t_recursionDetector.Value.Contains((Uid, DeclaringFile)))
            {
                var referenceMap = t_recursionDetector.Value.Select(x => x.parent).ToList();
                throw Errors.CircularReference(referenceMap).ToException();
            }

            try
            {
                t_recursionDetector.Value.Push((Uid, DeclaringFile));
                var spec = new ExternalXrefSpec
                {
                    Uid = Uid,
                    Monikers = Monikers,
                };

                if (forXrefMapOutput)
                {
                    var (_, _, fragment) = UrlUtility.SplitUrl(Href);
                    var path = DeclaringFile.CanonicalUrlWithoutLocale;

                    // DHS appends branch infomation from cookie cache to URL, which is wrong for UID resolved URL
                    // output xref map with URL appending "?branch=master" for master branch
                    var query = DeclaringFile.Docset.Repository?.Branch == "master" ? "?branch=master" : "";
                    spec.Href = path + query + fragment;
                }
                else
                {
                    // relative path for internal UID resolving
                    spec.Href = PathUtility.GetRelativePathToFile(DeclaringFile.SiteUrl, Href);
                }

                foreach (var (key, value) in ExtensionData)
                {
                    try
                    {
                        spec.ExtensionData[key] = value.Value;
                    }
                    catch (DocfxException ex)
                    {
                        context.ErrorLog.Write(DeclaringFile, ex.Error);
                    }
                }
                return spec;
            }
            finally
            {
                Debug.Assert(t_recursionDetector.Value.Count > 0);
                t_recursionDetector.Value.Pop();
            }
        }

        private JsonSchemaContentType GetXrefPropertyContentType(string propertyName)
        {
            if (propertyName is null)
                return default;

            return PropertyContentTypeMapping.TryGetValue(propertyName, out var value) ? value : default;
        }
    }
}
