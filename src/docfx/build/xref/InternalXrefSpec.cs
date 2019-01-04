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

        public HashSet<string> Monikers { get; set; } = new HashSet<string>();

        public Dictionary<string, Lazy<JValue>> ExtensionData { get; } = new Dictionary<string, Lazy<JValue>>();

        public string GetXrefPropertyValue(string propertyName, Document rootFile)
        {
            if (propertyName is null)
                return null;

            try
            {
                if (t_recursionDetector.Value.Contains((propertyName, Uid, ReferencedFile, rootFile)))
                {
                    var referenceMap = t_recursionDetector.Value.Where(x => x.rootFile == rootFile).Select(x => x.referencedFile).ToList();
                    if (!referenceMap.Contains(rootFile))
                    {
                        referenceMap.Reverse();
                        referenceMap.Add(ReferencedFile);
                        referenceMap.Insert(0, rootFile);
                        throw Errors.CircularReference(rootFile, referenceMap).ToException();
                    }
                    else
                    {
                        referenceMap.Add(rootFile);
                        throw Errors.CircularReference(rootFile, referenceMap).ToException();
                    }
                }
                t_recursionDetector.Value.Push((propertyName, Uid, ReferencedFile, rootFile));

                return ExtensionData.TryGetValue(propertyName, out var internalValue) && internalValue.Value.Value is string internalStr ? internalStr : null;
            }
            finally
            {
                if (t_recursionDetector.Value.Count > 0)
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
