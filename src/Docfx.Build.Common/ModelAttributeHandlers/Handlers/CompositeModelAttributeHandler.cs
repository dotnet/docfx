// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.Common;

public class CompositeModelAttributeHandler : IModelAttributeHandler
{
    private readonly IModelAttributeHandler[] _handlers;
    public CompositeModelAttributeHandler(params IModelAttributeHandler[] handlers)
    {
        _handlers = handlers;
    }

    public object Handle(object obj, HandleModelAttributesContext context)
    {
        foreach (var handler in _handlers)
        {
            obj = handler.Handle(obj, context);
        }

        return obj;
    }
}
