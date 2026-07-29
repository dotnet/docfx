// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Jint.Native;

namespace Docfx.Build.Engine;

public static class JintProcessorHelper
{
    public static JsValue ConvertObjectToJsValue(Jint.Engine engine, object raw)
    {
        if (raw is IDictionary<string, object> dict)
        {
            // Build the object directly in the engine's hidden class ("shape") representation rather than
            // storing a property descriptor per property. Every document of a given document type is handed
            // to the template with the same property layout, so the objects built here end up sharing one
            // interned shape and the property reads in the template script stay monomorphic across documents.
            //
            // Layouts the representation cannot express - integer-index-like keys, very wide objects - fall
            // back to the ordinary property-dictionary representation silently and correctly.
            return JsObject.CreateFromEntries(engine, ConvertEntries(engine, dict));
        }

        if (raw is IList<object> list)
        {
            // allow Jint to take ownership of the array
            var elements = new JsValue[list.Count];
            for (int i = 0; i < (uint)elements.Length; i++)
            {
                elements[i] = ConvertObjectToJsValue(engine, list[i]);
            }

            return new JsArray(engine, elements);
        }

        return JsValue.FromObject(engine, raw);
    }

    /// <summary>
    /// Streams the converted entries instead of materializing them into an array first: the entries are
    /// enumerated exactly once while the object is being built, so a model with many properties does not
    /// need a temporary buffer of its own size.
    /// </summary>
    private static IEnumerable<KeyValuePair<string, JsValue>> ConvertEntries(Jint.Engine engine, IDictionary<string, object> dict)
    {
        foreach (var pair in dict)
        {
            yield return new KeyValuePair<string, JsValue>(pair.Key, ConvertObjectToJsValue(engine, pair.Value));
        }
    }
}
