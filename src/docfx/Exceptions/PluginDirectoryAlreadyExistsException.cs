// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class PluginDirectoryAlreadyExistsException : PluginException
    {
        public PluginDirectoryAlreadyExistsException(string directoryName) : base($"Plugin directory {directoryName} already exists! Please remove this directory manually and have a retry.")
        {
        }

        protected PluginDirectoryAlreadyExistsException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
