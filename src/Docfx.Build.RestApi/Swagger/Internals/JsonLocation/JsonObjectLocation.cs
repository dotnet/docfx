// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Docfx.Build.RestApi.Swagger.Internals;

internal class JsonObjectLocation : IJsonLocation
{
    private readonly string _propertyName;

    public JsonObjectLocation(string propertyName)
    {
        _propertyName = propertyName;
    }

    public void WriteTo(StringBuilder sb)
    {
        sb.Append('/');
        sb.Append(RestApiHelper.FormatDefinitionSinglePath(_propertyName));
    }
}
