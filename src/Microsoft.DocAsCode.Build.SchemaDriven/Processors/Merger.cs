// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;

    internal sealed class Merger
    {
        public OverwriteModelType OverwriteType { get; set; }

        public void Merge(ref object src, object overwrite, string uid, string path, BaseSchema schema)
        {
            if (schema == null)
            {
                MergeCore(ref src, overwrite, uid, path, schema);
                return;
            }
            switch (schema.MergeType)
            {
                case MergeType.Merge:
                    MergeCore(ref src, overwrite, uid, path, schema);
                    break;
                case MergeType.Replace:
                    src = overwrite;
                    break;
                case MergeType.Key:
                case MergeType.Ignore:
                default:
                    break;
            }
        }

        private void MergeCore(ref object src, object overwrite, string uid, string path, BaseSchema schema)
        {
            if (overwrite == null)
            {
                src = null;
                return;
            }

            if (src == null)
            {
                src = overwrite;
                return;
            }

            if (src is IDictionary<string, object> sdict)
            {
                if (overwrite is IDictionary<string, object> odict)
                {
                    foreach (var pair in odict)
                    {
                        if (sdict.TryGetValue(pair.Key, out var so))
                        {
                            object refSo = so;
                            if (schema?.Properties != null && schema.Properties.TryGetValue(pair.Key, out var innerSchema))
                            {
                                Merge(ref refSo, pair.Value, uid, $"{path}/{pair.Key}", innerSchema);
                            }
                            else
                            {
                                // Use the default behavior (Merge) when schema is not defined
                                Merge(ref refSo, pair.Value, uid, $"{path}/{pair.Key}", null);
                            }
                            if (!ReferenceEquals(refSo, so))
                            {
                                sdict[pair.Key] = refSo;
                            }
                        }
                        else
                        {
                            sdict[pair.Key] = pair.Value;
                        }
                    }
                }
                else
                {
                    ThrowError($"Expected dictionary, however overwrite object for {uid}'s path \"{path}\" is {overwrite.GetType()}");
                }
            }
            else if (src is IList<object> sarray)
            {
                if (overwrite is IList<object> oarray)
                {
                    // If match, modify
                    // If not match, do nothing
                    for (int j = 0; j < oarray.Count; j++)
                    {
                        var item = oarray[j];
                        bool matched = false;
                        for (int i = 0; i < sarray.Count; i++)
                        {
                            if (TestKey(sarray[i], item, schema?.Items))
                            {
                                matched = true;
                                var si = sarray[i];
                                Merge(ref si, item, uid, $"{path}/{i}", schema?.Items);
                                if (!ReferenceEquals(si, sarray[i]))
                                {
                                    Logger.LogDiagnostic($"Merging \"{path}/{i}\" for {uid}");
                                    sarray[i] = si;
                                }
                            }
                        }
                        if (!matched)
                        {
                            Logger.LogWarning($"\"{path}/{j}\" in overwrite object fails to overwrite \"{path}\" for \"{uid}\" because it does not match any existing item.",
                                code: OverwriteType == OverwriteModelType.MarkdownFragments ? WarningCodes.Overwrite.InvalidMarkdownFragments : null);
                        }
                    }
                }
                else
                {
                    ThrowError($"Expected list, however overwrite object for {uid}'s path \"{path}\" is {overwrite.GetType()}");
                }
            }
            else
            {
                // For primitive type, replace
                src = overwrite;
            }
        }

        private void ThrowError(string message)
        {
            Logger.LogError(message);
            throw new InvalidOverwriteDocumentException(message);
        }

        private bool TestKey(object source, object overrides, BaseSchema schema)
        {
            if (overrides == null || overrides == null)
            {
                return false;
            }
            return schema?.Properties != null && schema.Properties.Any(p => p.Value.MergeType == MergeType.Key) && schema.Properties.Where(p => p.Value.MergeType == MergeType.Key).All(p =>
            {
                (source as IDictionary<string, object>).TryGetValue(p.Key, out var s);
                (overrides as IDictionary<string, object>).TryGetValue(p.Key, out var o);
                return object.Equals(s, o);
            });
        }
    }
}
