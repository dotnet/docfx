// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference.FSharp.Tests

open System
open System.IO
open System.Collections.Generic
open Xunit
open Xunit.Abstractions
open FSharp.Compiler.SourceCodeServices

open Microsoft.DocAsCode.Metadata.ManagedReference
open Microsoft.DocAsCode.Metadata.ManagedReference.FSharp


[<Collection("F# Test Collection")>]
type FSharpProjectTests  (output: ITestOutputHelper) =
    let printfn format = Printf.kprintf (fun msg -> output.WriteLine(msg)) format 
   
    let makeLoader () =
        let msBuildProps = Dictionary<string, string> ()
        let fsLoader = FSharpProjectLoader (msBuildProps)
        AbstractProjectLoader ([fsLoader])

    [<Fact>]
    let NetCoreProjectInfo () =
        let loader = makeLoader ()
        let projPath = "TestData/NetCoreProject/NetCoreProject.fsproj"

        let proj = loader.Load(projPath)
        Assert.Equal (Path.GetFullPath projPath, proj.FilePath)
        Assert.True (proj.HasDocuments)
        let docs = List.ofSeq proj.Documents
        Assert.Equal (5, docs.Length)
        Assert.Equal (Path.GetFullPath "TestData/NetCoreProject/Module1.fs", docs.[1].FilePath)
        Assert.Equal (Path.GetFullPath "TestData/NetCoreProject/Module2.fs", docs.[2].FilePath)
        Assert.Equal (Path.GetFullPath "TestData/NetCoreProject/InterfaceImpls.fs", docs.[3].FilePath)
        Assert.Equal (Path.GetFullPath "TestData/NetCoreProject/Program.fs", docs.[4].FilePath)
        let refs = List.ofSeq proj.ProjectReferences
        Assert.Equal (1, refs.Length)
        Assert.IsType<FSharpProject> refs.[0] |> ignore
        Assert.Equal (Path.GetFullPath "TestData/NetCoreLibProject/NetCoreLibProject.fsproj", refs.[0].FilePath)
                                  
    [<Fact>]
    let NetCoreProjectCompilation () =
        let loader = makeLoader ()
        let projPath = "TestData/NetCoreProject/NetCoreProject.fsproj"

        let proj = loader.Load(projPath)
        let comp = proj.GetCompilationAsync().Result
        Assert.IsType<FSharpCompilation> comp |> ignore

    [<Fact>]
    let NetCoreLibProjectInfo () =
        let loader = makeLoader ()
        let projPath = "TestData/NetCoreLibProject/NetCoreLibProject.fsproj"

        let proj = loader.Load(projPath)
        Assert.Equal (Path.GetFullPath projPath, proj.FilePath)
        Assert.True (proj.HasDocuments)
        let docs = List.ofSeq proj.Documents
        Assert.Equal (2, docs.Length)
        Assert.Equal (Path.GetFullPath "TestData/NetCoreLibProject/Library.fs", docs.[1].FilePath)
        let refs = List.ofSeq proj.ProjectReferences
        Assert.Equal (0, refs.Length)

    [<Fact>]
    let ProjectInfosAreCached () =
        let loader = makeLoader ()
        let projDir = "TestData/NetCoreLibProjectCopy"
        let projPath = "TestData/NetCoreLibProjectCopy/NetCoreLibProjectCopy.fsproj"

        let assertProjectInfo () =
            let proj = loader.Load(projPath)
            Assert.True (proj.HasDocuments)
            let docs = List.ofSeq proj.Documents
            Assert.Equal (2, docs.Length)
            Assert.Equal (Path.GetFullPath "TestData/NetCoreLibProjectCopy/Library.fs", docs.[1].FilePath)

        assertProjectInfo ()

        // Check multiple projects can be loaded and they don't interfere
        let otherProj = loader.Load("TestData/NetCoreProject/NetCoreProject.fsproj")
        Assert.Equal (5, Seq.length otherProj.Documents)

        // Now move the original project to a different directory
        Directory.Move(projDir, projDir + ".old")
        use _cleanup = { new IDisposable with override __.Dispose () = Directory.Move(projDir + ".old", projDir) }

        // Assert that we can still load the original project (as its project info is cached)
        Assert.False (File.Exists projPath)
        assertProjectInfo ()
