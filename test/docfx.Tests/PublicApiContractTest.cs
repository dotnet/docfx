// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using PublicApiGenerator;

namespace Docfx.Tests;

public class PublicApiContractTest
{
    [Fact]
    public static Task TestPublicApiContract()
    {
        var assemblies = new HashSet<Assembly>();
        GetAssemblies(typeof(Docset).Assembly);

        var publicApi = string.Join('\n', assemblies
            .OrderBy(a => a.FullName)
            .Select(a => a.GeneratePublicApi(new() { IncludeAssemblyAttributes = false })));

        return Verify(new Target("cs", publicApi)).UseFileName("Api").AutoVerify(includeBuildServer: false);

        void GetAssemblies(Assembly assembly)
        {
            assemblies.Add(assembly);

            foreach (var name in assembly.GetReferencedAssemblies())
            {
                if (name.Name.StartsWith("Docfx.", StringComparison.OrdinalIgnoreCase))
                {
                    GetAssemblies(Assembly.Load(name.Name));
                }
            }
        }
    }
}
