// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    internal class RestoreGitResult
    {
        public string Path { get; private set; }

        public string Remote { get; private set; }

        public string Branch { get; private set; }

        public string Commit { get; private set; }

        public RestoreGitResult(string path, string remote, string branch, string commit)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));
            Debug.Assert(!string.IsNullOrEmpty(remote));
            Debug.Assert(!string.IsNullOrEmpty(branch));
            Debug.Assert(!string.IsNullOrEmpty(commit));

            Path = path;
            Remote = remote;
            Branch = branch;
            Commit = commit;
        }
    }
}
