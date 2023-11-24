// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.Plugins;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Utilities;
using Constants = Docfx.DataContracts.Common.Constants;
using YamlDeserializer = Docfx.YamlSerialization.YamlDeserializer;

namespace Docfx.Build.Common;

public class OverwriteDocumentReader
{
    public static FileModel Read(FileAndType file)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Type != DocumentType.Overwrite)
        {
            throw new NotSupportedException(file.Type.ToString());
        }

        return new FileModel(file, null);
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
        ArgumentNullException.ThrowIfNull(model);

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
                var message = $"Unable to deserialize YAML header from \"{s.Documentation.Path}\" Line {s.Documentation.StartLine} to TYPE {typeof(T).Name}: {ye.Message}";
                Logger.LogError(message, code: ErrorCodes.Overwrite.InvalidOverwriteDocument);
                throw new DocumentException(message, ye);
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
            using var sr = new StringReader(sw.ToString());
            var serializer = new YamlDeserializer(ignoreUnmatched: true);
            var placeholderValueDeserializer = new PlaceholderValueDeserializer(serializer.ValueDeserializer, item.Conceptual);
            item = serializer.Deserialize<T>(sr, placeholderValueDeserializer);
            if (placeholderValueDeserializer.ContainPlaceholder)
            {
                item.Conceptual = null;
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
