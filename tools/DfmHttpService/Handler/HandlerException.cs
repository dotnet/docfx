// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System;

    internal abstract class HandlerException : Exception
    {
        public HandlerException() : this("Error happens while handling http context")
        {
        }

        public HandlerException(string message) : base(message)
        {
        }
    }
}