// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Reflection;

    public abstract class BaseHandler<T> : IModelAttributeHandler where T: Attribute
    {
        private readonly PropInfo[] _props;
        private readonly IModelAttributeHandler _handler;

        protected BaseHandler(Type type, IModelAttributeHandler handler)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _props = GetProps(type);
            _handler = handler;
        }

        protected abstract void HandleCurrentProperty(object declaringObject, PropertyInfo currentPropertyInfo, HandleModelAttributesContext context);

        public virtual void Handle(object obj, HandleModelAttributesContext context)
        {
            if (obj == null)
            {
                return;
            }

            foreach (var prop in _props)
            {
                if (prop.Attr != null)
                {
                    HandleCurrentProperty(obj, prop.Prop, context);
                }
                else
                {
                    var type = prop.Prop.PropertyType;
                    if (ReflectionHelper.IsDictionaryType(type))
                    {
                        // Not supported
                    }
                    else if (type != typeof(string) && ReflectionHelper.IsIEnumerableType(type))
                    {
                        var propertyValue = prop.Prop.GetValue(obj);
                        if (propertyValue != null)
                        {
                            var value = (IEnumerable)propertyValue;
                            foreach (var i in value)
                            {
                                object temp = i;
                                _handler.Handle(temp, context);
                            }
                        }
                    }
                    else if (!type.IsPrimitive)
                    {
                        var propertyObject = prop.Prop.GetValue(obj);
                        _handler.Handle(propertyObject, context);
                    }
                }
            }
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
