// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class PluginException : DocfxException
    {
        public PluginException() : this("Error happens when loading plugins for docfx")
        {
        }
        public PluginException(string message) : base(message)
        {
        }

        protected PluginException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
