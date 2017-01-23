// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.Common
{
    using System;

    using Newtonsoft.Json.Linq;

    public static class JTokenConverter
    {
        public static T Convert<T>(object obj)
        {
            if (obj is T)
            {
                return (T)obj;
            }
            var jtoken = obj as JToken;
            if (jtoken != null)
            {
                return jtoken.ToObject<T>();
            }
            throw new InvalidCastException();
        }
    }
}
