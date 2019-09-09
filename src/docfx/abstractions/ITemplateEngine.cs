// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal interface ITemplateEngine
    {
        bool IsPage(string schemaName);

        bool TryGetLocalizedToken(string token, string localizedToken);

        JsonSchema GetJsonSchema(SourceInfo<string> schemaName);

        string RunLiquid(string fileName, TemplateModel model);

        string RunMustache(string fileName, JToken model);

        JToken RunJint(string fileName, JToken model);
    }
}
