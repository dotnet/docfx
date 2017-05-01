// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.UniversalReference
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class ApiListInDevlangs<T>: List<ApiLanguageValuePair<T>>
    {
    }
}
