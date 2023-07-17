// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Docfx.Build.RestApi.Swagger.Internals;

internal interface IJsonLocation
{
    void WriteTo(StringBuilder sb);
}
