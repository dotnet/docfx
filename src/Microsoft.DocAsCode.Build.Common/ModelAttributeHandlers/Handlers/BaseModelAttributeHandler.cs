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
        private const int MaximumNestedLevel = 32;
        private readonly TypeInfo _typeInfo;
        protected readonly IModelAttributeHandler Handler;
        private Type _type;
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
            _type = type;
            _typeInfo = GetTypeInfo(type);
            Handler = handler;
        }

        protected abstract object HandleCurrent(object currentObj, object declaringObject, PropertyInfo currentPropertyInfo, HandleModelAttributesContext context);

        public object Handle(object obj, HandleModelAttributesContext context)
        {
            var type = obj.GetType();
            if (type != _type)
            {
                throw new InvalidOperationException($"Input type {type} is not the supported type {_type}");
            }

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
                if (context.NestedLevel > MaximumNestedLevel)
                {
                    // If maximum nested level reached, return the object directly
                    return obj;
                }

                context.NestedLevel++;

                if (_typeInfo.TypeOfType == TypeOfType.IDictionary)
                {
                    result = HandleDictionaryType(obj, context);
                }
                else if (_typeInfo.TypeOfType == TypeOfType.IEnumerable)
                {
                    result = HandleIEnumerableType(obj, context);
                }
                else if (_typeInfo.TypeOfType == TypeOfType.Primitive)
                {
                    result = ProcessPrimitiveType(obj, context);
                }
                else
                {
                    result = ProcessNonPrimitiveType(obj, context);
                }

                context.NestedLevel--;
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
            Func<object, object> handler = s => Handler.Handle(s, context);
            if (!HandleGenericItemsHelper.EnumerateIDictionary(currentObj, handler))
            {
                HandleGenericItemsHelper.EnumerateIReadonlyDictionary(currentObj, handler);
            }
            return currentObj;
        }

        /// <summary>
        /// By default enumerate Enumerable type if it does not have defined Attribute
        /// </summary>
        /// <param name="currentObj"></param>
        /// <param name="context"></param>
        protected virtual object HandleIEnumerableType(object currentObj, HandleModelAttributesContext context)
        {
            if (currentObj == null)
            {
                return null;
            }
            Func<object, object> handler = s => Handler.Handle(s, context);
            HandleGenericItemsHelper.EnumerateIEnumerable(currentObj, s => Handler.Handle(s, context));
            return currentObj;
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
                foreach (var prop in _typeInfo.PropInfos)
                {
                    var value = ReflectionHelper.GetPropertyValue(currentObj, prop.Prop);
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

        protected virtual IEnumerable<PropInfo> GetProps(Type type)
        {
            return from prop in ReflectionHelper.GetGettableProperties(type)
                   let attr = prop.GetCustomAttribute<T>()
                   select new PropInfo
                   {
                       Prop = prop,
                       Attr = attr,
                   };
        }

        protected sealed class PropInfo
        {
            public PropertyInfo Prop { get; set; }
            public Attribute Attr { get; set; }
        }

        private TypeInfo GetTypeInfo(Type type)
        {
            if (type.IsPrimitive)
            {
                return new TypeInfo
                {
                    TypeOfType = TypeOfType.Primitive
                };
            }
            if (type == typeof(string))
            {
                return new TypeInfo
                {
                    TypeOfType = TypeOfType.String
                };
            }
            if (ReflectionHelper.IsDictionaryType(type))
            {
                return new TypeInfo
                {
                    TypeOfType = TypeOfType.IDictionary
                };
            }
            if (ReflectionHelper.IsIEnumerableType(type))
            {
                return new TypeInfo
                {
                    TypeOfType = TypeOfType.IEnumerable
                };
            }

            var propInfos = GetProps(type);
            return new TypeInfo
            {
                TypeOfType = TypeOfType.NonPrimitive,
                PropInfos = propInfos.ToArray(),
            };
        }

        private sealed class TypeInfo
        {
            public PropInfo[] PropInfos { get; set; }
            public TypeOfType TypeOfType { get; set; }
        }

        private enum TypeOfType
        {
            IDictionary,
            IEnumerable,
            Primitive,
            String,
            NonPrimitive,
        }
    }
}
