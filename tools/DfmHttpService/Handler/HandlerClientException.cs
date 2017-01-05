// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    public class HandlerClientException : HandlerException
    {
        public HandlerClientException() : this("Error happens while handling http context")
        {
        }

        public HandlerClientException(string message) : base(message)
        {
        }
    }
}