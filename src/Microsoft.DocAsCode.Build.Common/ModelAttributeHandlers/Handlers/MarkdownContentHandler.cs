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

    using Microsoft.DocAsCode.DataContracts.Common.Attributes;
    using Microsoft.DocAsCode.Plugins;

    public class MarkdownContentHandler : IModelAttributeHandler
    {
        private readonly ConcurrentDictionary<Type, MarkdownContentHandlerImpl> _cache = new ConcurrentDictionary<Type, MarkdownContentHandlerImpl>();

        public void Handle(object obj, HandleModelAttributesContext context)
        {
            if (obj == null)
            {
                return;
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.host == null)
            {
                throw new ArgumentNullException(nameof(context.host));
            }

            var type = obj.GetType();
            _cache.GetOrAdd(type, new MarkdownContentHandlerImpl(type, this)).Handle(obj, context);
        }

        private sealed class MarkdownContentHandlerImpl : BaseModelAttributeHandler<MarkdownContentAttribute>
        {
            private const string ContentPlaceholder = "*content";

            public MarkdownContentHandlerImpl(Type type, IModelAttributeHandler handler) : base(type, handler)
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
                    var marked = Markup(val, context);
                    currentPropertyInfo.SetValue(declaringObject, marked);
                }
                else
                {
                    throw new NotSupportedException($"Type {obj.GetType()} is NOT a supported type for {nameof(MarkdownContentAttribute)}");
                }
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

            protected override bool ShouldHandle(PropInfo currentPropInfo, object declaringObject, HandleModelAttributesContext context)
            {
                if (context.SkipMarkup)
                {
                    return false;
                }

                // MarkdownContent will be marked and set back to the property, so the property type must be assignable from string
                if (!currentPropInfo.Prop.PropertyType.IsAssignableFrom(typeof(string)))
                {
                    if (currentPropInfo.Attr != null)
                    {
                        throw new NotSupportedException($"Type {declaringObject.GetType()} is NOT a supported type for {nameof(MarkdownContentAttribute)}");
                    }

                    return false;
                }

                if (context.EnableContentPlaceholder)
                {
                    var currentValue = currentPropInfo.Prop.GetValue(declaringObject) as string;
                    if (currentValue != null && IsPlaceholderContent(currentValue))
                    {
                        return true;
                    }
                }

                return base.ShouldHandle(currentPropInfo, declaringObject, context);
            }

            private bool IsPlaceholderContent(string content)
            {
                return content.Trim() == ContentPlaceholder;
            }

            private string Markup(string content, HandleModelAttributesContext context)
            {
                if (string.IsNullOrEmpty(content))
                {
                    return content;
                }

                if (context.EnableContentPlaceholder && IsPlaceholderContent(content))
                {
                    if (string.IsNullOrEmpty(context.PlaceholderContent))
                    {
                        return context.PlaceholderContent;
                    }
                    else
                    {
                        // Not using cache considering: multiple *content is not common condition
                        // Key should be context.FileAndType & context.PlaceholderContent, context.PlaceholderContent can be long
                        return MarkupCore(context.PlaceholderContent, context);
                    }
                }

                return MarkupCore(content, context);
            }

            private string MarkupCore(string content, HandleModelAttributesContext context)
            {
                var host = context.host;
                var mr = host.Markup(content, context.FileAndType);
                context.LinkToUids.UnionWith(mr.LinkToUids);
                context.LinkToFiles.UnionWith(mr.LinkToFiles);
                AddRange(context.FileLinkSources, mr.FileLinkSources);
                AddRange(context.UidLinkSources, mr.UidLinkSources);
                return mr.Html;
            }

            private static void AddRange(Dictionary<string, List<LinkSourceInfo>> left, ImmutableDictionary<string, ImmutableList<LinkSourceInfo>> right)
            {
                foreach (var pair in right)
                {
                    List<LinkSourceInfo> list;
                    if (left.TryGetValue(pair.Key, out list))
                    {
                        list.AddRange(pair.Value);
                    }
                    else
                    {
                        left[pair.Key] = pair.Value.ToList();
                    }
                }
            }
        }
    }
}
