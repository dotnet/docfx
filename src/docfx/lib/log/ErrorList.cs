// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class ErrorList : ErrorBuilder, IReadOnlyList<Error>
    {
        private readonly List<Error> _items = new();

        public Error this[int index] => _items[index];

        public int Count => _items.Count;

        public override bool HasError => _items.Any(item => item.Level == ErrorLevel.Error);

        public override void Add(Error error)
        {
            _items.Add(error);
            Console.WriteLine($"[ErrorList] Error {error} has been added, current count: {_items.Count}");
        }

        public override bool FileHasError(FilePath file) => throw new NotSupportedException();

        public IEnumerator<Error> GetEnumerator() => _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
    }
}
