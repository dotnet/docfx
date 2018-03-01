// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    internal class HandlerClientException : HandlerException
    {
        public HandlerClientException() : this("Client error happens while handling http context")
        {
        }

        public HandlerClientException(string message) : base(message)
        {
        }
    }
}