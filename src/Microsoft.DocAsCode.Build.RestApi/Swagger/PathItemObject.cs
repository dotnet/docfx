// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Swagger
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    using Microsoft.DocAsCode.YamlSerialization;

    /// <summary>
    /// TODO: need a converter
    /// </summary>
    [Serializable]
    public class PathItemObject : Dictionary<string, OperationObject>
    {
        [ExtensibleMember]
        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
