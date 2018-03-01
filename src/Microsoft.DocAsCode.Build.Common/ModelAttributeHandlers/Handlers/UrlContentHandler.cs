// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;

    public class UrlContentHandler : IModelAttributeHandler
    {
        private readonly ConcurrentDictionary<Type, UrlContentHandlerImpl> _cache = new ConcurrentDictionary<Type, UrlContentHandlerImpl>();

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

            var type = obj.GetType();
            return _cache.GetOrAdd(type, t => new UrlContentHandlerImpl(t, this)).Handle(obj, context);
        }

        private sealed class UrlContentHandlerImpl : BaseModelAttributeHandler<UrlContentAttribute>
        {
            public UrlContentHandlerImpl(Type type, IModelAttributeHandler handler) : base(type, handler)
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
                    var updated = GetHrefFromRoot(val, context);
                    if (currentPropertyInfo != null)
                    {
                        ReflectionHelper.SetPropertyValue(declaringObject, currentPropertyInfo, updated);
                    }
                    return updated;
                }

                if (currentObj is IList<string> list)
                {
                    for (var i = 0; i < list.Count; i++)
                    {
                        var item = list[i];
                        if (item != null)
                        {
                            list[i] = GetHrefFromRoot(item, context);
                        }
                    }
                    return list;
                }

                throw new NotSupportedException($"Type {currentObj.GetType()} is NOT a supported type for {nameof(UrlContentAttribute)}");
            }

            protected override IEnumerable<PropInfo> GetProps(Type type)
            {
                return from prop in base.GetProps(type)
                       where prop.Prop.GetSetMethod() != null
                       where !prop.Prop.IsDefined(typeof(UrlContentIgnoreAttribute), false)
                       select prop;
            }

            private string GetHrefFromRoot(string originalHref, HandleModelAttributesContext context)
            {
                if (context.FileAndType == null || string.IsNullOrEmpty(originalHref) || !RelativePath.IsRelativePath(originalHref))
                {
                    return originalHref;
                }

                var result = originalHref;
                var ft = context.FileAndType;
                var path = (RelativePath)ft.File + (RelativePath)UriUtility.GetPath(originalHref);
                var file = path.GetPathFromWorkingFolder().UrlDecode();
                if (context.Host.SourceFiles.ContainsKey(file))
                {
                    result = file.UrlEncode().ToString() + UriUtility.GetQueryStringAndFragment(originalHref);
                }

                if (!context.FileLinkSources.TryGetValue(file, out List<LinkSourceInfo> sources))
                {
                    sources = new List<LinkSourceInfo>();
                    context.FileLinkSources[file] = sources;
                }
                sources.Add(new LinkSourceInfo
                {
                    Target = file,
                    Anchor = UriUtility.GetFragment(originalHref),
                    SourceFile = ft.File,
                });
                context.LinkToFiles.Add(file);

                return result;
            }
        }
    }
}
