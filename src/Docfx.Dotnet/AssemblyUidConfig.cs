// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx;

/// <summary>
/// The assemblies whose APIs carry an assembly component in their UID, mapped to that component.
/// A null value means the assembly is qualified by its own name, which is the normal case.
/// <para>
/// Accepts both shapes: an array of assembly names, <c>[ "MyLib", "MyLib.Tools" ]</c>, and an object that
/// names the component explicitly, <c>{ "MyLib.Tools": "Tools" }</c>.
/// </para>
/// </summary>
[Newtonsoft.Json.JsonConverter(typeof(AssemblyUidConfigConverter.NewtonsoftJsonConverter))]
[System.Text.Json.Serialization.JsonConverter(typeof(AssemblyUidConfigConverter.SystemTextJsonConverter))]
internal class AssemblyUidConfig : Dictionary<string, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssemblyUidConfig"/> class.
    /// </summary>
    public AssemblyUidConfig()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AssemblyUidConfig"/> class.
    /// </summary>
    /// <param name="assemblyUids">The assembly name to component pairs copied to the new instance.</param>
    public AssemblyUidConfig(IEnumerable<KeyValuePair<string, string>> assemblyUids) : base(assemblyUids)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AssemblyUidConfig"/> class from assembly names alone,
    /// each qualified by its own name.
    /// </summary>
    /// <param name="assemblyNames">The names of the assemblies to qualify.</param>
    public AssemblyUidConfig(IEnumerable<string> assemblyNames)
    {
        foreach (var assemblyName in assemblyNames)
        {
            // A name repeated in the array means the same thing twice, and an empty one is reported as an
            // invalid entry later. Neither is worth throwing out of a JSON converter over.
            this[assemblyName ?? ""] = null;
        }
    }
}
