// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Git
{
    using System;

    public class GitException : Exception
    {
        public GitException(string message):base(message)
        {
        }
    }
}