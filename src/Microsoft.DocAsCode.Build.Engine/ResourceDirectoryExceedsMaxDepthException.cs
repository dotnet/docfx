// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Runtime.Serialization;
    using System.Security.Permissions;

    using Microsoft.DocAsCode.Exceptions;

    [Serializable]
    public class ResourceFileExceedsMaxDepthException : DocfxException
    {
        public int MaxDepth { get; }
        public string DirectoryName { get; }
        public string ResourceName { get; }

        public ResourceFileExceedsMaxDepthException(int maxDepth, string fileName, string resourceName) : base($"Resource file \"{fileName}\" in resource \"{resourceName}\" exceeds the max allowed depth {maxDepth}.")
        {
            MaxDepth = maxDepth;
            DirectoryName = fileName;
            ResourceName = resourceName;
        }

        protected ResourceFileExceedsMaxDepthException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            MaxDepth = info.GetInt32(nameof(MaxDepth));
            DirectoryName = info.GetString(nameof(DirectoryName));
            ResourceName = info.GetString(nameof(ResourceName));
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info,
            StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(MaxDepth), MaxDepth);
            info.AddValue(nameof(DirectoryName), DirectoryName);
            info.AddValue(nameof(ResourceName), ResourceName);
        }
    }
}
