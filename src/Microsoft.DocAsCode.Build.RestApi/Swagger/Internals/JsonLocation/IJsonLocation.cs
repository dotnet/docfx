// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Swagger.Internals
{
    using System.Text;

    internal interface IJsonLocation
    {
        void WriteTo(StringBuilder sb);
    }
}
