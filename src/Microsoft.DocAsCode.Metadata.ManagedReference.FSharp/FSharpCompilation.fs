// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference.FSharp
open System.IO
open System.Collections.Generic
open System.Text.RegularExpressions
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.DocAsCode.Metadata.ManagedReference
open Microsoft.DocAsCode.DataContracts.ManagedReference
open Microsoft.DocAsCode.DataContracts.Common


/// <summary>An F# compilation of an F# project. Can extract metadata from the F# project.</summary>
/// <param name="compilation">The compilation output from the F# compiler service.</param>
/// <param name="projPath">The path to the F# project file.</param>
type FSharpCompilation (compilation: FSharpCheckProjectResults, projPath: string) =
    inherit AbstractCompilation()

    override this.GetBuildController() =
        FSharpBuildController (this) :> IBuildController


/// <summary>Build controller for an F# compilation.</summary>
/// <param name="compilation">the F# compilation</param>
and FSharpBuildController (compilation: FSharpCompilation) =
    interface IBuildController with
        member __.ExtractMetadata parameters =
            // TODO: actual metadata extraction
            MetadataItem()

