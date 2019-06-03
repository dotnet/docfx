// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class CommandLineOptions
    {
        public string Output;
        public bool Legacy;
        public bool Verbose;
        public string Locale;
        public string Template;
        public int Port;

        public JObject ToJObject()
        {
            var output = new JObject();
            if (Output != null)
                output["path"] = Output;

            if (Legacy)
            {
                output["json"] = true;
                output["copyResources"] = false;
            }

            var result = new JObject();
            result["output"] = output;

            if (Template != null)
                result["template"] = Template;

            return result;
        }
    }
}
