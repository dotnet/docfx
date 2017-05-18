// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.DocAsCode.MarkdownLite;

    public static class DfmContextExtensions
    {
        private const string BaseFolderKey = "BaseFolder";
        private const string FilePathStackKey = "FilePathStack";
        private const string DependencyKey = "Dependency";
        private const string IsIncludeKey = "IsInclude";
        private const string IsInTableKey = "IsInTable";
        private const string FallbackFoldersKey = "FallbackFolders";

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
            return (string)context.Variables[BaseFolderKey];
        }

        public static IMarkdownContext SetBaseFolder(this IMarkdownContext context, string baseFolder)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            return context.CreateContext(context.Variables.SetItem(BaseFolderKey, baseFolder));
        }

        public static IReadOnlyList<string> GetFallbackFolders(this IMarkdownContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            return (IReadOnlyList<string>)(context.Variables[FallbackFoldersKey]) ?? new List<string>();
        }

        public static IMarkdownContext SetFallbackFolders(this IMarkdownContext context, IReadOnlyList<string> fallbackFolders)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return context.CreateContext(context.Variables.SetItem(FallbackFoldersKey, (IReadOnlyList<string>)fallbackFolders.Select(folder => folder.Replace('/', '\\')).ToList()));
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
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            var dependency = (HashSet<string>)context.Variables[DependencyKey];
            if (dependency != null)
            {
                dependency.Add(file);
            }
        }

        public static void ReportDependency(this IMarkdownContext context, IEnumerable<string> files)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (files == null)
            {
                throw new ArgumentNullException(nameof(files));
            }
            var dependency = (HashSet<string>)context.Variables[DependencyKey];
            if (dependency != null)
            {
                dependency.UnionWith(files);
            }
        }

        public static bool GetIsInclude(this IMarkdownContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            return context.Variables.ContainsKey(IsIncludeKey);
        }

        public static IMarkdownContext SetIsInclude(this IMarkdownContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            return context.CreateContext(context.Variables.SetItem(IsIncludeKey, null));
        }

        public static bool GetIsInTable(this IMarkdownContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            return context.Variables.ContainsKey(IsInTableKey);
        }

        public static IMarkdownContext SetIsInTable(this IMarkdownContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            return context.CreateContext(context.Variables.SetItem(IsInTableKey, null));
        }
    }
}
