// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    using YamlDotNet.Serialization;
    using YamlDotNet.Core;
    using YamlDotNet.Serialization.Utilities;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;

    using YamlDeserializer = Microsoft.DocAsCode.YamlSerialization.YamlDeserializer;

    public class OverwriteDocumentReader
    {
        public static FileModel Read(FileAndType file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (file.Type != DocumentType.Overwrite)
            {
                throw new NotSupportedException(file.Type.ToString());
            }

            return new FileModel(file, null, serializer: new BinaryFormatter());
        }

        /// <summary>
        /// TODO: use Attributes to automatically markup and handle uid inside the model instead of pass in the itemBuilder action
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="itemBuilder"></param>
        /// <returns></returns>
        public static IEnumerable<T> Transform<T>(FileModel model, string uid, Func<T, T> itemBuilder) where T : class, IOverwriteDocumentViewModel
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var overwrites = ((List<OverwriteDocumentModel>)model.Content).Where(s => s.Uid == uid);
            return overwrites.Select(s =>
            {
                try
                {
                    var item = s.ConvertTo<T>();
                    return ResolveContent(item, itemBuilder);
                }
                catch (YamlException ye)
                {
                    throw new DocumentException($"Unable to deserialize YAML header from \"{s.Documentation.Path}\" Line {s.Documentation.StartLine} to TYPE {typeof(T).Name}: {ye.Message}", ye);
                }
            });
        }

        private static T ResolveContent<T>(T item, Func<T, T> itemBuilder) where T : IOverwriteDocumentViewModel
        {
            if (itemBuilder != null)
            {
                item = itemBuilder(item);
            }

            using (var sw = new StringWriter())
            {
                YamlUtility.Serialize(sw, item);
                using (var sr = new StringReader(sw.ToString()))
                {
                    var serializer = new YamlDeserializer(ignoreUnmatched: true);
                    var placeholderValueDeserializer = new PlaceholderValueDeserializer(serializer.ValueDeserializer, item.Conceptual);
                    item = serializer.Deserialize<T>(sr, placeholderValueDeserializer);
                    if (placeholderValueDeserializer.ContainPlaceholder)
                    {
                        item.Conceptual = null;
                    }
                }
            }

            return item;
        }

        private sealed class PlaceholderValueDeserializer : IValueDeserializer
        {
            private readonly IValueDeserializer _innerDeserializer;
            private readonly string _replacer;

            public bool ContainPlaceholder { get; private set; }

            public PlaceholderValueDeserializer(IValueDeserializer innerDeserializer, string replacer)
            {
                _innerDeserializer = innerDeserializer ?? throw new ArgumentNullException(nameof(innerDeserializer));
                _replacer = replacer;
            }

            /// <summary>
            /// AWARENESS: not thread safe
            /// </summary>
            /// <param name="reader"></param>
            /// <param name="expectedType"></param>
            /// <param name="state"></param>
            /// <param name="nestedObjectDeserializer"></param>
            /// <returns></returns>
            public object DeserializeValue(IParser parser, Type expectedType, SerializerState state, IValueDeserializer nestedObjectDeserializer)
            {
                object value = _innerDeserializer.DeserializeValue(parser, expectedType, state, nestedObjectDeserializer);

                if (value is string str && str.Trim() == Constants.ContentPlaceholder)
                {
                    ContainPlaceholder = true;
                    return _replacer;
                }

                return value;
            }
        }
    }
}
