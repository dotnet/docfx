// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public abstract class BaseModelAttributeHandler<T> : IModelAttributeHandler where T: Attribute
    {
        protected readonly PropInfo[] Props;
        protected readonly IModelAttributeHandler Handler;

        protected BaseModelAttributeHandler(Type type, IModelAttributeHandler handler)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Props = GetProps(type);
            Handler = handler;
        }

        protected abstract void HandleCurrentProperty(object declaringObject, PropertyInfo currentPropertyInfo, HandleModelAttributesContext context);

        public virtual object Handle(object obj, HandleModelAttributesContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (obj == null)
            {
                return null;
            }

            foreach (var prop in Props)
            {
                if (ShouldHandle(prop, obj, context))
                {
                    HandleCurrentProperty(obj, prop.Prop, context);
                }
                else
                {
                    var type = prop.Prop.PropertyType;
                    if (ReflectionHelper.IsDictionaryType(type))
                    {
                        HandleDictionaryType(obj, prop.Prop, context);
                    }
                    else if (type != typeof(string) && ReflectionHelper.IsIEnumerableType(type))
                    {
                        HandleEnumerableType(obj, prop.Prop, context);
                    }
                    else if (type.IsPrimitive)
                    {
                        HandlePrimitiveType(obj, prop.Prop, context);
                    }
                    else
                    {
                        HandleNonPrimitiveType(obj, prop.Prop, context);
                    }
                }
            }

            return obj;
        }

        protected virtual bool ShouldHandle(PropInfo currentPropInfo, object declaringObject, HandleModelAttributesContext context)
        {
            return currentPropInfo.Attr != null;
        }

        /// <summary>
        /// By default enumerate Dictionary's value if it does not have defined Attribute
        /// </summary>
        /// <param name="declaringObject"></param>
        /// <param name="currentPropertyInfo"></param>
        /// <param name="context"></param>
        protected virtual void HandleDictionaryType(object declaringObject, PropertyInfo currentPropertyInfo, HandleModelAttributesContext context)
        {
            dynamic propertyValue = currentPropertyInfo.GetValue(declaringObject);
            if (propertyValue != null)
            {
                foreach (var i in propertyValue)
                {
                    Handler.Handle(i.Value, context);
                }
            }
        }

        /// <summary>
        /// By default enumerate Enumerable type if it does not have defined Attribute
        /// </summary>
        /// <param name="declaringObject"></param>
        /// <param name="currentPropertyInfo"></param>
        /// <param name="context"></param>
        protected virtual void HandleEnumerableType(object declaringObject, PropertyInfo currentPropertyInfo, HandleModelAttributesContext context)
        {
            var propertyValue = currentPropertyInfo.GetValue(declaringObject);
            if (propertyValue != null)
            {
                var value = (IEnumerable)propertyValue;
                foreach (var i in value)
                {
                    Handler.Handle(i, context);
                }
            }
        }

        /// <summary>
        /// By default skip Primitive type if it does not have defined Attribute
        /// </summary>
        /// <param name="decalringObject"></param>
        /// <param name="currentPropertyInfo"></param>
        /// <param name="context"></param>
        protected virtual void HandlePrimitiveType(object declaringObject, PropertyInfo currentPropertyInfo, HandleModelAttributesContext context)
        {
        }

        /// <summary>
        /// By default step into NonPrimitive type if it does not have defined Attribute
        /// </summary>
        /// <param name="decalringObject"></param>
        /// <param name="currentPropertyInfo"></param>
        /// <param name="context"></param>
        protected virtual void HandleNonPrimitiveType(object declaringObject, PropertyInfo currentPropertyInfo, HandleModelAttributesContext context)
        {
            var propertyObject = currentPropertyInfo.GetValue(declaringObject);
            Handler.Handle(propertyObject, context);
        }

        protected virtual PropInfo[] GetProps(Type type)
        {
            return (from prop in ReflectionHelper.GetGettableProperties(type)
                    let attr = prop.GetCustomAttribute<T>()
                    select new PropInfo
                    {
                        Prop = prop,
                        Attr = attr
                    }).ToArray();
        }

        protected sealed class PropInfo
        {
            public PropertyInfo Prop { get; set; }
            public Attribute Attr { get; set; }
        }
    }
}
