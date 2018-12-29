// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class AttributeTransformer
    {
        private static ConcurrentStack<UidPropertyReference> s_recursionDetector = new ConcurrentStack<UidPropertyReference>();

        public static Func<IEnumerable<DataTypeAttribute>, object, string, object> TransformSDP(
            Context context,
            List<Error> errors,
            Document file,
            Action<Document> buildChild)
        {
            return Transform;

            object Transform(IEnumerable<DataTypeAttribute> attributes, object value, string jsonPath)
            {
                var attribute = attributes.SingleOrDefault(attr => !(attr is XrefPropertyAttribute));
                return TransformContent(context, errors, attribute, value, file, buildChild);
            }
        }

        public static Func<IEnumerable<DataTypeAttribute>, object, string, object> TransformXref(
            Context context,
            List<Error> errors,
            Document file,
            Action<Document> buildChild,
            Dictionary<string, Lazy<Func<string, string, Document, Document, JValue>>> extensionData)
        {
            return TransformXrefSpec;

            object TransformXrefSpec(IEnumerable<DataTypeAttribute> attributes, object value, string jsonPath)
            {
                var attribute = attributes.SingleOrDefault(attr => !(attr is XrefPropertyAttribute));
                extensionData[jsonPath] = new Lazy<Func<string, string, Document, Document, JValue>>(() => Load);
                return null;

                JValue Load(string propertyName, string uid, Document referencedFile, Document rootFile)
                {
                    try
                    {
                        if (referencedFile != null)
                        {
                            s_recursionDetector.Push(new UidPropertyReference(uid, propertyName, referencedFile, rootFile));
                            var groups = s_recursionDetector.Where(x => x.RootFile == rootFile).GroupBy(x => x, new UidPropertyReferenceComparer());
                            if (rootFile == referencedFile || groups.Any(group => group.Count() > 1))
                            {
                                var callStack = s_recursionDetector.Where(x => x.RootFile == rootFile).Select(x => x.ReferencedFile).ToList();
                                throw Errors.CircularReference(rootFile, callStack).ToException();
                            }
                        }
                        return new JValue(TransformContent(context, errors, attribute, value, file, buildChild));
                    }
                    finally
                    {
                        if (referencedFile != null)
                        {
                            s_recursionDetector.TryPop(out var result);
                        }
                    }
                }
            }
        }

        private static object TransformContent(Context context, List<Error> errors, DataTypeAttribute attribute, object value, Document file, Action<Document> buildChild)
        {
            if (attribute is HrefAttribute)
            {
                var (error, link, _) = context.DependencyResolver.ResolveLink((string)value, file, file, buildChild);
                errors.AddIfNotNull(error);
                return link;
            }

            if (attribute is MarkdownAttribute)
            {
                var (html, markup) = MarkdownUtility.ToHtml((string)value, file, context.DependencyResolver, buildChild, null, MarkdownPipelineType.Markdown);
                errors.AddRange(markup.Errors);
                return html;
            }

            if (attribute is InlineMarkdownAttribute)
            {
                var (html, markup) = MarkdownUtility.ToHtml((string)value, file, context.DependencyResolver, buildChild, null, MarkdownPipelineType.InlineMarkdown);
                errors.AddRange(markup.Errors);
                return html;
            }

            if (attribute is HtmlAttribute)
            {
                var html = HtmlUtility.TransformLinks((string)value, href =>
                {
                    var (error, link, _) = context.DependencyResolver.ResolveLink(href, file, file, buildChild);
                    errors.AddIfNotNull(error);
                    return link;
                });
                return HtmlUtility.StripTags(HtmlUtility.LoadHtml(html)).OuterHtml;
            }

            if (attribute is XrefAttribute)
            {
                // TODO: how to fill xref resolving data besides href
                var (error, link, _, _) = context.DependencyResolver.ResolveXref((string)value, file);
                errors.AddIfNotNull(error);
                return link;
            }

            return value;
        }

        private sealed class UidPropertyReference
        {
            public string Uid { get; set; }

            public string PropertyName { get; set; }

            public Document ReferencedFile { get; set; }

            public Document RootFile { get; set; }

            public UidPropertyReference(string uid, string propertyName, Document referencedFile, Document rootFile)
            {
                Uid = uid;
                PropertyName = propertyName;
                ReferencedFile = referencedFile;
                RootFile = rootFile;
            }
        }

        private class UidPropertyReferenceComparer : IEqualityComparer<UidPropertyReference>
        {
            public bool Equals(UidPropertyReference x, UidPropertyReference y)
                => x.Uid == y.Uid && x.PropertyName == y.PropertyName && x.ReferencedFile == y.ReferencedFile && x.RootFile == y.RootFile;

            public int GetHashCode(UidPropertyReference obj)
                => HashCode.Combine(obj.Uid, obj.PropertyName, obj.ReferencedFile, obj.RootFile);
        }
    }
}
