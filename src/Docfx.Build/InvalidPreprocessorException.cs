// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Exceptions;

namespace Docfx.Build.Engine;

public class InvalidPreprocessorException : DocfxException
{
    public InvalidPreprocessorException(string message) : base(message)
    {
    }
}
