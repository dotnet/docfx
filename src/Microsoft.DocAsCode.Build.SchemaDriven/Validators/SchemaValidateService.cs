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
        private static readonly object _sync = new object();
        private static SchemaValidateService _service = null;

        private readonly object _locker = new object();
        private bool _schemaValidationEnabled = true;

        private SchemaValidateService(string license = null)
        {
            if (!string.IsNullOrEmpty(license))
            {
                RegisterLicense(license);
            }
        }

        public static SchemaValidateService GetInstance(string license = null)
        {
            if (_service == null)
            {
                lock (_sync)
                {
                    if (_service == null)
                    {
                        _service = new SchemaValidateService(license);
                    }
                }
            }

            return _service;
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
                throw new InvalidSchemaException($"Validation against {schema.SchemaVersion.OriginalString} failed: \n{errors.ToDelimitedString("\n")}");
            }
        }

        private void ValidateObject(object obj, JSchema schema, SchemaValidationEventHandler validationEventHandler)
        {
            var jsonReader = obj is JObject jo ? jo.CreateReader() : new ObjectJsonReader(obj);
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

        private void RegisterLicense(string license)
        {
            try
            {
                License.RegisterLicense(license);
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Encountered issue registering license for NewtonsoftJson.Schema, schema validation will be disabled: {e.Message}");
                _schemaValidationEnabled = false;
            }
        }
    }
}
