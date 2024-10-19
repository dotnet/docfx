// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx;

[System.Text.Json.Serialization.JsonConverter(typeof(FileItemsConverter))]
public class FileItems : List<string>
{
    private static readonly IEnumerable<string> Empty = new List<string>();
    public FileItems(string file)
    {
        Add(file);
    }

    public FileItems(IEnumerable<string> files) : base(files ?? Empty)
    {
    }

    public static explicit operator FileItems(string input)
    {
        return new FileItems(input);
    }
}
