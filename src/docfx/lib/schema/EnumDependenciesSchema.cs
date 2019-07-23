// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    // srting -> attribute name, JToken -> valid value, EnumDependenciesSchema -> other attribute depends top attribute value
    internal class EnumDependenciesSchema : Dictionary<string, Dictionary<JToken, EnumDependenciesSchema>>
    {
    }
}
