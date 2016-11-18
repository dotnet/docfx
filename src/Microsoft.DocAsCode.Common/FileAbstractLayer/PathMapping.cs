// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.FileAbstractLayer
{
    using System;
    using System.Collections.Immutable;

    public struct PathMapping
    {
        public PathMapping(RelativePath logicPath, string physicPath)
        {
            if (logicPath == null)
            {
                throw new ArgumentNullException(nameof(logicPath));
            }
            if (physicPath == null)
            {
                throw new ArgumentNullException(nameof(physicPath));
            }
            LogicPath = logicPath.GetPathFromWorkingFolder();
            PhysicPath = physicPath;
            AllowMoveOut = false;
            Properties = ImmutableDictionary<string, string>.Empty;
        }

        public RelativePath LogicPath { get; }
        public string PhysicPath { get; }

        public bool IsFolder => LogicPath.FileName == string.Empty;
        public bool AllowMoveOut { get; set; }
        public ImmutableDictionary<string, string> Properties { get; set; }
    }
}
