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
        /// <returns>The splited path, query and fragment</returns>
        public static (string path, string query, string fragment) SplitHref(string href)
        {
            var path = "";
            var query = "";
            var fragment = "";

            var fragmentIndex = href.IndexOf('#');
            if (fragmentIndex >= 0)
            {
                fragment = href.Substring(fragmentIndex);
                var queryIndex = href.IndexOf('?', 0, fragmentIndex);
                if (queryIndex >= 0)
                {
                    query = href.Substring(queryIndex, fragmentIndex - queryIndex);
                    path = href.Substring(0, queryIndex);
                }
                else
                {
                    path = href.Substring(0, fragmentIndex);
                }
            }
            else
            {
                var queryIndex = href.IndexOf('?');
                if (queryIndex >= 0)
                {
                    query = href.Substring(queryIndex);
                    path = href.Substring(0, queryIndex);
                }
                else
                {
                    path = href;
                }
            }

            return (path, query, fragment);
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
