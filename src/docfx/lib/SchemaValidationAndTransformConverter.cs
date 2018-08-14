using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal class SchemaValidationAndTransformConverter : SchemaValidationConverter
    {
        private readonly Func<SchemaContentType, string, Document, (List<Error>, string)> _transform;
        private readonly SchemaContentType _type;
        private readonly Document _file;
        private readonly List<Error> _errors;

        public SchemaValidationAndTransformConverter(
            Func<SchemaContentType, string, Document, (List<Error>, string)> transform,
            SchemaContentType type,
            Document file,
            IEnumerable<ValidationAttribute> validators,
            List<Error> errors)
            : base(validators, errors)
        {
            _file = file;
            _transform = transform;
            _type = type;
            _errors = errors;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Execute shcema validation
            base.ReadJson(reader, objectType, existingValue, serializer);

            // Schema violation if the field is not string
            if (reader.TokenType != JsonToken.String)
            {
                var lineInfo = reader as IJsonLineInfo;
                var range = new Range(lineInfo.LineNumber, lineInfo.LinePosition);
                _errors.Add(Errors.ViolateSchema(range, $"Field with attribute '{_type}' should be of string type."));
            }

            var (transformErrors, value) = _transform(_type, (string)reader.Value, _file);
            _errors.AddRange(transformErrors);
            return value;
        }
    }
}
