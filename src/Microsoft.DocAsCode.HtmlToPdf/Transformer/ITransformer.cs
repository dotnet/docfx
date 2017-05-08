// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.HtmlToPdf.Transformer
{
    using System.Collections.Generic;

    public interface ITransformer
    {
        void Transform(IEnumerable<string> htmlFilePaths);
    }
}
