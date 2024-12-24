// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Reflection;

using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.Plugins;

namespace Docfx.Build.Common;

public class UrlContentHandler : IModelAttributeHandler
{
    private readonly ConcurrentDictionary<Type, UrlContentHandlerImpl> _cache = new();

    public object Handle(object obj, HandleModelAttributesContext context)
    {
        if (obj == null)
        {
            return null;
        }

        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Host);

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

        private static string GetHrefFromRoot(string originalHref, HandleModelAttributesContext context)
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
                result = file.UrlEncode() + UriUtility.GetQueryStringAndFragment(originalHref);
            }

            if (!context.FileLinkSources.TryGetValue(file, out List<LinkSourceInfo> sources))
            {
                sources = [];
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
