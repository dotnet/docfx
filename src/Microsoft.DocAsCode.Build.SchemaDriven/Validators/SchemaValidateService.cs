// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;

    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Schema;

    public class SchemaValidateService
    {
        private readonly object _locker = new object();
        private volatile bool _schemaValidationEnabled = true;

        public static SchemaValidateService Instance { get; private set; } = new SchemaValidateService();

        private SchemaValidateService()
        {
        }

        public static void RegisterLicense(string license)
        {
            if (string.IsNullOrEmpty(license))
            {
                return;
            }

            try
            {
                License.RegisterLicense(license);
                Instance = new SchemaValidateService();
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Encountered issue registering license for NewtonsoftJson.Schema: {e.Message}");
            }
        }

        public void Validate(object obj, JSchema schema)
        {
            if (!_schemaValidationEnabled)
            {
                return;
            }

            var errors = new List<string>();
            try
            {
                ValidateObject(obj, schema, (sender, args) => errors.Add(args.Message));
            }
            catch (JSchemaException e)
            {
                if (_schemaValidationEnabled)
                {
                    lock (_locker)
                    {
                        if (_schemaValidationEnabled)
                        {
                            Logger.LogWarning($"Limitation reached when validating object using NewtonsoftJson.Schema, you can provide your license using '--schemaLicense' for advanced uses: {e.Message}");
                            _schemaValidationEnabled = false;
                        }
                    }
                }
            }

            if (errors.Count > 0)
            {
                var idInfo = schema.Id == null ? string.Empty : $" ({schema.Id})";
                throw new InvalidSchemaException($"Schema validation failed. Please validate the file and make sure it conforms to schema '{schema.Title}'{idInfo}: \n{errors.ToDelimitedString("\n")}");
            }
        }

        private void ValidateObject(object obj, JSchema schema, SchemaValidationEventHandler validationEventHandler)
        {
            var jsonReader = obj is JObject jo ? jo.CreateReader() : new IgnoreStrongTypeObjectJsonReader(obj);
            using (var reader = new JSchemaValidatingReader(jsonReader))
            {
                reader.Schema = schema;
                if (validationEventHandler != null)
                {
                    reader.ValidationEventHandler += validationEventHandler;
                }
                while (reader.Read())
                {
                }
            }
        }
    }
}
