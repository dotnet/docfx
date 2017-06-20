// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.IO;
    using System.Text.RegularExpressions;

    public static class UriUtility
    {
        private const char QueryMarker = '?';
        private const char FragmentMarker = '#';
        private static readonly char[] QueryAndFragmentMarkers = { QueryMarker, FragmentMarker };
        private static readonly Regex AbsoluteUriRegex = new Regex("^[a-zA-Z]+:.*$", RegexOptions.Compiled);

        public static bool HasFragment(string uriString)
        {
            if (uriString == null)
            {
                throw new ArgumentNullException(nameof(uriString));
            }
            return uriString.IndexOf(FragmentMarker) != -1;
        }

        public static bool HasQueryString(string uriString)
        {
            if (uriString == null)
            {
                throw new ArgumentNullException(nameof(uriString));
            }
            return uriString.IndexOf(QueryMarker) != -1;
        }

        public static string GetFragment(string uriString)
        {
            if (uriString == null)
            {
                throw new ArgumentNullException(nameof(uriString));
            }
            var index = uriString.IndexOf(FragmentMarker);
            return index == -1 ? string.Empty : uriString.Substring(index);
        }

        public static string GetNonFragment(string uriString)
        {
            if (uriString == null)
            {
                throw new ArgumentNullException(nameof(uriString));
            }
            var index = uriString.IndexOf(FragmentMarker);
            return index == -1 ? uriString : uriString.Remove(index);
        }

        public static string GetQueryString(string uriString)
        {
            if (uriString == null)
            {
                throw new ArgumentNullException(nameof(uriString));
            }
            var index = uriString.IndexOf(QueryMarker);
            return index == -1 ? string.Empty : GetNonFragment(uriString.Substring(index));
        }

        public static string GetPath(string uriString)
        {
            if (uriString == null)
            {
                throw new ArgumentNullException(nameof(uriString));
            }
            var index = uriString.IndexOfAny(QueryAndFragmentMarkers);
            return index == -1 ? uriString : uriString.Remove(index);
        }

        public static string GetQueryStringAndFragment(string uriString)
        {
            if (uriString == null)
            {
                throw new ArgumentNullException(nameof(uriString));
            }
            var index = uriString.IndexOfAny(QueryAndFragmentMarkers);
            return index == -1 ? string.Empty : uriString.Substring(index);
        }

        public static bool IsAbsoluteUri(string uriString)
        {
            if (uriString == null)
            {
                throw new ArgumentNullException(nameof(uriString));
            }
            return AbsoluteUriRegex.Match(uriString).Success;
        }

        public static bool IsAbsolutePath(string uriString)
        {
            if (uriString == null)
            {
                throw new ArgumentNullException(nameof(uriString));
            }
            var length = uriString.Length;
            return length >= 1 && (uriString[0] == Path.DirectorySeparatorChar || uriString[0] == Path.AltDirectorySeparatorChar);
        }
    }
}
