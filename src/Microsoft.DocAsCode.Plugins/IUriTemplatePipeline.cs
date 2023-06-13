// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DocAsCode.Plugins;

public interface IUriTemplatePipeline<T>
{
    T Handle(T value, string[] parameters);
}
