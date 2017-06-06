// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference
{
    using System;
    using System.Composition;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Newtonsoft.Json.Linq;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.Plugins;

    public class ApplyPlatformVersion : BaseDocumentBuildStep, ISupportIncrementalBuildStep
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
                    page.Metadata.TryGetValue("platform", out object value))
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

        #region ISupportIncrementalBuildStep Members

        public bool CanIncrementalBuild(FileAndType fileAndType) => true;

        public string GetIncrementalContextHash() => null;

        public IEnumerable<DependencyType> GetDependencyTypesToRegister() => null;

        #endregion

        private static List<string> GetPlatformVersionFromMetadata(object value)
        {
            if (value is string text)
            {
                return new List<string> { text };
            }

            if (value is IEnumerable<object> collection)
            {
                return collection.OfType<string>().ToList();
            }

            if (value is JArray jarray)
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
