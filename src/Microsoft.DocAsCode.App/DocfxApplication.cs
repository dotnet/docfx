// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.DocAsCode
{
    /// <summary>
    /// A docfx project provides access to a set of documentations
    /// and their associated configs, compilations and models.
    /// </summary>
    public abstract class DocfxProject : IDisposable
    {
        /// <summary>
        /// Loads a docfx project from docfx.json.
        /// </summary>
        /// <param name="configPath">The path to docfx.json config file.</param>
        /// <returns>The created docfx project.</returns>
        public static DocfxProject Load(string configPath)
        {
            throw new NotImplementedException();
        }

        public abstract Task Build();

        public abstract void Dispose();
    }
}
