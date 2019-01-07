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
    internal class InternalXrefSpec
    {
        private static ThreadLocal<Stack<(string propertyName, Document referencedFile)>> t_recursionDetector
            = new ThreadLocal<Stack<(string, Document)>>(() => new Stack<(string, Document)>());

        public string Uid { get; set; }

        public string Href { get; set; }

        public Document ReferencedFile { get; set; }

        public HashSet<string> Monikers { get; set; } = new HashSet<string>();

        public Dictionary<string, Lazy<JValue>> ExtensionData { get; } = new Dictionary<string, Lazy<JValue>>();

        public string GetXrefPropertyValue(string propertyName)
        {
            if (propertyName is null)
                return null;

            // TODO: fix reference map while markdig engine split xref reference from inclusion
            if (t_recursionDetector.Value.Contains((propertyName, ReferencedFile)))
            {
                var referenceMap = t_recursionDetector.Value.Select(x => x.referencedFile).ToList();
                referenceMap.Reverse();
                referenceMap.Add(ReferencedFile);
                throw Errors.CircularReference(referenceMap).ToException();
            }

            try
            {
                t_recursionDetector.Value.Push((propertyName, ReferencedFile));
                return ExtensionData.TryGetValue(propertyName, out var internalValue) && internalValue.Value.Value is string internalStr ? internalStr : null;
            }
            finally
            {
                Debug.Assert(t_recursionDetector.Value.Count > 0);
                t_recursionDetector.Value.Pop();
            }
        }

        public string GetName() => GetXrefPropertyValue("name");

        public XrefSpec ToExternalXrefSpec()
        {
            var spec = new XrefSpec
            {
                Uid = Uid,
                Monikers = Monikers,
                Href = Href,
            };
            foreach (var (key, value) in ExtensionData)
            {
                // TODO: remove exception sallow while markdig engine split xref reference from inclusion
                try
                {
                    spec.ExtensionData[key] = GetXrefPropertyValue(key);
                }
                catch (DocfxException)
                {
                }
            }
            return spec;
        }
    }
}
