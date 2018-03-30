// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference.FSharp

open System.IO
open System.Collections.Generic
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.DocAsCode.Metadata.ManagedReference


/// <summary>An F# source file.</summary>
/// <param name="docPath">Path to the source file</param>
type FSharpDocument (docPath: string) =
    inherit AbstractDocument()
    let docPath = Path.GetFullPath(docPath)
    override __.FilePath = docPath


/// <summary>An F# project.</summary>
/// <param name="projPath">Path to F# project file.</param>
/// <param name="msbuildProps">MsBuild properties to set.</param>
/// <param name="loader">Project loader to use for loading referenced projects.</param>
/// <param name="checker">F# compiler service instance.</param>
type FSharpProject (projPath: string, 
                    msbuildProps: IDictionary<string, string>,
                    loader: AbstractProjectLoader,
                    checker: FSharpChecker) =
    inherit AbstractProject()

    let projPath = Path.GetFullPath(projPath)
    let projInfo = FSharpProjectInfo.fromProjectFile projPath msbuildProps

    override __.FilePath = 
        projPath

    override __.Documents = 
        projInfo.Srcs
        |> List.map (fun src -> FSharpDocument(src) :> AbstractDocument)
        |> Seq.ofList
    
    override __.HasDocuments =
        __.Documents |> Seq.isEmpty |> not

    override __.ProjectReferences = 
        projInfo.Refs |> Seq.map (fun projPath -> loader.Load(projPath))

    override __.PortableExecutableMetadataReferences = 
        Seq.empty

    override __.GetCompilationAsync() =
        async {
            let args = projInfo.Args @ projInfo.Srcs |> List.toArray
            let opts = checker.GetProjectOptionsFromCommandLineArgs(projPath, args)         
            Log.verbose "Compiling F# project %s" projPath
            Log.debug "Using F# compiler options %A" opts
            let! compilation = checker.ParseAndCheckProject(opts)
            let errStr =
                compilation.Errors
                |> Seq.filter (fun err -> err.Severity = FSharpErrorSeverity.Error)
                |> Seq.map (fun err -> 
                    sprintf "%s(%d): %s" err.FileName err.StartLineAlternate err.Message)
                |> String.concat "\n"
            if errStr.Length > 0 || compilation.HasCriticalErrors then
                failwithf "F# compiler reported errors in %s:\n%s" projPath errStr 
            Log.verbose "F# project %s compiled" projPath
            return FSharpCompilation(compilation, projPath) :> AbstractCompilation
        }
        |> Async.StartAsTask
        

/// <summary>F# project loader.</summary>
/// <param name="msbuildProps">MsBuild properties to set.</param>
type FSharpProjectLoader (msbuildProps: IDictionary<string,string>) =
    let checker = FSharpChecker.Create()

    interface IProjectLoader with
        member __.TryLoad(path, loader) =
            let ext = Path.GetExtension(path).ToLowerInvariant()
            if ext = ".fsproj" then
                FSharpProject(path, msbuildProps, loader, checker) :> AbstractProject
            else
                null

