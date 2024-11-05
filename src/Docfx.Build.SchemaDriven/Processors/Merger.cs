// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.Exceptions;

namespace Docfx.Build.SchemaDriven.Processors;

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

    private static void ThrowError(string message)
    {
        Logger.LogError(message);
        throw new InvalidOverwriteDocumentException(message);
    }

    private static bool TestKey(object source, object overrides, BaseSchema schema)
    {
        if (overrides == null || source == null)
        {
            return false;
        }

        var properties = schema?.Properties;
        if (properties == null)
        {
            return false;
        }

        var sourceDictionary = (IDictionary<string, object>)source;
        var overridesDictionary = (IDictionary<string, object>)overrides;
        return properties.Any(p => p.Value.MergeType == MergeType.Key) &&
               properties.Where(p => p.Value.MergeType == MergeType.Key)
                   .All(p =>
                   {
                       sourceDictionary.TryGetValue(p.Key, out var s);
                       overridesDictionary.TryGetValue(p.Key, out var o);
                       return object.Equals(s, o);
                   });
    }
}
