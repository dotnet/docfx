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
        Assert.Equal (proj.FilePath, Path.GetFullPath projPath)
        Assert.True (proj.HasDocuments)
        let docs = List.ofSeq proj.Documents
        Assert.Equal (docs.Length, 3)
        Assert.Equal (docs.[0].FilePath, Path.GetFullPath "TestData/NetCoreProject/Module1.fs")
        Assert.Equal (docs.[1].FilePath, Path.GetFullPath "TestData/NetCoreProject/Module2.fs")
        Assert.Equal (docs.[2].FilePath, Path.GetFullPath "TestData/NetCoreProject/Program.fs")
        let refs = List.ofSeq proj.ProjectReferences
        Assert.Equal (refs.Length, 1)
        Assert.IsType<FSharpProject> refs.[0] |> ignore
        Assert.Equal (refs.[0].FilePath, Path.GetFullPath "TestData/NetCoreLibProject/NetCoreLibProject.fsproj")
                                  
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
        Assert.Equal (proj.FilePath, Path.GetFullPath projPath)
        Assert.True (proj.HasDocuments)
        let docs = List.ofSeq proj.Documents
        Assert.Equal (docs.Length, 1)
        Assert.Equal (docs.[0].FilePath, Path.GetFullPath "TestData/NetCoreLibProject/Library.fs")
        let refs = List.ofSeq proj.ProjectReferences
        Assert.Equal (refs.Length, 0)
        
        