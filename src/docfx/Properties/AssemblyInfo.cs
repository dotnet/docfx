// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("docfx")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyProduct("docfx")]
[assembly: AssemblyCopyright("Copyright © Microsoft DocAsCode 2015")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: InternalsVisibleTo("docfx.Tests")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("ab97bedf-d207-42e9-a56f-c9dd395bfcdd")]

[assembly: AssemblyLicense(
    "This is open-source software under MIT License.")]
[assembly: AssemblyUsage(
    "   Usage1: docfx <docfx.json file path> [-o <output folder path>]",
    "   Usage2: docfx <command> [<args>]",
    "",
    "Supported commands are:",
    "   help        : Get an overall guide for the commands",
    "   init        : Generate an initial docfx.json following the instructions",
    "   metadata    : Generate YAML files for the APIs",
    "   website     : Generate client-only website combining API in YAML files and conceptual files",
    "   export      : Export APIs in project for external reference",
    "   pack        : Pack existed APIs for external reference",
    "",
    "See 'docfx help <command> to read about a specific subcommand guide"
    )]
