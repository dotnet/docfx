// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.Docs.Build
{
    internal static class MarkdownInclusionStack
    {
        private static readonly ThreadLocal<Stack<(Document file, string uid, string xrefPropertyName)>> t_stack
                          = new ThreadLocal<Stack<(Document file, string uid, string xrefPropertyName)>>(
                                  () => new Stack<(Document file, string uid, string xrefPropertyName)>());

        public static StackMarker Push(Document file, string uid = null, string xrefPropertyName = null)
        {
            var entry = (file, uid, xrefPropertyName);
            var stack = t_stack.Value;

            if (stack.Contains(entry))
            {
                var textStack = stack.Select(
                    item => item.uid == null ? item.file.ToString() : $"{item.file} ({item.uid}/{item.xrefPropertyName})");

                throw Errors.CircularReference(textStack).ToException();
            }

            stack.Push((file, uid, xrefPropertyName));
            return default;
        }

        public struct StackMarker : IDisposable
        {
            public void Dispose()
            {
                t_stack.Value.Pop();
            }
        }
    }
}
