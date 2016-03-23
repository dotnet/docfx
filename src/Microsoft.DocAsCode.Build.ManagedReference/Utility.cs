// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference
{
    using System.Text.RegularExpressions;

    public static class Utility
    {
        /// <summary>
        /// TODO: completely move into template
        /// Must be consistent with template input.replace(/\W/g, '_');
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private static Regex HtmlEncodeRegex = new Regex(@"\W", RegexOptions.Compiled);
        public static string GetHtmlId(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return HtmlEncodeRegex.Replace(id, "_");
        }
    }
}
