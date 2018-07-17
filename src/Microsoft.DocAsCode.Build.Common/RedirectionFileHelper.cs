// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Text.RegularExpressions;

    public class RedirectionFileHelper
    {
        public static readonly Regex RedirectionRegex = new Regex(@"^\-{3}\s*(.+\:.*\s)*(redirect_url\:.*\s)(.+\:.*\s)*\s*\-{3}\s$", RegexOptions.Compiled | RegexOptions.Multiline, TimeSpan.FromSeconds(10));

        public static bool IsRedirectionFile(string markdown)
        {
            var match = RedirectionRegex.Match(markdown);
            return match.Success;
        }
    }
}
