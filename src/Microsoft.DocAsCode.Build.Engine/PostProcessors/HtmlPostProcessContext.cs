// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Common.StreamSegmentSerialization;
    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json;

    public class HtmlPostProcessContext
    {
        [JsonIgnore]
        public IPostProcessorHost PostProcessorHost { get; private set; }
        private Dictionary<string, object> _savingContext = new Dictionary<string, object>();

        public HtmlPostProcessContext(IPostProcessorHost host)
        {
            PostProcessorHost = host;
        }

        public T Load<T>(string contextName, Func<Stream, T> loader)
        {
            using (var stream = PostProcessorHost?.LoadContextInfo())
            {
                if (stream == null)
                {
                    return default(T);
                }
                var deserializer = new StreamDeserializer(stream);
                var seg = deserializer.ReadSegment();
                var entries = deserializer.ReadDictionaryLazy(seg);
                if (!entries.TryGetValue(contextName, out Lazy<object> lazy))
                {
                    return default(T);
                }
                var bytes = (byte[])lazy.Value;
                return loader(new MemoryStream(bytes));
            }
        }

        public void Save(string contextName, Action<Stream> saver)
        {
            var ms = new MemoryStream();
            saver(ms);
            _savingContext[contextName] = ms.ToArray();
        }

        public void Save()
        {
            using (var stream = PostProcessorHost?.SaveContextInfo())
            {
                if (stream == null || PostProcessorHost?.ShouldTraceIncrementalInfo == false)
                {
                    return;
                }
                var serializer = new StreamSerializer(stream);
                serializer.Write(_savingContext);
            }
        }
    }
}
