﻿// Copyright (c) Microsoft. All rights reserved.
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
            return _cache.GetOrAdd(type, t => new MarkdownContentHandlerImpl(t, this)).Handle(obj, context);
        }

        private sealed class MarkdownContentHandlerImpl : BaseModelAttributeHandler<MarkdownContentAttribute>
        {
            private const string ContentPlaceholder = "*content";

            public MarkdownContentHandlerImpl(Type type, IModelAttributeHandler handler) : base(type, handler)
            {
            }

            protected override bool ShouldHandle(object currentObj, object declaringObject, PropInfo currentPropInfo, HandleModelAttributesContext context)
            {
                if (context.EnableContentPlaceholder)
                {
                    var str = currentObj as string;
                    if (IsPlaceholderContent(str))
                    {
                        return true;
                    }
                }
                return base.ShouldHandle(currentObj, declaringObject, currentPropInfo, context);
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
                    var marked = Markup(val, context);
                    if (currentPropertyInfo != null)
                    {
                        ReflectionHelper.SetPropertyValue(declaringObject, currentPropertyInfo, marked);
                    }
                    return marked;
                }
                else
                {
                    throw new NotSupportedException($"Type {currentObj.GetType()} is NOT a supported type for {nameof(MarkdownContentAttribute)}");
                }
            }

            protected override object HandleDictionaryType(object currentObj, HandleModelAttributesContext context)
            {
                Func<object, object> handler = s => Handler.Handle(s, context);
                HandleGenericItemsHelper.HandleIDictionary(currentObj, s => Handler.Handle(s, context));
                return currentObj;
            }

            protected override object HandleIEnumerableType(object currentObj, HandleModelAttributesContext context)
            {
                Func<object, object> handler = s => Handler.Handle(s, context);
                HandleGenericItemsHelper.HandleIList(currentObj, s => Handler.Handle(s, context));
                return currentObj;
            }

            protected override IEnumerable<PropInfo> GetProps(Type type)
            {
                return from prop in base.GetProps(type)
                       where prop.Prop.GetSetMethod() != null
                       where !prop.Prop.IsDefined(typeof(MarkdownContentIgnoreAttribute), false)
                       select prop;
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
        }
    }
}
