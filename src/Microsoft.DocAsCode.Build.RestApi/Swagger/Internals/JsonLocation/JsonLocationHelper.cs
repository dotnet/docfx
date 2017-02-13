// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Swagger.Internals
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Newtonsoft.Json.Linq;

    internal class JsonLocationHelper
    {
        public static string GetLocation(JToken token)
        {
            if (token.Parent == null)
            {
                return "#";
            }

            IList<JToken> ancestors = token.AncestorsAndSelf().Reverse().ToList();

            var locations = new List<IJsonLocation>();
            for (int i = 0; i < ancestors.Count; i++)
            {
                JToken current = ancestors[i];
                switch (current.Type)
                {
                    case JTokenType.Property:
                        JProperty property = (JProperty)current;
                        locations.Add(new JsonObjectLocation(property.Name));
                        break;
                    case JTokenType.Array:
                    case JTokenType.Constructor:
                        if (i < ancestors.Count - 1)
                        {
                            var next = ancestors[i + 1];
                            int index = ((IList<JToken>)current).IndexOf(next);
                            locations.Add(new JsonIndexLocation(index));
                        }
                        break;
                }
            }

            StringBuilder sb = new StringBuilder();
            foreach (var state in locations)
            {
                state.WriteTo(sb);
            }

            return sb.ToString();
        }
    }
}
