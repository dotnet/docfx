// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;

    [Serializable]
    public class BuildCacheException : Exception
    {
        public BuildCacheException()
        {
        }

        public BuildCacheException(string message) : base(message)
        {
        }

        public BuildCacheException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
