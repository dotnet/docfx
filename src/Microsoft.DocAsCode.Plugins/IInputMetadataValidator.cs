// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Immutable;

    public interface IInputMetadataValidator
    {
        void Validate(string sourceFile, ImmutableDictionary<string, object> metadata);
    }
}
