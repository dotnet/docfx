// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System;
    using System.Composition;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Newtonsoft.Json.Linq;

    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Plugins;

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
                object value;
                if (page?.Metadata != null &&
                    page.Metadata.TryGetValue("platform", out value))
                {
                    page.Metadata.Remove("platform");
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
            var text = value as string;
            if (text != null)
            {
                return new List<string> { text };
            }

            var collection = value as IEnumerable<object>;
            if (collection != null)
            {
                return collection.OfType<string>().ToList();
            }

            var jarray = value as JArray;
            if (jarray != null)
            {
                try
                {
                    return jarray.ToObject<List<string>>();
                }
                catch (Exception)
                {
                    Logger.LogWarning($"Unknown platform-version metadata: {jarray.ToString()}");
                }
            }

            return null;
        }
    }
}
