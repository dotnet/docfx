// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference.FSharp.Tests

open System.IO
open System.Collections.Generic
open Xunit
open Xunit.Abstractions
open Microsoft.FSharp.Compiler.SourceCodeServices

open Microsoft.DocAsCode.Metadata.ManagedReference
open Microsoft.DocAsCode.Metadata.ManagedReference.FSharp


[<Collection("F# Test Collection")>]
type FSharpProjectTests  (output: ITestOutputHelper) =
    let printfn format = Printf.kprintf (fun msg -> output.WriteLine(msg)) format 
   
    let loaderCheckerProps () =
        let msBuildProps = Dictionary<string, string> ()
        let fsLoader = FSharpProjectLoader (msBuildProps)
        let loader = AbstractProjectLoader ([fsLoader])
        let checker = FSharpChecker.Create()    
        loader, checker, msBuildProps    

    [<Fact>]
    let NetCoreProjectInfo () =
        let loader, checker, msBuildProps = loaderCheckerProps()
        let projPath = "TestData/NetCoreProject/NetCoreProject.fsproj"

        let proj = FSharpProject (projPath, msBuildProps, loader, checker)
        Assert.Equal (Path.GetFullPath projPath, proj.FilePath)
        Assert.True (proj.HasDocuments)
        let docs = List.ofSeq proj.Documents
        Assert.Equal (4, docs.Length)
        Assert.Equal (Path.GetFullPath "TestData/NetCoreProject/Module1.fs", docs.[1].FilePath)
        Assert.Equal (Path.GetFullPath "TestData/NetCoreProject/Module2.fs", docs.[2].FilePath)
        Assert.Equal (Path.GetFullPath "TestData/NetCoreProject/Program.fs", docs.[3].FilePath)
        let refs = List.ofSeq proj.ProjectReferences
        Assert.Equal (1, refs.Length)
        Assert.IsType<FSharpProject> refs.[0] |> ignore
        Assert.Equal (Path.GetFullPath "TestData/NetCoreLibProject/NetCoreLibProject.fsproj", refs.[0].FilePath)
                                  
    [<Fact>]
    let NetCoreProjectCompilation () =
        let loader, checker, msBuildProps = loaderCheckerProps()
        let projPath = "TestData/NetCoreProject/NetCoreProject.fsproj"

        let proj = FSharpProject (projPath, msBuildProps, loader, checker)
        let comp = proj.GetCompilationAsync().Result
        Assert.IsType<FSharpCompilation> comp |> ignore

    [<Fact>]
    let NetCoreLibProjectInfo () =
        let loader, checker, msBuildProps = loaderCheckerProps()
        let projPath = "TestData/NetCoreLibProject/NetCoreLibProject.fsproj"

        let proj = FSharpProject (projPath, msBuildProps, loader, checker)
        Assert.Equal (Path.GetFullPath projPath, proj.FilePath)
        Assert.True (proj.HasDocuments)
        let docs = List.ofSeq proj.Documents
        Assert.Equal (2, docs.Length)
        Assert.Equal (Path.GetFullPath "TestData/NetCoreLibProject/Library.fs", docs.[1].FilePath)
        let refs = List.ofSeq proj.ProjectReferences
        Assert.Equal (0, refs.Length)
        
        