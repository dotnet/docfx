// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Microsoft.DocAsCode.Plugins;

public interface IInputMetadataValidator
{
    void Validate(string sourceFile, ImmutableDictionary<string, object> metadata);
}
