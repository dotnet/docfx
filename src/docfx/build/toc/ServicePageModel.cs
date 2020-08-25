// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class ServicePageModel
    {
        public SourceInfo<string?> Name { get; private set; }

        public SourceInfo<string?> FullName { get; private set; }

        public SourceInfo<string?> Href { get; private set; }

        public SourceInfo<string?> Uid { get; private set; }

        public List<SourceInfo<ServicePageModel>> Children { get; private set; } = new List<SourceInfo<ServicePageModel>>();

        public List<string?> Langs { get; private set; } = new List<string?>();

        public SourceInfo<string?> PageType { get; private set; }

        public JObject Metadata { get; set; } = new JObject();

        public SourceInfo<string?> YamlMime { get; private set; }

        public ServicePageModel(
            SourceInfo<string?> name,
            SourceInfo<string?> fullName,
            SourceInfo<string?> href,
            SourceInfo<string?> uid,
            List<SourceInfo<ServicePageModel>> children,
            List<string?> langs,
            SourceInfo<string?> pageType,
            SourceInfo<string?> yamlMime)
        {
            Name = name;
            FullName = fullName;
            Href = href;
            Uid = uid;
            Children = children;
            Langs = langs;
            PageType = pageType;
            YamlMime = yamlMime;
        }

        public ServicePageModel(
            SourceInfo<string?> name,
            SourceInfo<string?> href,
            SourceInfo<string?> uid)
        {
            Name = name;
            Href = href;
            Uid = uid;
        }
    }
}
