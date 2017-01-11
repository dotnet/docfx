// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;

    public class MarkdownContentHandler : IModelAttributeHandler
    {
        private readonly ConcurrentDictionary<Type, MarkdownContentHandlerImpl> _cache = new ConcurrentDictionary<Type, MarkdownContentHandlerImpl>();

        public object Handle(object obj, HandleModelAttributesContext context)
        {
            if (obj == null)
            {
                return null;
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Host == null)
            {
                throw new ArgumentNullException(nameof(context.Host));
            }

            if (context.SkipMarkup)
            {
                return obj;
            }

            var type = obj.GetType();
            return _cache.GetOrAdd(type, new MarkdownContentHandlerImpl(type, this)).Handle(obj, context);
        }

        private sealed class MarkdownContentHandlerImpl : BaseModelAttributeHandler<MarkdownContentAttribute>
        {
            private const string ContentPlaceholder = "*content";

            public MarkdownContentHandlerImpl(Type type, IModelAttributeHandler handler) : base(type, handler)
            {
            }

            public override object Handle(object obj, HandleModelAttributesContext context)
            {
                // Special handle for *content
                var val = obj as string;
                if (val != null)
                {
                    string marked;
                    if (TryMarkupPlaceholderContent(val, context, out marked))
                    {
                        return marked;
                    }
                    else
                    {
                        return obj;
                    }
                }

                return base.Handle(obj, context);
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
                    var marked = Markup(val, context);
                    currentPropertyInfo.SetValue(declaringObject, marked);
                }
                else
                {
                    throw new NotSupportedException($"Type {obj.GetType()} is NOT a supported type for {nameof(MarkdownContentAttribute)}");
                }
            }

            protected override void HandleDictionaryType(object declaringObject, PropertyInfo currentPropertyInfo, HandleModelAttributesContext context)
            {
                HandleItems(typeof(IDictionary<,>), typeof(HandleIDictionaryItems<,>), declaringObject, currentPropertyInfo, context);
                base.HandleDictionaryType(declaringObject, currentPropertyInfo, context);
            }

            protected override void HandleEnumerableType(object declaringObject, PropertyInfo currentPropertyInfo, HandleModelAttributesContext context)
            {
                HandleItems(typeof(IList<>), typeof(HandleIListItems<>), declaringObject, currentPropertyInfo, context);
                base.HandleEnumerableType(declaringObject, currentPropertyInfo, context);
            }

            protected override void HandleNonPrimitiveType(object declaringObject, PropertyInfo currentPropertyInfo, HandleModelAttributesContext context)
            {
                if (context.EnableContentPlaceholder)
                {
                    var type = currentPropertyInfo.PropertyType;
                    if (type == typeof(string))
                    {
                        string result;
                        var val = (string)currentPropertyInfo.GetValue(declaringObject);
                        if (TryMarkupPlaceholderContent(val, context, out result) && result != val)
                        {
                            currentPropertyInfo.SetValue(declaringObject, result);
                        }
                    }
                }

                base.HandleNonPrimitiveType(declaringObject, currentPropertyInfo, context);
            }

            protected override PropInfo[] GetProps(Type type)
            {
                return (from prop in ReflectionHelper.GetSettableProperties(type)
                        let attr = prop.GetCustomAttribute<MarkdownContentAttribute>()
                        select new PropInfo
                        {
                            Prop = prop,
                            Attr = attr
                        }).ToArray();
            }

            private string Markup(string content, HandleModelAttributesContext context)
            {
                if (string.IsNullOrEmpty(content))
                {
                    return content;
                }

                string result;
                if (TryMarkupPlaceholderContent(content, context, out result))
                {
                    return result;
                }

                return MarkupCore(content, context);
            }

            private bool TryMarkupPlaceholderContent(string currentValue, HandleModelAttributesContext context, out string result)
            {
                result = null;
                if (context.EnableContentPlaceholder && IsPlaceholderContent(currentValue))
                {
                    context.ContainsPlaceholder = true;
                    result = context.PlaceholderContent;
                    return true;
                }

                return false;
            }

            private bool IsPlaceholderContent(string content)
            {
                return content != null && content.Trim() == ContentPlaceholder;
            }

            private string MarkupCore(string content, HandleModelAttributesContext context)
            {
                var host = context.Host;
                var mr = host.Markup(content, context.FileAndType);
                context.LinkToUids.UnionWith(mr.LinkToUids);
                context.LinkToFiles.UnionWith(mr.LinkToFiles);
                context.FileLinkSources = context.FileLinkSources.Merge(mr.FileLinkSources.Select(s => new KeyValuePair<string, IEnumerable<LinkSourceInfo>>(s.Key, s.Value)));
                context.UidLinkSources = context.UidLinkSources.Merge(mr.UidLinkSources.Select(s => new KeyValuePair<string, IEnumerable<LinkSourceInfo>>(s.Key, s.Value)));
                return mr.Html;
            }

            private void HandleItems(Type genericInterface, Type implHandlerType, object declaringObject, PropertyInfo currentPropertyInfo, HandleModelAttributesContext context)
            {
                var type = currentPropertyInfo.PropertyType;
                Type genericType;
                if (ReflectionHelper.TryGetGenericType(type, genericInterface, out genericType))
                {
                    var obj = currentPropertyInfo.GetValue(declaringObject);
                    if (obj != null)
                    {
                        var implType = implHandlerType.MakeGenericType(genericType.GetGenericArguments());
                        var instance = (IHandleItems)Activator.CreateInstance(implType, obj);
                        instance.Handle(s => Handler.Handle(s, context));
                    }
                }
            }

            private interface IHandleItems
            {
                void Handle(Func<object, object> handler);
            }

            private class HandleIListItems<T> : IHandleItems
            {
                private readonly IList<T> _list;
                public HandleIListItems(IList<T> list)
                {
                    _list = list;
                }
                public void Handle(Func<object, object> handler)
                {
                    Handle(s => (T)handler((T)s));
                }

                private void Handle(Func<T, T> handler)
                {
                    for (int i = 0; i < _list.Count; i++)
                    {
                        _list[i] = handler(_list[i]);
                    }
                }
            }

            private class HandleIDictionaryItems<TKey, TValue> : IHandleItems
            {
                private readonly IDictionary<TKey, TValue> _dict;
                public HandleIDictionaryItems(IDictionary<TKey, TValue> dict)
                {
                    _dict = dict;
                }
                public void Handle(Func<object, object> handler)
                {
                    Handle(s => (TValue)handler(s));
                }

                private void Handle(Func<TValue, TValue> handler)
                {
                    foreach (var key in _dict.Keys.ToList())
                    {
                        _dict[key] = handler(_dict[key]);
                    }
                }
            }
        }
    }
}
