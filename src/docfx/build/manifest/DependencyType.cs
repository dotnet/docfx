// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    public enum DependencyType
    {
        File = 1, // file reference
        Uid = 2, // uid reference
        Toc = 3, // toc reference
        Inclusion = 4, // token or codesnippet
        Overwrite = 5, // overwrite markdown reference
    }

    public enum ImpactType
    {
        Content, // the changing file content has impact to the referenced file(s), like token/codesnippet, overwrite markdown
        URL, // the changing file output URL has impact to the referenced file(s), like file link, UID reference
    }

    public static class DependencyTypeExtensions
    {
        public static ImpactType ToImpact(this DependencyType dependencyType)
        {
            if ((int)dependencyType <= 3)
            {
                return ImpactType.URL;
            }

            return ImpactType.Content;
        }
    }

}
