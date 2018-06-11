// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class LegacyExtensions
    {
        public static string ToLegacyPathRelativeToBasePath(this Document doc, Docset docset)
        {
            return PathUtility.NormalizeFile(Path.GetRelativePath(docset.Config.SourceBasePath ?? string.Empty, doc.FilePath));
        }

        public static LegacyDependencyMapType ToLegacyDependencyMapType(this DependencyType dependencyType)
        {
            switch (dependencyType)
            {
                case DependencyType.Link:
                    return LegacyDependencyMapType.File;
                case DependencyType.Inclusion:
                    return LegacyDependencyMapType.Include;
                case DependencyType.LinkWithBookmark:
                    return LegacyDependencyMapType.Bookmark;
                default:
                    return LegacyDependencyMapType.None;
            }
        }
    }
}
