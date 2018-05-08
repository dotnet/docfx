// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;

    using Markdig.Extensions.Yaml;
    using Markdig.Renderers;
    using Markdig.Renderers.Html;
    using Microsoft.DocAsCode.Common;

    public class YamlHeaderRenderer : HtmlObjectRenderer<YamlFrontMatterBlock>
    {
        protected override void Write(HtmlRenderer renderer, YamlFrontMatterBlock obj)
        {
            if (InclusionContext.IsInclude)
            {
                return;
            }

            var content = obj.Lines.ToString();

            try
            {
                using (StringReader reader = new StringReader(content))
                {
                    var result = YamlUtility.Deserialize<Dictionary<string, object>>(reader);
                    if (result != null)
                    {
                        renderer.Write("<yamlheader").Write($" start=\"{obj.Line + 1}\" end=\"{obj.Line + obj.Lines.Count + 2}\"");
                        renderer.WriteAttributes(obj).Write(">");
                        renderer.Write(WebUtility.HtmlEncode(obj.Lines.ToString()));
                        renderer.Write("</yamlheader>");
                    }
                }
            }
            catch (Exception)
            {
                // not a valid ymlheader, do nothing
                Logger.LogWarning("Invalid YamlHeader, ignored");
            }
        }
    }
}
