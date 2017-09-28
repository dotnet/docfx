// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    public class XrefServiceResolver
    {
        private readonly List<UriTemplate<Task<List<XRefSpec>>>> _uriTemplates;

        public XrefServiceResolver(ImmutableArray<string> xrefServiceUrls, int maxParallelism)
            : this(null, xrefServiceUrls, maxParallelism) { }

        public XrefServiceResolver(HttpClient client, ImmutableArray<string> xrefServiceUrls, int maxParallelism)
        {
            _uriTemplates =
                (from url in xrefServiceUrls
                 select UriTemplate.Create(
                     url,
                     new XrefClient(client, maxParallelism).ResolveAsync,
                     GetPipeline)).ToList();
        }

        public async Task<List<string>> ResolveAsync(List<string> uidList, ConcurrentDictionary<string, XRefSpec> externalXRefSpec)
        {
            if (_uriTemplates.Count == 0)
            {
                return uidList;
            }

            var unresolvedUidList = new List<string>();

            var xrefObjects = await Task.WhenAll(
                from uid in uidList
                select ResolveAsync(uid));
            foreach (var tuple in uidList.Zip(xrefObjects, Tuple.Create))
            {
                if (tuple.Item2 == null)
                {
                    unresolvedUidList.Add(tuple.Item1);
                }
                else
                {
                    externalXRefSpec.TryAdd(tuple.Item1, tuple.Item2);
                }
            }
            return unresolvedUidList;
        }

        public async Task<XRefSpec> ResolveAsync(string uid)
        {
            var d = new Dictionary<string, string> { ["uid"] = uid };
            foreach (var t in _uriTemplates)
            {
                List<XRefSpec> value = null;
                try
                {
                    value = await t.Evaluate(d);
                }
                catch (Exception ex)
                {
                    Logger.LogInfo($"Unable to resolve uid ({uid}) from {t.Template}, details: {ex.Message}");
                }
                if (value?.Count > 0)
                {
                    return value[0];
                }
            }
            return null;
        }

        private IUriTemplatePipeline<Task<List<XRefSpec>>> GetPipeline(string name)
        {
            // todo : add pipeline.
            return EmptyUriTemplatePipeline.Default;
        }

        private sealed class EmptyUriTemplatePipeline : IUriTemplatePipeline<Task<List<XRefSpec>>>
        {
            public static readonly EmptyUriTemplatePipeline Default = new EmptyUriTemplatePipeline();

            public Task<List<XRefSpec>> Handle(Task<List<XRefSpec>> value, string[] parameters)
            {
                return value;
            }
        }
    }
}
