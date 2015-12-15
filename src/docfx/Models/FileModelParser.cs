// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System.Linq;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

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
                JProperty jProperty = item as JProperty;
                FileMappingItem model = new FileMappingItem { Name = jProperty.Name };
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
                    throw new JsonReaderException(string.Format("Unsupported value {0} (type: {1}).", value, value.Type));
                }

                return model;
            }
            else if (item.Type == JTokenType.String)
            {
                return new FileMappingItem { Files = new FileItems(item.Value<string>()) };
            }
            else
            {
                throw new JsonReaderException(string.Format("Unsupported value {0} (type: {1}).", item, item.Type));
            }
        }
    }
}
