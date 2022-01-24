// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class ErrorList : ErrorBuilder
{
    private readonly List<Error> _items = new();

    public override bool HasError
    {
        get
        {
            lock (_items)
            {
                return _items.Any(item => item.Level == ErrorLevel.Error);
            }
        }
    }

    public override void Add(Error error)
    {
        lock (_items)
        {
            _items.Add(error);
        }
    }

    public override bool FileHasError(FilePath file) => throw new NotSupportedException();

    public Error[] ToArray()
    {
        lock (_items)
        {
            return _items.ToArray();
        }
    }
}
