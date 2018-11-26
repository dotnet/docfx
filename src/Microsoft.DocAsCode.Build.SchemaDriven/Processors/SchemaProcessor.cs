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

        public object Process(object raw, BaseSchema schema, IProcessContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return InterpretCore(raw, schema, string.Empty, context);
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

        private object InterpretCore(object value, BaseSchema schema, string path, IProcessContext context)
        {
            if (!DictionaryInterpret<object>(value, schema, path, context))
            {
                if (!DictionaryInterpret<string>(value, schema, path, context))
                {
                    if (value is IList<object> array)
                    {
                        for (var i = 0; i < array.Count; i++)
                        {
                            var val = array[i];
                            var obj = InterpretCore(val, schema?.Items, $"{path}/{i}", context);
                            if (!ReferenceEquals(obj, val))
                            {
                                array[i] = obj;
                            }
                        }
                    }
                }
            }

            return Interpret(value, schema, path, context);
        }

        private bool DictionaryInterpret<TKey>(object value, BaseSchema schema, string path, IProcessContext context)
        {
            if (value is IDictionary<TKey, object> dict)
            {
                foreach (var keyRaw in dict.Keys.ToList())
                {
                    var key = keyRaw as string;
                    if (key != null)
                    {
                        BaseSchema baseSchema = null;
                        schema?.Properties?.TryGetValue(key, out baseSchema);
                        var val = dict[keyRaw];
                        var obj = InterpretCore(val, baseSchema, $"{path}/{key}", context);
                        if (!ReferenceEquals(obj, val))
                        {
                            dict[keyRaw] = obj;
                        }
                    }
                    else
                    {
                        throw new NotSupportedException("Only support string as key");
                    }
                }
                return true;
            }

            return false;
        }
    }
}
