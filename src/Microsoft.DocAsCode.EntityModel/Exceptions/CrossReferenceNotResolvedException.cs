// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Exceptions
{
    using System;
    using System.Runtime.Serialization;
    using System.Security.Permissions;

    using Microsoft.DocAsCode.Plugins;

    [Serializable]
    public class CrossReferenceNotResolvedException : DocumentException
    {
        public string Uid { get; }
        public string UidRawText { get; }
        public string File { get; set; }

        public CrossReferenceNotResolvedException(string uid, string uidRawText, string file) : base()
        {
            Uid = uid;
            UidRawText = uidRawText;
            File = file;
        }

        protected CrossReferenceNotResolvedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Uid = info.GetString(nameof(Uid));
            UidRawText = info.GetString(nameof(UidRawText));
            File = info.GetString(nameof(File));
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info,
            StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(Uid), Uid);
            info.AddValue(nameof(UidRawText), UidRawText);
            info.AddValue(nameof(File), File);
        }
    }
}
