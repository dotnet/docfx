// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Reflection;

    using Microsoft.DocAsCode.DataContracts.Common.Attributes;

    public class UniqueIdentityReferenceHandler : IModelAttributeHandler
    {
        private readonly static ConcurrentDictionary<Type, UniqueIdentityHandlerImpl> _cache = new ConcurrentDictionary<Type, UniqueIdentityHandlerImpl>();

        public void Handle(object obj, HandleModelAttributesContext context)
        {
            if (obj == null)
            {
                return;
            }
            var type = obj.GetType();
            _cache.GetOrAdd(type, new UniqueIdentityHandlerImpl(type, this)).Handle(obj, context);
        }

        private sealed class UniqueIdentityHandlerImpl : BaseModelAttributeHandler<UniqueIdentityReferenceAttribute>
        {
            public UniqueIdentityHandlerImpl(Type type, IModelAttributeHandler handler) : base(type, handler)
            {
            }

            protected override void HandleCurrentProperty(object declaringObject, PropertyInfo currentPropertyInfo, HandleModelAttributesContext context)
            {
                var obj = currentPropertyInfo.GetValue(declaringObject);
                if (obj == null)
                {
                    return;
                }

                var val = obj as string;
                if (val != null)
                {
                    context.LinkToUids.Add(val);
                }
                else
                {
                    var list = obj as IEnumerable;
                    if (list != null)
                    {
                        foreach (var i in list)
                        {
                            if (i != null)
                            {
                                var item = i as string;
                                if (item != null)
                                {
                                    context.LinkToUids.Add(item);
                                }
                                else
                                {
                                    throw new NotSupportedException($"Type {obj.GetType()} inside IEnumerable is NOT a supported item type for {nameof(UniqueIdentityReferenceAttribute)}");
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new NotSupportedException($"Type {obj.GetType()} is NOT a supported type for {nameof(UniqueIdentityReferenceAttribute)}");
                    }
                }
            }
        }
    }
}
