// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Microsoft.DocAsCode.DataContracts.Common;

    public class UniqueIdentityReferenceHandler : IModelAttributeHandler
    {
        private readonly ConcurrentDictionary<Type, UniqueIdentityHandlerImpl> _cache = new ConcurrentDictionary<Type, UniqueIdentityHandlerImpl>();

        public object Handle(object obj, HandleModelAttributesContext context)
        {
            if (obj == null)
            {
                return null;
            }
            var type = obj.GetType();
            return _cache.GetOrAdd(type, t => new UniqueIdentityHandlerImpl(t, this)).Handle(obj, context);
        }

        private sealed class UniqueIdentityHandlerImpl : BaseModelAttributeHandler<UniqueIdentityReferenceAttribute>
        {
            public UniqueIdentityHandlerImpl(Type type, IModelAttributeHandler handler) : base(type, handler)
            {
            }

            protected override object HandleCurrent(object currentObj, object declaringObject, PropertyInfo currentPropertyInfo, HandleModelAttributesContext context)
            {
                if (currentObj == null && currentPropertyInfo != null && declaringObject != null)
                {
                    currentObj = currentPropertyInfo.GetValue(declaringObject);
                }

                if (currentObj == null)
                {
                    return null;
                }

                var val = currentObj as string;
                if (val != null)
                {
                    context.LinkToUids.Add(val);
                }
                else
                {
                    var list = currentObj as IEnumerable;
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
                                    throw new NotSupportedException($"Type {currentObj.GetType()} inside IEnumerable is NOT a supported item type for {nameof(UniqueIdentityReferenceAttribute)}");
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new NotSupportedException($"Type {currentObj.GetType()} is NOT a supported type for {nameof(UniqueIdentityReferenceAttribute)}");
                    }
                }

                return currentObj;
            }

            protected override IEnumerable<PropInfo> GetProps(Type type)
            {
                return from prop in base.GetProps(type)
                       where !prop.Prop.IsDefined(typeof(UniqueIdentityReferenceIgnoreAttribute), false)
                       select prop;
            }
        }
    }
}
