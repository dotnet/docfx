// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;

namespace Microsoft.Docs.Build
{
    internal static class LegacyXrefMap
    {
        public static void Convert(Docset docset, Context context, XrefMap xrefMap)
        {
            using (var writer = new StreamWriter(context.WriteStream(Path.Combine(docset.Config.DocumentId.SiteBasePath, "xrefmap.yml"))))
            {
                writer.WriteLine("### YamlMime:XRefMap");
                var serializer = new SerializerBuilder().Build();
                serializer.Serialize(writer, new { sorted = true, references = xrefMap.InternalReferences.Select(x => ExpandJObject(x)) });
            }
        }

        // YamlDotNet serializer does not like Jobject, it needs to be expanded
        // https://github.com/aaubry/YamlDotNet/issues/254
        private static dynamic ExpandJObject(XrefSpec spec)
        {
            var extensionData = spec.ExtensionData.ToObject<Dictionary<string, object>>();
            dynamic expanedoObj = new ExpandoObject();
            var collection = (ICollection<KeyValuePair<string, object>>)expanedoObj;
            foreach (var kv in extensionData)
            {
                collection.Add(kv);
            }
            expanedoObj.Uid = spec.Uid;
            expanedoObj.Href = spec.Href;
            return expanedoObj;
        }
    }
}
