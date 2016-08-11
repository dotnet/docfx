// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;

    public class ProcessorInfo
    {
        /// <summary>
        /// The name of processor.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The context hash for incremental.
        /// </summary>
        public string IncrementalContextHash { get; set; }
        /// <summary>
        /// The information for steps.
        /// </summary>
        public List<ProcessorStepInfo> Steps { get; } = new List<ProcessorStepInfo>();
    }
}
