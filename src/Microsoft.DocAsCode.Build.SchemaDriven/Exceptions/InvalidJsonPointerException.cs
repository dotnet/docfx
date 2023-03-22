// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.Exceptions;

public class InvalidJsonPointerException : DocumentException
{
    public InvalidJsonPointerException() : this("The value of json pointer is not valid")
    {
    }

    public InvalidJsonPointerException(string message) : base(message)
    {
    }

    public InvalidJsonPointerException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
