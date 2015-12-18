// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.MarkdownLite;

    public static class DfmContextExtensions
    {
        private const string FilePathStackKey = "FilePathStack";

        public static ImmutableStack<string> GetFilePathStack(this IMarkdownContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            return (ImmutableStack<string>)context.Variables[DfmEngine.FilePathStackKey]; ;
        }

        public static IMarkdownContext SetFilePathStack(this IMarkdownContext context, ImmutableStack<string> filePathStack)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            return context.CreateContext(context.Variables.SetItem(DfmEngine.FilePathStackKey, filePathStack));
        }
    }
}
