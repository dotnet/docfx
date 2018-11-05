// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class LegacySchema
    {
        public static JObject Transform(JObject metadata, PageModel model)
        {
            switch (model.PageType)
            {
                case "LandingData":
                    metadata["_op_layout"] = "LandingPage";
                    metadata["layout"] = "LandingPage";
                    metadata["page_type"] = "landingdata";

                    metadata.Remove("_op_gitContributorInformation");
                    metadata.Remove("_op_allContributorsStr");
                    break;

                case "Conceptual":
                case "ContextObject":
                    break;

                default:
                    throw new NotImplementedException($"Unknown page type {model.PageType}");
            }

            return metadata;
        }
    }
}
