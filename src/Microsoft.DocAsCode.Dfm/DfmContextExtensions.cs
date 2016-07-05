// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.MarkdownLite;

    public static class DfmContextExtensions
    {
        private const string BaseFolderKey = "BaseFolder";
        private const string FilePathStackKey = "FilePathStack";
        private const string DependencyKey = "Dependency";

        public static ImmutableStack<string> GetFilePathStack(this IMarkdownContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            return (ImmutableStack<string>)context.Variables[FilePathStackKey]; ;
        }

        public static IMarkdownContext SetFilePathStack(this IMarkdownContext context, ImmutableStack<string> filePathStack)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            return context.CreateContext(context.Variables.SetItem(FilePathStackKey, filePathStack));
        }

        public static string GetBaseFolder(this IMarkdownContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            return (string)context.Variables[BaseFolderKey]; ;
        }

        public static IMarkdownContext SetBaseFolder(this IMarkdownContext context, string baseFolder)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            return context.CreateContext(context.Variables.SetItem(BaseFolderKey, baseFolder));
        }

        public static IMarkdownContext SetDependency(this IMarkdownContext context, HashSet<string> dependency)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            return context.CreateContext(context.Variables.SetItem(DependencyKey, dependency));
        }

        public static void ReportDependency(this IMarkdownContext context, string file)
        {
            var dependency = (HashSet<string>)context.Variables[DependencyKey];
            if (dependency != null)
            {
                dependency.Add(file);
            }
        }
    }
}
