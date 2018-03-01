// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    public enum ChangeKindWithDependency
    {
        None = ChangeKind.None,
        Created = ChangeKind.Created,
        Updated = ChangeKind.Updated,
        Deleted = ChangeKind.Deleted,
        DependencyUpdated = 1024,
    }
}
