// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Utility
{
    using System;

    public static class UriUtility
    {
        private const string QueryMarker = "?";
        private const string FragmentMarker = "#";

        public static bool HasFragment(string uriString)
        {
            if (string.IsNullOrEmpty(uriString)) throw new ArgumentNullException(nameof(uriString));
            return uriString.IndexOf(FragmentMarker) != -1;
        }

        public static string GetFragment(string uriString)
        {
            if (string.IsNullOrEmpty(uriString)) throw new ArgumentNullException(nameof(uriString));
            var index = uriString.IndexOf(FragmentMarker);
            return index == -1 ? string.Empty : uriString.Substring(index);
        }

        public static string GetNonFragment(string uriString)
        {
            if (string.IsNullOrEmpty(uriString)) throw new ArgumentNullException(nameof(uriString));
            var index = uriString.IndexOf(FragmentMarker);
            return index == -1 ? uriString : uriString.Remove(index);
        }

    }
}
