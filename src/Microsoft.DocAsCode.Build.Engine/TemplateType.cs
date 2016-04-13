// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    public enum TemplateType
    {
        Default,

        /// <summary>
        /// Primary template type means documents processed by this template will be responsible for hyperlink
        /// </summary>
        Primary,

        /// <summary>
        /// Auxiliary template type means documents processed by this template will not be referenced by other documents
        /// </summary>
        Auxiliary,
    }
}
