// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dotnet
{
    internal interface IResolverPipeline
    {
        void Run(MetadataModel yaml, ResolverContext context);
    }
}
