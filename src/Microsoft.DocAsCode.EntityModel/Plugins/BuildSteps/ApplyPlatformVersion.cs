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

    [Export(nameof(ManagedReferenceDocumentProcessor), typeof(IDocumentBuildStep))]
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
                    page.Metadata.TryGetValue("platformVersion", out value))
                {
                    var list = GetPlatformVersionFromMetadata(value);
                    if (list != null)
                    {
                        foreach (var item in page.Items)
                        {
                            item.PlatformVersion = list;
                        }
                    }
                }
            });
            host.LogInfo("Platform-version applied.");
            return models;
        }

        private static List<string> GetPlatformVersionFromMetadata(object value)
        {
            var text = value as string;
            if (text != null)
            {
                return new List<string> { text };
            }

            var collection = value as IEnumerable<string>;
            if (collection != null)
            {
                return collection.ToList();
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
