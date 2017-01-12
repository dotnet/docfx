// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System.Net;

    internal class ServiceContext
    {
        private CommandMessage _message;

        public HttpListenerContext HttpContext { get; set; }

        public DfmHttpServer Server { get; set; }


        public CommandMessage Message
        {
            get
            {
                _message = _message ?? Utility.GetCommandMessage(HttpContext);
                return _message;
            }
        }
    }
}