// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AwesomeAssertions;
using Docfx.DataContracts.ManagedReference;
using Xunit;

namespace Docfx.Dotnet.Tests;

public class SetDerivedClassTest
{
    [Fact]
    public void ATypeImplementingAnInterfaceTwiceIsListedOnceAsDerived()
    {
        // `class C : IConverter<int>, IConverter<string>`, resolved twice, as two metadata items of one run are
        MetadataModel Model() => new()
        {
            Members =
            [
                new() { Name = "N.IConverter`1", Type = MemberType.Interface },
                new() { Name = "N.C", Type = MemberType.Class, Implements = ["N.IConverter{System.Int32}", "N.IConverter{System.String}"] },
            ],
        };
        var context = new ResolverContext
        {
            References = new()
            {
                ["N.IConverter{System.Int32}"] = new() { Definition = "N.IConverter`1" },
                ["N.IConverter{System.String}"] = new() { Definition = "N.IConverter`1" },
            },
        };
        var pipeline = new SetDerivedClass();

        var first = Model();
        pipeline.Run(first, context);
        var second = Model();
        pipeline.Run(second, context);

        first.Members[0].DerivedClasses.Should().Equal("N.C");
        second.Members[0].DerivedClasses.Should().Equal("N.C");
    }

    [Fact]
    public void DerivedClassesAreSorted()
    {
        var model = new MetadataModel
        {
            Members =
            [
                new() { Name = "N.B", Type = MemberType.Class },
                new() { Name = "N.Z", Type = MemberType.Class, Inheritance = ["System.Object", "N.B"] },
                new() { Name = "N.A", Type = MemberType.Class, Inheritance = ["System.Object", "N.B"] },
            ],
        };

        new SetDerivedClass().Run(model, new ResolverContext { References = [] });

        model.Members[0].DerivedClasses.Should().Equal("N.A", "N.Z");
    }
}
