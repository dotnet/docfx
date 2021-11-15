// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Diagnostics;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    /// <summary>
    /// Copies doc comments to items marked with 'inheritdoc' from interfaces and base classes.
    /// </summary>
    public class CopyInherited : IResolverPipeline
    {
        public void Run(MetadataModel yaml, ResolverContext context)
        {
            TreeIterator.Preorder(
                yaml.TocYamlViewModel,
                null,
                s => s.IsInvalid ? null : s.Items,
                (current, parent) =>
                {
                    if (current.InheritDoc != null)
                    {
                        InheritDoc(current, context);
                        current.InheritDoc = null;
                    }
                    return true;
                });
        }

        private static void InheritDoc(MetadataItem dest, ResolverContext context)
        {
            switch (dest.Type)
            {
                case MemberType.Constructor:
                    if (dest.Parent == null || dest.Syntax == null || dest.Syntax.Parameters == null)
                    {
                        return;
                    }
                    Debug.Assert(dest.Parent.Type == MemberType.Class);

                    //try to find the base class
                    if (dest.Parent.Inheritance == null || dest.Parent.Inheritance.Count == 0)
                    {
                        return;
                    }
                    if (!context.Members.TryGetValue(dest.Parent.Inheritance[dest.Parent.Inheritance.Count - 1], out MetadataItem baseClass))
                    {
                        return;
                    }
                    if (baseClass.Items == null)
                    {
                        return;
                    }

                    //look a constructor in the base class which has a matching signature
                    foreach (var ctor in baseClass.Items)
                    {
                        if (ctor.Type != MemberType.Constructor)
                        {
                            continue;
                        }
                        if (ctor.Syntax == null || ctor.Syntax.Parameters == null)
                        {
                            continue;
                        }
                        if (ctor.Syntax.Parameters.Count != dest.Syntax.Parameters.Count)
                        {
                            continue;
                        }

                        bool parametersMatch = true;
                        for (int ndx = 0; ndx < dest.Syntax.Parameters.Count; ndx++)
                        {
                            var myParam = dest.Syntax.Parameters[ndx];
                            var baseParam = ctor.Syntax.Parameters[ndx];
                            if (myParam.Name != baseParam.Name)
                            {
                                parametersMatch = false;
                            }
                            if (myParam.Type != baseParam.Type)
                            {
                                parametersMatch = false;
                            }
                        }

                        if (parametersMatch)
                        {
                            Copy(dest, ctor, context);
                            return;
                        }
                    }
                    break;

                case MemberType.Method:
                case MemberType.Property:
                case MemberType.Event:
                    Copy(dest, dest.Overridden, context);
                    if (dest.Implements != null)
                    {
                        foreach (var item in dest.Implements)
                        {
                            Copy(dest, item, context);
                        }
                    }
                    break;

                case MemberType.Class:
                    if (dest.Inheritance.Count != 0)
                    {
                        Copy(dest, dest.Inheritance[dest.Inheritance.Count - 1], context);
                    }
                    if (dest.Implements != null)
                    {
                        foreach (var item in dest.Implements)
                        {
                            Copy(dest, item, context);
                        }
                    }
                    break;
            }
        }

        private static void Copy(MetadataItem dest, string srcName, ResolverContext context)
        {
            MetadataItem src = null;

            // An explicit <inheritdoc/> (i.e. cref) overrides the default behavior
            if (!string.IsNullOrEmpty(dest.InheritDoc) && context.Members.TryGetValue(dest.InheritDoc, out src) && dest != src)
            {
                srcName = dest.InheritDoc;
            }

            if (string.IsNullOrEmpty(srcName))
                return;

            if (src == null && !context.Members.TryGetValue(srcName, out src))
            {
                // Try to resolve any templated references before giving up
                if (!context.References.TryGetValue(srcName, out var referenceItem) || referenceItem.Definition == null ||
                    !context.Members.TryGetValue(referenceItem.Definition, out src))
                {
                    Logger.LogWarning($"Could not resolve base documentation for '{dest.Name}'",
                                      file: dest.Source.Path,
                                      line: dest.Source.StartLine != 0 ? dest.Source.StartLine.ToString() : null);
                    return;
                }
                    
            }

            Copy(dest, src, context);
        }

        private static void Copy(MetadataItem dest, MetadataItem src, ResolverContext context)
        {
            if (src.InheritDoc != null)
            {
                InheritDoc(src, context);
                src.InheritDoc = null;
            }

            dest.CopyInheritedData(src);
            dest.InheritDoc = null;
        }
    }
}
