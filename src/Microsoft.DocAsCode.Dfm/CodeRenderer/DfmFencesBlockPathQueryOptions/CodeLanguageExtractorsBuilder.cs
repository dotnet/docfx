// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Generic;

    internal sealed class CodeLanguageExtractorsBuilder
    {
        private readonly Dictionary<string, HashSet<string>> _alias = new Dictionary<string, HashSet<string>>();
        private readonly Dictionary<string, List<ICodeSnippetExtractor>> _dict = new Dictionary<string, List<ICodeSnippetExtractor>>();

        public CodeLanguageExtractorsBuilder AddAlias(string key, params string[] alias)
        {
            if (_alias.TryGetValue(key, out var set))
            {
                set.UnionWith(alias);
            }
            else
            {
                set = new HashSet<string>(alias);
                _alias[key] = set;
            }
            return this;
        }

        public CodeLanguageExtractorsBuilder Add(ICodeSnippetExtractor extractor, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (_dict.TryGetValue(key, out var list))
                {
                    list.Add(extractor);
                }
                else
                {
                    list = new List<ICodeSnippetExtractor> { extractor };
                    _dict[key] = list;
                }
            }
            return this;
        }

        public Dictionary<string, List<ICodeSnippetExtractor>> ToDictionay()
        {
            var result = new Dictionary<string, List<ICodeSnippetExtractor>>();
            foreach (var pair in _dict)
            {
                AddItem(result, pair.Key, pair.Value);
                if (_alias.TryGetValue(pair.Key, out var set))
                {
                    foreach (var item in set)
                    {
                        AddItem(result, item, pair.Value);
                    }
                }
            }
            return result;
        }

        private static void AddItem(Dictionary<string, List<ICodeSnippetExtractor>> result, string key, List<ICodeSnippetExtractor> value)
        {
            if (result.TryGetValue(key, out var list))
            {
                list.AddRange(value);
            }
            else
            {
                list = new List<ICodeSnippetExtractor>(value);
                result[key] = list;
            }
        }
    }
}
