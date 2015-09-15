// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Builders
{
    using System.IO;
    using System.Runtime.Serialization;
    using System.Text;

    public sealed class YamlFormatter<T> : IFormatter
    {
        public static readonly YamlFormatter<T> Instance = new YamlFormatter<T>();

        public SerializationBinder Binder { get; set; }

        public StreamingContext Context { get; set; }

        public ISurrogateSelector SurrogateSelector { get; set; }

        public T Deserialize(Stream serializationStream)
        {
            using (var reader = new StreamReader(serializationStream, Encoding.UTF8, false, 4096, true))
            {
                return YamlUtility.Deserialize<T>(reader);
            }
        }

        public void Serialize(Stream serializationStream, T graph)
        {
            using (var writer = new StreamWriter(serializationStream, Encoding.UTF8, 4096, true))
            {
                YamlUtility.Serialize(writer, graph);
            }
        }

        object IFormatter.Deserialize(Stream serializationStream)
        {
            return Deserialize(serializationStream);
        }

        void IFormatter.Serialize(Stream serializationStream, object graph)
        {
            Serialize(serializationStream, (T)graph);
        }
    }
}
