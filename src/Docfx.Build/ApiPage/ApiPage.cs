// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using OneOf;

#nullable enable

namespace Docfx.Build.ApiPage;

struct LinkSpan
{
    public required string text { get; init; }
    public string? url { get; init; }
}

[GenerateOneOf]
partial class Span : OneOfBase<string, LinkSpan> { }

[GenerateOneOf]
partial class Inline : OneOfBase<Span, Span[]> { }

struct Markdown
{
    public required string markdown { get; init; }
}

struct H1
{
    public required string h1 { get; init; }
    public string? id { get; init; }
}

struct H2
{
    public required string h2 { get; init; }
    public string? id { get; init; }
}

struct H3
{
    public required string h3 { get; init; }
    public string? id { get; init; }
}

struct H4
{
    public required string h4 { get; init; }
    public string? id { get; init; }
}

struct H5
{
    public required string h5 { get; init; }
    public string? id { get; init; }
}

struct H6
{
    public required string h6 { get; init; }
    public string? id { get; init; }
}

[GenerateOneOf]
partial class Heading : OneOfBase<H1, H2, H3, H4, H5, H6> { }

abstract class ApiBase
{
    public string? id { get; init; }
    public OneOf<bool, string>? deprecated { get; init; }
    public OneOf<bool, string>? preview { get; init; }
    public string? src { get; init; }
    public Dictionary<string, string>? metadata { get; init; }
}

class Api1 : ApiBase
{
    public required string api1 { get; init; }
}

class Api2 : ApiBase
{
    public required string api2 { get; init; }
}

class Api3 : ApiBase
{
    public required string api3 { get; init; }
}

class Api4 : ApiBase
{
    public required string api4 { get; init; }
}


[GenerateOneOf]
partial class Api : OneOfBase<Api1, Api2, Api3, Api4> { }

record struct Fact(string name, Inline value);

struct Facts
{
    public required Fact[] facts { get; init; }
}

struct List
{
    public required Inline[] list { get; init; }
}

struct Inheritance
{
    public required Inline[] inheritance { get; init; }
}

struct Code
{
    public required string code { get; init; }
    public string? languageId { get; init; }
}

class Parameter
{
    public string? name { get; init; }
    public Inline? type { get; init; }
    public string? @default { get; init; }
    public string? description { get; init; }
    public OneOf<bool, string>? deprecated { get; init; }
    public OneOf<bool, string>? preview { get; init; }
    public bool? optional { get; init; }
}

struct Parameters
{
    public required Parameter[] parameters { get; init; }
}

[GenerateOneOf]
partial class Block : OneOfBase<Heading, Api, Markdown, Facts, Parameters, List, Inheritance, Code> { }

record ApiPage
{
    public static JsonSerializerOptions JsonSerializerOptions { get; } = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault,
    };

    static ApiPage()
    {
        JsonSerializerOptions.Converters.Add(new OneOfJsonConverterFactory());
    }

    public required string title { get; init; }
    public required Block[] body { get; init; }

    public string? languageId { get; init; }
    public Dictionary<string, OneOf<string, string[]>>? metadata { get; init; }
}
