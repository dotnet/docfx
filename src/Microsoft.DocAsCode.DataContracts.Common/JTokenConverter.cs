// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.DocAsCode.DataContracts.Common;

public static class JTokenConverter
{
    public static T Convert<T>(object obj)
    {
        if (obj is T)
        {
            return (T)obj;
        }

        if (obj is JToken jtoken)
        {
            return jtoken.ToObject<T>();
        }
        throw new InvalidCastException();
    }
}
