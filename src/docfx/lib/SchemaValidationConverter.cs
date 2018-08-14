// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class SchemaValidationConverter : JsonConverter
    {
        private readonly IEnumerable<ValidationAttribute> _validators;
        private readonly List<Error> _errors;

        public SchemaValidationConverter(IEnumerable<ValidationAttribute> validators, List<Error> errors)
        {
            _validators = validators;
            _errors = errors;
        }

        public override bool CanConvert(Type objectType) => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.StartObject:
                    var obj = JObject.Load(reader);
                    Validate(reader, obj);
                    break;
                case JsonToken.StartArray:
                    var array = JArray.Load(reader);
                    Validate(reader, array);
                    break;
                case JsonToken.StartConstructor:
                    var constructor = JConstructor.Load(reader);
                    Validate(reader, constructor);
                    break;
                case JsonToken.Integer:
                case JsonToken.Float:
                case JsonToken.String:
                case JsonToken.Boolean:
                case JsonToken.Date:
                case JsonToken.Bytes:
                case JsonToken.Raw:
                case JsonToken.None:
                case JsonToken.Null:
                case JsonToken.Undefined:
                    Validate(reader, reader.Value);
                    break;
                case JsonToken.PropertyName:
                case JsonToken.Comment:
                case JsonToken.EndObject:
                case JsonToken.EndArray:
                case JsonToken.EndConstructor:
                default:
                    break;
            }
            return reader.Value;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        private void Validate(JsonReader reader, object value)
        {
            foreach (var validator in _validators)
            {
                if (validator != null && !validator.IsValid(value))
                {
                    var lineInfo = reader as IJsonLineInfo;
                    var range = new Range(lineInfo.LineNumber, lineInfo.LinePosition);
                    var validationResult = validator.GetValidationResult(value, new ValidationContext(value, null));
                    _errors.Add(Errors.ViolateSchema(range, validationResult.ErrorMessage));
                }
            }
        }
    }
}
