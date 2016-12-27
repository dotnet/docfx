// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Reflection;

    using Microsoft.DocAsCode.DataContracts.Common.Attributes;
    using System.Linq;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Plugins;

    public class MarkdownContentHandler : IModelAttributeHandler
    {
        private readonly static ConcurrentDictionary<Type, MarkdownContentHandlerImpl> _cache = new ConcurrentDictionary<Type, MarkdownContentHandlerImpl>();

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
            private string placeholderContentAfterMarkup = null;

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

                return currentPropInfo.Attr != null;
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

                if (context.EnableContentPlaceholder)
                {
                    if (IsPlaceholderContent(content))
                    {
                        if (string.IsNullOrEmpty(context.PlaceholderContent))
                        {
                            return context.PlaceholderContent;
                        }
                        else
                        {
                            if (placeholderContentAfterMarkup == null)
                            {
                                placeholderContentAfterMarkup = MarkupCore(context.PlaceholderContent, context);
                            }

                            return placeholderContentAfterMarkup;
                        }
                    }
                }

                return MarkupCore(content, context);
            }

            private string MarkupCore(string content, HandleModelAttributesContext context)
            {
                var host = context.host;
                var mr = host.Markup(content, context.FileAndType);
                context.LinkToUids.AddRange(mr.LinkToUids);
                AddRange(context.LinkToFiles, mr.LinkToFiles);
                AddRange(context.FileLinkSources, mr.FileLinkSources);
                AddRange(context.UidLinkSources, mr.UidLinkSources);
                return mr.Html;
            }

            private static void AddRange(HashSet<string> left, IEnumerable<string> right)
            {
                if (right == null)
                {
                    return;
                }
                foreach (var i in right)
                {
                    left.Add(i);
                }
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
