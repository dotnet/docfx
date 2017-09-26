// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class XrefServiceResolver
    {
        private readonly ImmutableArray<string> _xrefServiceUrls;

        public XrefServiceResolver(ImmutableArray<string> xrefServiceUrls)
        {
            _xrefServiceUrls = xrefServiceUrls;
        }

        private async Task<List<string>> ResolveByXRefServiceAsync(List<string> uidList, ConcurrentDictionary<string, XRefSpec> externalXRefSpec)
        {
            if (_xrefServiceUrls == null || _xrefServiceUrls.Length == 0)
            {
                return uidList;
            }

            var unresolvedUidList = new List<string>();
            var resolve = GetResolver();

            // todo : parallel.
            foreach (var uid in uidList)
            {
                var result = await resolve(uid);
                if (resolve == null)
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

        private Func<string, Task<XRefSpec>> GetResolver()
        {
            var uriTemplates =
                (from url in _xrefServiceUrls
                 select UriTemplate.Create(url, XrefClient.Default.ResloveAsync, GetPipeline)).ToList();
            return async uid =>
            {
                var d = new Dictionary<string, string> { ["uid"] = uid };
                foreach (var t in uriTemplates)
                {
                    XRefSpec[] value = null;
                    try
                    {
                        value = await t.Evaluate(d);
                    }
                    catch (Exception)
                    {
                    }
                    if (value != null && value.Length > 0)
                    {
                        return value[0];
                    }
                }
                return null;
            };
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
