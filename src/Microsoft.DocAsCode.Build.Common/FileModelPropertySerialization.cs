// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Common.StreamSegmentSerialization;
    using Microsoft.DocAsCode.Plugins;

    public static class FileModelPropertySerialization
    {
        public static void Serialize(FileModel model, Stream stream, Action<object, Stream> contentSerializer, Action<IDictionary<string, object>, Stream> propertySerializer, Action<FileModel, Stream> otherSerializer)
        {
            var ss = new StreamSerializer(stream);

            ss.Write(new ModelBasicPropertyCollection(model));

            ss.Write(s => contentSerializer(model.Content, s));

            if (propertySerializer == null)
            {
                ss.WriteNull();
            }
            else
            {
                ss.Write(s => propertySerializer(new Dictionary<string, object>((IDictionary<string, object>)model.Properties), s));
            }

            if (otherSerializer == null)
            {
                ss.WriteNull();
            }
            else
            {
                ss.Write(s => otherSerializer(model, s));
            }
        }

        public static FileModel Deserialize(Stream stream, IFormatter formatter, Func<Stream, object> contentDeserializer, Func<Stream, IDictionary<string, object>> propertyDeserializer, Action<Stream, FileModel> otherDeserializer)
        {
            var sd = new StreamDeserializer(stream);

            var basicPropertiesSegment = sd.ReadSegment();
            var basicProperties = sd.ReadDictionary(basicPropertiesSegment);

            var contentSegment = sd.ReadNext(basicPropertiesSegment);
            var content = contentDeserializer(sd.ReadBinaryAsStream(contentSegment));

            var result = new FileModel(
                JsonUtility.Deserialize<FileAndType>(new StringReader((string)basicProperties[nameof(FileModel.FileAndType)])),
                content,
                JsonUtility.Deserialize<FileAndType>(new StringReader((string)basicProperties[nameof(FileModel.OriginalFileAndType)])),
                formatter,
                (string)basicProperties[nameof(FileModel.Key)]);

            // Deserialize basic properties.
            result.LocalPathFromRoot = (string)basicProperties[nameof(FileModel.LocalPathFromRoot)];
            result.LinkToFiles = ((object[])basicProperties[nameof(FileModel.LinkToFiles)]).OfType<string>().ToImmutableHashSet();
            result.LinkToUids = ((object[])basicProperties[nameof(FileModel.LinkToUids)]).OfType<string>().ToImmutableHashSet();
            result.FileLinkSources =
                JsonUtility.Deserialize<Dictionary<string, List<LinkSourceInfo>>>(
                    new StringReader((string)basicProperties[nameof(FileModel.FileLinkSources)]))
                .ToImmutableDictionary(
                    pair => pair.Key,
                    pair => pair.Value.ToImmutableList());
            result.UidLinkSources =
                JsonUtility.Deserialize<Dictionary<string, List<LinkSourceInfo>>>(
                    new StringReader((string)basicProperties[nameof(FileModel.UidLinkSources)]))
                .ToImmutableDictionary(
                    pair => pair.Key,
                    pair => pair.Value.ToImmutableList());
            result.Uids = JsonUtility.Deserialize<List<UidDefinition>>(
                new StringReader((string)basicProperties[nameof(FileModel.Uids)])).ToImmutableArray();

            var manifestProperties = (IDictionary<string, object>)result.ManifestProperties;
            foreach (var pair in
                JsonUtility.Deserialize<Dictionary<string, object>>(
                    new StringReader((string)basicProperties[nameof(FileModel.ManifestProperties)])))
            {
                manifestProperties[pair.Key] = pair.Value;
            }

            // Deserialize properties.
            var propertySegment = sd.ReadNext(contentSegment);
            var properties = (IDictionary<string, object>)result.Properties;
            if (propertyDeserializer != null && propertySegment.ContentType == StreamSegmentType.Binary)
            {
                var dictionary = propertyDeserializer(sd.ReadBinaryAsStream(propertySegment));
                foreach (var pair in dictionary)
                {
                    properties[pair.Key] = pair.Value;
                }
            }

            // Deserialize others.
            var otherSegment = sd.ReadNext(propertySegment);
            if (otherDeserializer != null && otherSegment.ContentType == StreamSegmentType.Binary)
            {
                otherDeserializer(sd.ReadBinaryAsStream(otherSegment), result);
            }

            return result;
        }

        private sealed class ModelBasicPropertyCollection : IReadOnlyCollection<KeyValuePair<string, object>>
        {
            public FileModel Model { get; }

            public ModelBasicPropertyCollection(FileModel model)
            {
                Model = model;
            }

            public int Count => 10;

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                yield return new KeyValuePair<string, object>(nameof(Model.FileAndType), JsonUtility.ToJsonString(Model.FileAndType));
                yield return new KeyValuePair<string, object>(nameof(Model.OriginalFileAndType), JsonUtility.ToJsonString(Model.OriginalFileAndType));
                yield return new KeyValuePair<string, object>(nameof(Model.Key), Model.Key);
                yield return new KeyValuePair<string, object>(nameof(Model.LocalPathFromRoot), Model.LocalPathFromRoot);
                yield return new KeyValuePair<string, object>(nameof(Model.LinkToFiles), Model.LinkToFiles);
                yield return new KeyValuePair<string, object>(nameof(Model.LinkToUids), Model.LinkToUids);
                yield return new KeyValuePair<string, object>(nameof(Model.FileLinkSources), JsonUtility.ToJsonString(Model.FileLinkSources));
                yield return new KeyValuePair<string, object>(nameof(Model.UidLinkSources), JsonUtility.ToJsonString(Model.UidLinkSources));
                yield return new KeyValuePair<string, object>(nameof(Model.Uids), JsonUtility.ToJsonString(Model.Uids));
                yield return new KeyValuePair<string, object>(nameof(Model.ManifestProperties), JsonUtility.ToJsonString(new Dictionary<string, object>((IDictionary<string, object>)Model.ManifestProperties)));
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
