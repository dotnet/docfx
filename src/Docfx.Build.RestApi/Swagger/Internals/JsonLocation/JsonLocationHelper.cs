// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

using Newtonsoft.Json.Linq;

namespace Docfx.Build.RestApi.Swagger.Internals;

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

        StringBuilder sb = new();
        foreach (var state in locations)
        {
            state.WriteTo(sb);
        }

        return sb.ToString();
    }
}
