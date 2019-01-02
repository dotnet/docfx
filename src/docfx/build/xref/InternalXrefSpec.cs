// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class InternalXrefSpec
    {
        private static ThreadLocal<Stack<(string propertyName, string uid, Document referencedFile, Document rootFile)>> t_recursionDetector
            = new ThreadLocal<Stack<(string, string, Document, Document)>>(() => new Stack<(string, string, Document, Document)>());

        public string Uid { get; set; }

        public string Href { get; set; }

        public Document ReferencedFile { get; set; }

        // if uid defined in SDP file, we need to check if circular reference exists
        public bool NeedRecursionCheck
            => ReferencedFile.FilePath.EndsWith(".json") || ReferencedFile.FilePath.EndsWith(".yaml");

        public HashSet<string> Monikers { get; set; } = new HashSet<string>();

        public Dictionary<string, Lazy<JValue>> ExtensionData { get; } = new Dictionary<string, Lazy<JValue>>();

        public string GetXrefPropertyValue(string property, Document rootFile)
        {
            if (property is null)
                return null;

            try
            {
                if (NeedRecursionCheck)
                {
                    if (rootFile == ReferencedFile || t_recursionDetector.Value.Contains((property, Uid, ReferencedFile, rootFile)))
                    {
                        var referenceMap = t_recursionDetector.Value.Where(x => x.rootFile == rootFile).Select(x => x.referencedFile).ToList();
                        referenceMap.Insert(0, ReferencedFile);
                        throw Errors.CircularReference(rootFile, referenceMap).ToException();
                    }
                    t_recursionDetector.Value.Push((property, Uid, ReferencedFile, rootFile));
                }

                return ExtensionData.TryGetValue(property, out var internalValue) && internalValue.Value.Value is string internalStr ? internalStr : null;
            }
            finally
            {
                if (NeedRecursionCheck && t_recursionDetector.Value.Count > 0)
                {
                    t_recursionDetector.Value.Pop();
                }
            }
        }

        public string GetName(Document rootFile) => GetXrefPropertyValue("name", rootFile);

        public XrefSpec ToExternalXrefSpec(Document rootFile)
        {
            var spec = new XrefSpec
            {
                Uid = Uid,
                Monikers = Monikers,
                Href = Href,
            };
            foreach (var (key, value) in ExtensionData)
            {
                spec.ExtensionData[key] = GetXrefPropertyValue(key, rootFile);
            }
            return spec;
        }
    }
}
