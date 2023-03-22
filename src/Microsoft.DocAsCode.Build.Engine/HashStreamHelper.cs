// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Common;

namespace Microsoft.DocAsCode.Build.Engine;

internal static class HashStreamHelper
{
    public static Stream WithSha256Hash(this Stream stream, out Task<byte[]> hashTask)
    {
        var cs = new CircularStream();
        hashTask = Task.Run(() =>
        {
            using var csr = cs.CreateReaderView();
            return HashUtility.GetSha256Hash(csr);
        });
        return new CompositeStream(stream, cs.CreateWriterView());
    }
}
