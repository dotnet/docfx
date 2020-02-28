// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using DotLiquid;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal class LocalizeTag : Tag
    {
        public override void Render(DotLiquid.Context context, TextWriter result)
        {
            var id = Markup.Trim();
            var localizedStrings = (IReadOnlyDictionary<string, string>)context.Registers["localized_strings"];
            result.Write(localizedStrings.TryGetValue(id, out var value) ? value : id);
        }
    }
}
