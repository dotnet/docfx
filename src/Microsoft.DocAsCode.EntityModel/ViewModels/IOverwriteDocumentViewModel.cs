// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.ViewModels
{
    public interface IOverwriteDocumentViewModel
    {
        string Uid { get; set; }
        string Conceptual { get; set; }
        SourceDetail Documentation { get; set; }
    }
}
