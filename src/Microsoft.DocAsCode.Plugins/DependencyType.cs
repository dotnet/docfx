// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    public class DependencyType
    {
        public string Name { get; set; }

        public bool IsTransitive { get; set; }

        public bool TriggerBuild { get; set; }
    }
}
