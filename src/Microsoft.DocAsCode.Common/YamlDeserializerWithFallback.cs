// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.IO;

    using YamlDotNet.Core;

    public class YamlDeserializerWithFallback
    {
        private readonly Func<Func<TextReader>, object> _textReaderDeserialize;
        private readonly Func<string, object> _filePathDeserialize;

        private YamlDeserializerWithFallback(
            Func<Func<TextReader>, object> textReaderDeserialize,
            Func<string, object> filePathDeserialize)
        {
            _textReaderDeserialize = textReaderDeserialize;
            _filePathDeserialize = filePathDeserialize;
        }

        public static YamlDeserializerWithFallback Create<T>() =>
            new YamlDeserializerWithFallback(
                (Func<TextReader> tr) => YamlUtility.Deserialize<T>(tr()),
                (string path) => YamlUtility.Deserialize<T>(path));

        public YamlDeserializerWithFallback WithFallback<T>() =>
            new YamlDeserializerWithFallback(
                Fallback(_textReaderDeserialize, tr => YamlUtility.Deserialize<T>(tr())),
                Fallback(_filePathDeserialize, p => YamlUtility.Deserialize<T>(p)));

        public object Deserialize(Func<TextReader> reader) =>
            _textReaderDeserialize(reader);

        public object Deserialize(string filePath) =>
            _filePathDeserialize(filePath);

        private static Func<T1, object> Fallback<T1>(
            Func<T1, object> first,
            Func<T1, object> second) =>
            p =>
            {
                try
                {
                    return first(p);
                }
                catch (YamlException ex)
                {
                    try
                    {
                        return second(p);
                    }
                    catch (YamlException exFallback)
                    {
                        if (ex.Start.CompareTo(exFallback.Start) < 0)
                        {
                            throw;
                        }
                    }
                    throw;
                }
            };
    }
}
