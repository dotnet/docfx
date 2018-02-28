// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;

    public class FolderRedirectionManager
    {
        private Dictionary<string, RelativePath> _redirections = new Dictionary<string, RelativePath>();

        public void AddFolderRedirectionRule(string from, string to)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }
            if (to == null)
            {
                throw new ArgumentNullException(nameof(to));
            }
            var fromRel = (RelativePath)from;
            var toRel = (RelativePath)(to.TrimEnd('/', '\\') + "/");
            if (fromRel == null || toRel == null || fromRel == toRel)
            {
                return;
            }

            var normalizedFrom = fromRel.ToString() + "/";
            foreach (var redirection in _redirections)
            {
                if (redirection.Key.StartsWith(normalizedFrom, StringComparison.OrdinalIgnoreCase)
                    || normalizedFrom.StartsWith(normalizedFrom, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException($"Can't add redirection rule ({from})=>({to}): conflicts with the existing rule ({redirection.Key})=>({redirection.Value})", nameof(from));
                }
            }
            _redirections[normalizedFrom] = toRel;
        }

        public RelativePath GetRedirectedPath(RelativePath file)
        {
            var fileRel = file.ToString();
            foreach (var redirection in _redirections)
            {
                if (fileRel.StartsWith(redirection.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return redirection.Value + (RelativePath)(fileRel.Substring(redirection.Key.Length));
                }
            }
            return file;
        }
    }
}
