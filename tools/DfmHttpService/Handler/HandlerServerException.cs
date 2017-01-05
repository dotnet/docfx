// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    public class HandlerServerException : HandlerException
    {
        public HandlerServerException() : this("Error happens while handling http context")
        {
        }

        public HandlerServerException(string message) : base(message)
        {
        }
    }
}