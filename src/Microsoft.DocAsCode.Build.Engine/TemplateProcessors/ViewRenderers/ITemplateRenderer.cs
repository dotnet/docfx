// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;

    public interface ITemplateRenderer
    {
        string Render(object model);

        IEnumerable<string> Dependencies { get; }

        string Raw { get; }

        string Path { get; }

        string Name { get; }
    }
}
