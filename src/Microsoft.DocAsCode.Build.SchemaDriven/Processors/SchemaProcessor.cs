// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class SchemaProcessor
    {
        private readonly IList<IInterpreter> _interpreters;

        public SchemaProcessor(params IInterpreter[] interpreters)
        {
            _interpreters = interpreters;
        }

        public object Process(object raw, DocumentSchema schema, IProcessContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Model == null)
            {
                throw new ArgumentNullException(nameof(context) + "." + nameof(context.Model));
            }

            return InterpretCore(raw, schema, string.Empty, context);
        }

        private object InterpretCore(object value, BaseSchema schema, string path, IProcessContext context)
        {
            if (value is IDictionary<object, object> dict)
            {
                if (schema.Properties != null)
                {
                    foreach (var keyRaw in dict.Keys.ToList())
                    {
                        if (keyRaw is string key)
                        {
                            if (schema.Properties.TryGetValue(key, out var baseSchema))
                            {
                                var val = dict[keyRaw];
                                var obj = InterpretCore(val, baseSchema, $"{path}/{key}", context);
                                if (!ReferenceEquals(obj, val))
                                {
                                    dict[keyRaw] = obj;
                                }
                            }
                        }
                        else
                        {
                            throw new NotSupportedException("Only support string as key");
                        }
                    }
                }
            }

            if (value is IDictionary<string, object> idict)
            {
                if (schema.Properties != null)
                {
                    foreach (var key in idict.Keys.ToList())
                    {
                        if (schema.Properties.TryGetValue(key, out var baseSchema))
                        {
                            var val = idict[key];
                            var obj = InterpretCore(val, baseSchema, $"{path}/{key}", context);
                            if (!ReferenceEquals(obj, val))
                            {
                                idict[key] = obj;
                            }
                        }
                    }
                }
            }

            if (value is IList<object> array)
            {
                if (schema.Items != null)
                {
                    for (var i = 0; i < array.Count; i++)
                    {
                        var val = array[i];
                        var obj = InterpretCore(val, schema.Items, $"{path}/{i}", context);
                        if (!ReferenceEquals(obj, val))
                        {
                            array[i] = obj;
                        }
                    }
                }
            }

            return Interpret(value, schema, path, context);
        }

        private object Interpret(object value, BaseSchema schema, string path, IProcessContext context)
        {
            var val = value;
            foreach (var i in _interpreters.Where(s => s.CanInterpret(schema)))
            {
                val = i.Interpret(schema, val, context, path);
            }

            return val;
        }
    }
}
