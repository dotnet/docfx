// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Runtime.Serialization;
    using System.Security.Permissions;

    using Microsoft.DocAsCode.Plugins;

    [Serializable]
    public class InvalidCrossReferenceException : DocumentException
    {
        public XRefDetails XRefDetails { get; }

        public InvalidCrossReferenceException(XRefDetails xrefDetails) : base()
        {
            XRefDetails = xrefDetails;
        }

        protected InvalidCrossReferenceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            XRefDetails = (XRefDetails)info.GetValue(nameof(XRefDetails), typeof(XRefDetails));
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(XRefDetails), XRefDetails);
            base.GetObjectData(info, context);
        }
    }
}
