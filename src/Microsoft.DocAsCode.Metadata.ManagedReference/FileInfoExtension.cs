// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.IO;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    internal static class FileInfoExtension
    {
        public static bool IsSupportedProject(this FileInfo file)
        {
            return file.Type == FileType.Project || file.Type == FileType.ProjectJsonProject;
        }
    }
}
