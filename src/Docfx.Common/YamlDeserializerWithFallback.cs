// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Core;

namespace Docfx.Common;

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
        new(
            (Func<TextReader> tr) => YamlUtility.Deserialize<T>(tr()),
            (string path) => YamlUtility.Deserialize<T>(path));

    public YamlDeserializerWithFallback WithFallback<T>() =>
        new(
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
