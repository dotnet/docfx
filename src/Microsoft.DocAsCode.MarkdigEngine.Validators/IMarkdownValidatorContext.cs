// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Validators
{
    public interface IMarkdownValidatorContext
    {
        string GlobalRulesFilePath { get; }

        string CustomRulesFilePath { get; }
    }
}
