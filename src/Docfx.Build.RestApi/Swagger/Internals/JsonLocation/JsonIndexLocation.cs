// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Docfx.Build.RestApi.Swagger.Internals;

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
