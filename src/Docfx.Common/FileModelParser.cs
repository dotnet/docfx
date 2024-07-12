// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Docfx;

internal static class FileModelParser
{
    public static FileMappingItem ParseItem(JToken item)
    {
        if (item.Type == JTokenType.Object)
        {
            return JsonConvert.DeserializeObject<FileMappingItem>(item.ToString());
        }
        else if (item.Type == JTokenType.Property)
        {
            JProperty jProperty = (JProperty)item;
            FileMappingItem model = new() { Name = jProperty.Name };
            var value = jProperty.Value;
            if (value.Type == JTokenType.Array)
            {
                model.Files = new FileItems(value.Select(s => s.Value<string>()));
            }
            else if (value.Type == JTokenType.String)
            {
                model.Files = new FileItems((string)value);
            }
            else
            {
                throw new JsonReaderException($"Unsupported value {value} (type: {value.Type}).");
            }

            return model;
        }
        else if (item.Type == JTokenType.String)
        {
            return new FileMappingItem { Files = new FileItems(item.Value<string>()) };
        }
        else
        {
            throw new JsonReaderException($"Unsupported value {item} (type: {item.Type}).");
        }
    }
}
