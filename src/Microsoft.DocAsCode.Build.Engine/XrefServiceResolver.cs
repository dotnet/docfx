// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    public class XrefServiceResolver
    {
        private readonly List<UriTemplate<Task<XRefSpec[]>>> _uriTemplates;

        public XrefServiceResolver(ImmutableArray<string> xrefServiceUrls)
        {
            _uriTemplates =
                (from url in xrefServiceUrls
                 select UriTemplate.Create(url, XrefClient.Default.ResloveAsync, GetPipeline)).ToList();
        }

        private async Task<List<string>> ResolveByXRefServiceAsync(List<string> uidList, ConcurrentDictionary<string, XRefSpec> externalXRefSpec)
        {
            if (_uriTemplates.Count == 0)
            {
                return uidList;
            }

            var unresolvedUidList = new List<string>();

            // todo : parallel.
            foreach (var uid in uidList)
            {
                var result = await ResolveAsync(uid);
                if (result == null)
                {
                    unresolvedUidList.Add(uid);
                }
                else
                {
                    externalXRefSpec.TryAdd(uid, result);
                }
            }
            return unresolvedUidList;
        }

        private async Task<XRefSpec> ResolveAsync(string uid)
        {
            var d = new Dictionary<string, string> { ["uid"] = uid };
            foreach (var t in _uriTemplates)
            {
                XRefSpec[] value = null;
                try
                {
                    value = await t.Evaluate(d);
                }
                catch (Exception)
                {
                    // todo : log.
                }
                if (value?.Length > 0)
                {
                    return value[0];
                }
            }
            return null;
        }

        private IUriTemplatePipeline<Task<XRefSpec[]>> GetPipeline(string name)
        {
            // todo : add pipeline.
            return EmptyUriTemplatePipeline.Default;
        }

        private sealed class EmptyUriTemplatePipeline : IUriTemplatePipeline<Task<XRefSpec[]>>
        {
            public static readonly EmptyUriTemplatePipeline Default = new EmptyUriTemplatePipeline();

            public Task<XRefSpec[]> Handle(Task<XRefSpec[]> value, string[] parameters)
            {
                return value;
            }
        }
    }
}
