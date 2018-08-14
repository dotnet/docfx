using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    internal class SchemaValidationAndTransformContractResolver : JsonContractResolver
    {
        private readonly Func<SchemaContentType, string, Document, (List<Error>, string)> _transform;
        private readonly List<Error> _errors;
        private readonly Document _file;

        public SchemaValidationAndTransformContractResolver(Func<SchemaContentType, string, Document, (List<Error>, string)> transform, List<Error> errors, Document file)
        {
            _transform = transform;
            _errors = errors;
            _file = file;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var prop = base.CreateProperty(member, memberSerialization);
            var converter = GetConverter(member, _errors);

            if (converter != null)
            {
                if (prop.PropertyType.IsArray)
                {
                    prop.ItemConverter = converter;
                }
                else
                {
                    prop.Converter = converter;
                }
            }
            return prop;
        }

        private SchemaValidationConverter GetConverter(MemberInfo member, List<Error> errors)
        {
            var validators = member.GetCustomAttributes<ValidationAttribute>(false);
            var contentType = member.GetCustomAttribute<SchemaContentTypeAttribute>();
            if (contentType != null && contentType.ContentType != SchemaContentType.None)
            {
                return new SchemaValidationAndTransformConverter(_transform, contentType.ContentType, _file, validators, errors);
            }
            return null;
        }
    }
}
