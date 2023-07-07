// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

using Docfx.DataContracts.Common;

namespace Docfx.Build.Common;

public class UniqueIdentityReferenceHandler : IModelAttributeHandler
{
    private readonly ConcurrentDictionary<Type, UniqueIdentityHandlerImpl> _cache = new();

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

            if (currentObj is string val)
            {
                context.LinkToUids.Add(val);
            }
            else
            {
                if (currentObj is IEnumerable list)
                {
                    foreach (var i in list)
                    {
                        if (i != null)
                        {
                            if (i is string item)
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
