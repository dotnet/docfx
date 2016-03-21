// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;

    [Flags]
    public enum ApplyTemplateOptions
    {
        None = 0,
        ExportRawModel = 1,
        ExportViewModel = 2,
        TransformDocument = 4,
        All = ExportRawModel | ExportViewModel | TransformDocument
    }
}
