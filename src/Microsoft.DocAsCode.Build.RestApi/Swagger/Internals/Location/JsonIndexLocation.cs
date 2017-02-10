// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Swagger.Internals
{
    using System.Text;

    internal class JsonIndexLocation : IJsonLocation
    {
        private readonly int _position;

        public JsonIndexLocation(int position)
        {
            _position = position;
        }

        public void WriteTo(StringBuilder sb)
        {
            sb.Append('/');
            sb.Append(_position);
        }
    }
}
