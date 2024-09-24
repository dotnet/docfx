// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
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
partial class Inline : OneOfBase<Span, Span[]>
{
    public override bool Equals(object? obj)
    {
        if (obj is not Inline other)
            return false;

        if (Index != other.Index)
            return false;

        if (Value == null && other.Value != null)
            return false;

        if (Value == null && other.Value == null)
            return true;

        switch (Index)
        {
            case 0:
                return AsT0.Equals(other.AsT0);
            case 1:
            default:
                return AsT1.SequenceEqual(other.AsT1);
        }
    }

    public override int GetHashCode() => base.GetHashCode();
}

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

    public override bool Equals(object? obj)
    {
        if (obj is not ApiBase other)
            return false;

        return id == other.id
            && deprecated.Equals(other.deprecated)
            && preview.Equals(other.preview)
            && src == other.src
            && (metadata == other.metadata || (metadata != null && other.metadata != null
                                            && metadata.Count == other.metadata.Count
                                            && !metadata.Except(other.metadata).Any()));
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(id, deprecated, preview, src, metadata);
    }
}

class Api1 : ApiBase
{
    public required string api1 { get; init; }

    public override bool Equals(object? obj)
    {
        if (obj is not Api1 other)
            return false;

        return api1 == other.api1 && base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), api1);
    }
}

class Api2 : ApiBase
{
    public required string api2 { get; init; }

    public override bool Equals(object? obj)
    {
        if (obj is not Api2 other)
            return false;

        return api2 == other.api2 && base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), api2);
    }
}

class Api3 : ApiBase
{
    public required string api3 { get; init; }

    public override bool Equals(object? obj)
    {
        if (obj is not Api3 other)
            return false;

        return api3 == other.api3 && base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), api3);
    }
}

class Api4 : ApiBase
{
    public required string api4 { get; init; }

    public override bool Equals(object? obj)
    {
        if (obj is not Api4 other)
            return false;

        return api4 == other.api4 && base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), api4);
    }
}


[GenerateOneOf]
partial class Api : OneOfBase<Api1, Api2, Api3, Api4> { }

record struct Fact(string name, Inline value);

struct Facts
{
    public required Fact[] facts { get; init; }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is not Facts other)
            return false;

        return Enumerable.SequenceEqual(facts, other.facts);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(facts.Select(n => n.GetHashCode()).ToArray());
    }
}

struct List
{
    public required Inline[] list { get; init; }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is not List other)
            return false;

        return Enumerable.SequenceEqual(list, other.list);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(list.Select(n => n.GetHashCode()).ToArray());
    }
}

struct Inheritance
{
    public required Inline[] inheritance { get; init; }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is not Inheritance other)
            return false;

        return Enumerable.SequenceEqual(inheritance, other.inheritance);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(inheritance.Select(n => n.GetHashCode()).ToArray());
    }
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

    public override bool Equals(object? obj)
    {
        if (obj is not Parameter other)
            return false;

        return name == other.name
            && object.Equals(type, other.type)
            && @default == other.@default
            && description == other.description
            && deprecated.Equals(other.deprecated)
            && preview.Equals(other.preview)
            && optional == other.optional;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(name, type, @default, description, deprecated, preview, optional);
    }
}

struct Parameters
{
    public required Parameter[] parameters { get; init; }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is not Parameters other)
            return false;

        return Enumerable.SequenceEqual(parameters, other.parameters);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(parameters.Select(n => n.GetHashCode()).ToArray());
    }
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
