// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;

    internal static class Extensions
    {
        public static IEnumerable<XmlReader> Elements(this XmlReader reader, string name)
        {
            reader.Read();
            while (reader.ReadToNextSibling(name))
            {
                using (var result = reader.ReadSubtree())
                {
                    result.Read();
                    yield return result;
                }
            }
        }

        public static IEnumerable<T> ProtectResource<T>(this IEnumerable<T> source)
            where T : IDisposable
        {
            foreach (var item in source)
            {
                using (item)
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<T> EmptyIfThrow<T>(this Func<T> func)
        {
            try
            {
                return new[] { func() };
            }
            catch (Exception)
            {
                return Enumerable.Empty<T>();
            }
        }
    }
}
