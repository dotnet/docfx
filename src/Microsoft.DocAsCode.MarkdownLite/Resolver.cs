// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    public interface IResolverContext { }

    public class Resolver<TInput> : IResolver where TInput: class
    {
        public delegate X FuncWithRef<T, U, V, W, out X>(T input1, U input2, ref V input3, ref W input4);
        public Regex Regex { get; set; }
        public FuncWithRef<Match, IResolverContext, string, TInput, bool> Renderer { get; set; }
        public Func<IResolverContext, bool> Filter { get; set; }
        public string Name { get; private set; }

        public Resolver(string name, Regex regex, FuncWithRef<Match, IResolverContext, string, TInput, bool> renderer, Func<IResolverContext, bool> filter = null)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            Name = name;
            Regex = regex;
            Renderer = renderer;
            Filter = filter;
        }

        public bool Apply(ref string src, ref TInput result, IResolverContext context)
        {
            if (Regex == null || Renderer == null || src == null || result == null) return false;
            if (Filter != null && !Filter(context)) return false;
            var match = Regex.Match(src);
            if (!match.Success || match.Groups.Count == 0)
            {
                return false;
            }

            src = src.Substring(match.Groups[0].Value.Length);

            return Renderer(match, context, ref src, ref result);
        }

        public override string ToString()
        {
            return Name.ToString();
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (object.ReferenceEquals(this, obj)) return true;
            var target = obj as Resolver<TInput>;
            if (target == null) return false;
            return object.Equals(this.Name, target.Name);
        }
    }
}
