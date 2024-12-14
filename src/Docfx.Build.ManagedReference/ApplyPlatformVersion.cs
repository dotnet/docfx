// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Docfx.Build.Common;
using Docfx.Common;
using Docfx.DataContracts.ManagedReference;
using Docfx.Plugins;
using Newtonsoft.Json.Linq;

namespace Docfx.Build.ManagedReference;

public class ApplyPlatformVersion : BaseDocumentBuildStep
{
    public override int BuildOrder => 0x10;

    public override string Name => nameof(ApplyPlatformVersion);

    public override IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host)
    {
        host.LogInfo("Applying platform-version from metadata...");
        models.RunAll(m =>
        {
            if (m.Type != DocumentType.Article)
            {
                return;
            }
            var page = m.Content as PageViewModel;
            if (page?.Metadata != null &&
                page.Metadata.Remove("platform", out object value))
            {
                var list = GetPlatformVersionFromMetadata(value);
                if (list != null)
                {
                    list.Sort();
                    foreach (var item in page.Items)
                    {
                        if (item.Platform == null)
                        {
                            item.Platform = list;
                        }
                        else
                        {
                            var set = new SortedSet<string>(item.Platform);
                            foreach (var pv in list)
                            {
                                set.Add(pv);
                            }
                            item.Platform = set.ToList();
                        }
                    }
                }
            }
        });
        host.LogInfo("Platform applied.");
        return models;
    }

    private static List<string> GetPlatformVersionFromMetadata(object value)
    {
        if (value is string text)
        {
            return [text];
        }

        if (value is IEnumerable<object> collection)
        {
            return collection.OfType<string>().ToList();
        }

        if (value is JArray jArray)
        {
            try
            {
                return jArray.ToObject<List<string>>();
            }
            catch (Exception)
            {
                Logger.LogWarning($"Unknown platform-version metadata: {jArray}");
            }
        }

        return null;
    }
}
