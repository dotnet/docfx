// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Reflection;

    public abstract class BaseModelAttributeHandler<T> : IModelAttributeHandler where T: Attribute
    {
        private const int MaximumNestedLevel = 32;
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

        protected abstract object HandleCurrent(object currentObj, object declaringObject, PropertyInfo currentPropertyInfo, HandleModelAttributesContext context);

        public object Handle(object obj, HandleModelAttributesContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (obj == null)
            {
                return null;
            }

            object result;
            if (ShouldHandle(obj, null, null, context))
            {
                result = HandleCurrent(obj, null, null, context);
            }
            else
            {
                if (context.NestedType.Count > MaximumNestedLevel)
                {
                    // If maximum nested level reached, return the object directly
                    return obj;
                }

                var type = obj.GetType();
                context.NestedType.Push(type);

                if (ReflectionHelper.IsDictionaryType(type))
                {
                    result = HandleDictionaryType(obj, context);
                }
                else if (type != typeof(string) && ReflectionHelper.IsIEnumerableType(type))
                {
                    result = HandleIEnumerableType(obj, context);
                }
                else if (type.IsPrimitive)
                {
                    result = ProcessPrimitiveType(obj, context);
                }
                else
                {
                    result = ProcessNonPrimitiveType(obj, context);
                }

                context.NestedType.Pop();
            }

            return result;
        }

        protected virtual bool ShouldHandle(object currentObj, object declaringObject, PropInfo currentPropInfo, HandleModelAttributesContext context)
        {
            return currentPropInfo != null && currentPropInfo.Attr != null;
        }

        /// <summary>
        /// By default enumerate Dictionary's value if it does not have defined Attribute
        /// </summary>
        /// <param name="declaringObject"></param>
        /// <param name="currentPropertyInfo"></param>
        /// <param name="context"></param>
        protected virtual object HandleDictionaryType(object currentObj, HandleModelAttributesContext context)
        {
            if (currentObj == null)
            {
                return null;
            }

            dynamic value = currentObj;
            foreach (var i in value)
            {
                Handler.Handle(i.Value, context);
            }

            return value;
        }

        /// <summary>
        /// By default enumerate Enumerable type if it does not have defined Attribute
        /// </summary>
        /// <param name="currentObj"></param>
        /// <param name="context"></param>
        protected virtual object HandleIEnumerableType(object currentObj, HandleModelAttributesContext context)
        {
            var value = (IEnumerable)currentObj;
            if (value == null)
            {
                return null;
            }

            foreach (var i in value)
            {
                Handler.Handle(i, context);
            }

            return value;
        }

        /// <summary>
        /// By default skip Primitive type if it does not have defined Attribute
        /// </summary>
        /// <param name="currentObj"></param>
        /// <param name="context"></param>
        protected virtual object ProcessPrimitiveType(object currentObj, HandleModelAttributesContext context)
        {
            return currentObj;
        }

        /// <summary>
        /// By default step into NonPrimitive type if it does not have defined Attribute
        /// </summary>
        /// <param name="currentObj"></param>
        /// <param name="context"></param>
        protected virtual object ProcessNonPrimitiveType(object currentObj, HandleModelAttributesContext context)
        {
            // skip string type
            if (currentObj != null && !(currentObj is string))
            {
                foreach (var prop in Props)
                {
                    var value = prop.Prop.GetValue(currentObj);
                    if (ShouldHandle(value, currentObj, prop, context))
                    {
                        HandleCurrent(value, currentObj, prop.Prop, context);
                    }
                    else
                    {
                        Handler.Handle(value, context);
                    }
                }
            }

            return currentObj;
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
