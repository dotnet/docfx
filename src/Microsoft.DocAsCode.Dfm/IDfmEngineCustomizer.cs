// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Generic;

    public interface IDfmEngineCustomizer
    {
        /// <summary>
        /// Customize the <see cref="DfmEngineBuilder"/>.
        /// </summary>
        /// <param name="builder">The instance of <see cref="DfmEngineBuilder"/> to customize.</param>
        /// <param name="parameters">The markdown engine parameters.</param>
        void Customize(DfmEngineBuilder builder, IReadOnlyDictionary<string, object> parameters);
    }
}
