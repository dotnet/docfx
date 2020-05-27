// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Validators
{
    using System.Collections.Immutable;


    public interface IMarkdownObjectValidatorProvider
    {
        ImmutableArray<IMarkdownObjectValidator> GetValidators();
    }
}
