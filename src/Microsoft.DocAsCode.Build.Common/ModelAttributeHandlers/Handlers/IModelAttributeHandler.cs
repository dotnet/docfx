﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    public interface IModelAttributeHandler
    {
        object Handle(object obj, HandleModelAttributesContext context);
    }
}
