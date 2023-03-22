// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Exceptions;

namespace Microsoft.DocAsCode.Build.Engine;

public class InvalidPreprocessorException : DocfxException
{
    public InvalidPreprocessorException(string message) : base(message)
    {
    }
}
