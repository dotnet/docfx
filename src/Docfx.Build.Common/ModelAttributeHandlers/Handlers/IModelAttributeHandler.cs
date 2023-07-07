// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.Common;

public interface IModelAttributeHandler
{
    object Handle(object obj, HandleModelAttributesContext context);
}
