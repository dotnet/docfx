// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class HrefUtility
    {
        /// <summary>
        /// Split href to path, fragement and query
        /// </summary>
        /// <param name="href">The href</param>
        /// <returns>The splited path, fragment and query</returns>
        public static (string path, string fragment, string query) SplitHref(string href)
        {
            var path = "";
            var fragment = "";
            var query = "";

            var queryIndex = href.IndexOf('?');
            var fragmentIndex = href.IndexOf('#', queryIndex < 0 ? 0 : queryIndex);

            if (queryIndex >= 0)
            {
                path = href.Substring(0, queryIndex);

                if (fragmentIndex >= 0)
                {
                    query = href.Substring(queryIndex, fragmentIndex - queryIndex);
                    fragment = href.Substring(fragmentIndex);
                }
                else
                {
                    query = href.Substring(queryIndex);
                }
            }
            else
            {
                if (fragmentIndex >= 0)
                {
                    path = href.Substring(0, fragmentIndex);
                    fragment = href.Substring(fragmentIndex);
                }
                else
                {
                    path = href;
                }
            }

            return (path, fragment, query);
        }

        public static DependencyType FragmentToDependencyType(string fragment)
        {
            Debug.Assert(string.IsNullOrEmpty(fragment) || fragment[0] == '#');

            return fragment != null && fragment.Length > 1 ? DependencyType.Bookmark : DependencyType.Link;
        }

        public static bool IsAbsoluteHref(string str)
        {
            return str.StartsWith('/')
                || str.StartsWith('\\')
                || Uri.TryCreate(str, UriKind.Absolute, out _);
        }
    }
}
