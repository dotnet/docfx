// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Git
{
    using System;

    [Serializable]
    public enum RepoType
    {
        Unknown,
        GitHub,
        Vso
    }
}