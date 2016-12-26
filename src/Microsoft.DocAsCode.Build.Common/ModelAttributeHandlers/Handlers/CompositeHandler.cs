﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    public class CompositeModelAttributeHandler : IModelAttributeHandler
    {
        private IModelAttributeHandler[] _handlers;
        public CompositeModelAttributeHandler(params IModelAttributeHandler[] handlers)
        {
            _handlers = handlers;
        }

        public void Handle(object obj, HandleModelAttributesContext context)
        {
            foreach (var handler in _handlers)
            {
                handler.Handle(obj, context);
            }
        }
    }
}
