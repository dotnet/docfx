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
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;

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
                    externalXRefSpec.AddOrUpdate(tuple.Item1, tuple.Item2, (s, x) => x + tuple.Item2);
                }
            }
            if (unresolvedUidList.Count > 0 && Logger.LogLevelThreshold <= LogLevel.Verbose)
            {
                var capacity = 256 + 64 * (Math.Min(100, unresolvedUidList.Count)) + 64 * _uriTemplates.Count;
                var sb = new StringBuilder(capacity);
                sb.Append("Cannot resolve ");
                sb.Append(unresolvedUidList.Count);
                sb.Append(" uids by xref service, top 100:");
                foreach (var uid in unresolvedUidList.Take(100))
                {
                    sb.AppendLine().Append("    ").Append(uid);
                }
                sb.AppendLine().Append("  ").Append("xref service:");
                foreach (var t in _uriTemplates)
                {
                    sb.AppendLine().Append("    ").Append(t.Template);
                }
                Logger.LogVerbose(sb.ToString());
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
            // todo: pluggable.
            switch (name)
            {
                case "removeHost":
                    return RemoveHostUriTemplatePipeline.Default;
                case "addQueryString":
                    return AddQueryStringUriTemplatePipeline.Default;
                default:
                    Logger.LogWarning($"Unknown uri template pipeline: {name}.", code: WarningCodes.Build.UnknownUriTemplatePipeline);
                    return EmptyUriTemplatePipeline.Default;
            }
        }

        private sealed class RemoveHostUriTemplatePipeline : IUriTemplatePipeline<Task<List<XRefSpec>>>
        {
            public static readonly RemoveHostUriTemplatePipeline Default = new RemoveHostUriTemplatePipeline();

            public async Task<List<XRefSpec>> Handle(Task<List<XRefSpec>> value, string[] parameters)
            {
                var list = await value;
                foreach (var item in list)
                {
                    if (string.IsNullOrEmpty(item.Href))
                    {
                        continue;
                    }
                    if (Uri.TryCreate(item.Href, UriKind.Absolute, out var uri))
                    {
                        if (parameters.Length == 0 ||
                            Array.IndexOf(parameters, uri.Host) != -1)
                        {
                            item.Href = item.Href.Substring(uri.GetLeftPart(UriPartial.Authority).Length);
                        }
                    }
                }
                return list;
            }
        }

        private sealed class AddQueryStringUriTemplatePipeline : IUriTemplatePipeline<Task<List<XRefSpec>>>
        {
            public static readonly AddQueryStringUriTemplatePipeline Default = new AddQueryStringUriTemplatePipeline();

            public Task<List<XRefSpec>> Handle(Task<List<XRefSpec>> value, string[] parameters)
            {
                if (parameters.Length == 2 &&
                    !string.IsNullOrEmpty(parameters[0]) &&
                    !string.IsNullOrEmpty(parameters[1]))
                {
                    return HandleCoreAsync(value, parameters[0], parameters[1]);
                }
                return value;
            }

            private async Task<List<XRefSpec>> HandleCoreAsync(Task<List<XRefSpec>> task, string name, string value)
            {
                var list = await task;
                foreach (var item in list)
                {
                    if (string.IsNullOrEmpty(item.Href))
                    {
                        continue;
                    }
                    var mvc = HttpUtility.ParseQueryString(UriUtility.GetQueryString(item.Href));
                    mvc[name] = value;
                    item.Href = UriUtility.GetPath(item.Href) +
                        "?" + mvc.ToString() +
                        UriUtility.GetFragment(item.Href);
                }
                return list;
            }
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
