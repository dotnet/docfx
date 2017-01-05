// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System.Net;

    public class HttpContext
    {
        public HttpListenerContext Context { get; set; }

        public DfmHttpServer Server { get; set; }

        private CommandMessage _message;

        public CommandMessage Message
        {
            get
            {
                _message = _message ?? Utility.GetCommandMessage(Context);
                return _message;
            }
        }
    }
}