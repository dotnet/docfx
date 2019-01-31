// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference.FSharp.Tests

open System
open System.IO
open System.Collections.Generic
open Xunit
open Xunit.Abstractions
open Microsoft.FSharp.Compiler.SourceCodeServices

open Microsoft.DocAsCode.Metadata.ManagedReference
open Microsoft.DocAsCode.Metadata.ManagedReference.FSharp
open Microsoft.DocAsCode.DataContracts.ManagedReference
open Microsoft.DocAsCode.DataContracts.Common

[<Collection("F# Test Collection")>]
type FSharpCompilationTests (output: ITestOutputHelper) =
    let printfn format = Printf.kprintf (fun msg -> output.WriteLine(msg)) format 

    let getProject projPath =
        let msBuildProps = Dictionary<string, string> ()
        let fsLoader = FSharpProjectLoader (msBuildProps)
        let loader = AbstractProjectLoader ([fsLoader])
        let checker = FSharpChecker.Create()    
        let proj = FSharpProject (projPath, msBuildProps, loader, checker)
        proj

    let getCompilation projPath =
        let proj = getProject projPath
        let comp = proj.GetCompilationAsync().Result
        comp :?> FSharpCompilation
        
    let getAllEntites (comp: FSharpCompilation) =
        let rec getSubEntites (ents: seq<FSharpEntity>) = seq {
            for ent in ents do
                yield ent
                yield! getSubEntites (ent.PublicNestedEntities)
        }
        getSubEntites comp.Compilation.AssemblySignature.Entities

    let getEntity (comp: FSharpCompilation) fullName =              
        let ent =
            getAllEntites comp
            |> Seq.tryFind (fun ent -> ent.TryFullName = Some fullName)
        match ent with
        | Some ent -> ent
        | None -> 
            printfn "Available entities:"
            for ent in getAllEntites comp do
                printfn "%A" ent.TryFullName
            failwithf "Cannot find entity %s in compilation %A" fullName comp.Compilation

    let apiParameterString (p: ApiParameter) =
        if p = null then "null"
        else sprintf "Name=%s Type=%A Descriptions=%s Attributes=%A" p.Name p.Type p.Description p.Attributes

    let syntaxDetailString (s: SyntaxDetail) =
        if s = null then "null"
        else
            sprintf "Content=%s TypeParameters=[%s] Parameters=[%s] Return=(%s)" 
                    s.Content.[SyntaxLanguage.FSharp] 
                    (if s.TypeParameters = null then "null" else s.TypeParameters |> Seq.map apiParameterString |> String.concat ";") 
                    (if s.Parameters = null then "null" else s.Parameters |> Seq.map apiParameterString |> String.concat ";") 
                    (apiParameterString s.Return) 

    let sourceDetailString (s: SourceDetail) =
        if s = null then "null"
        else
            sprintf "Name=%s Path=%s StartLine=%d" s.Name s.Path s.StartLine

    let metadataItemString (md: MetadataItem) =
        let s1 = 
            sprintf "MetadataItem: Name=%s CommentId=%s Language=%A DisplayNames=%s DisplayNamesWithType=%s DisplayQualifiedNames=%s "
                    md.Name md.CommentId md.Language md.DisplayNames.[SyntaxLanguage.FSharp] md.DisplayNamesWithType.[SyntaxLanguage.FSharp] md.DisplayQualifiedNames.[SyntaxLanguage.FSharp]
        let s2 =
            sprintf "Type=%A AssemblyNameList=%A NamespaceName=%s Source={%s} "
                    md.Type md.AssemblyNameList md.NamespaceName (sourceDetailString md.Source)
        let s3 =
            sprintf "Syntax={%s} Overload=%s Overridden=%s Inheritance=%A Implements=%A InhertitedMembers=%A Attributes=%A"
                    (syntaxDetailString md.Syntax) md.Overload md.Overridden md.Inheritance md.Implements md.InheritedMembers md.Attributes
        s1 + s2 + s3

    [<Fact>]
    let BuildController () =
        let comp = getCompilation "TestData/NetCoreProject/NetCoreProject.fsproj"
        let bc = comp.GetBuildController()
        ignore bc

    [<Fact>]
    let TripleSlashSummaryComment () =
        let comp = getCompilation "TestData/NetCoreProject/NetCoreProject.fsproj"

        let xmlDoc = """Only summary text"""
        let xmlDoc = List [xmlDoc]
        let xmlDocSig = "M:NetCoreProject.Module2.summaryDocTest"

        let md = MetadataItem()
        md.Source <- SourceDetail (Name="VirtualFile", Path="VirtualFile", StartLine=1)
        md.Syntax <- SyntaxDetail()

        comp.ExtractXmlDoc md xmlDoc xmlDocSig
        Assert.Equal (md.Summary, "Only summary text")

    [<Fact>]
    let TripleSlashXMLComment () =
        let comp = getCompilation "TestData/NetCoreProject/NetCoreProject.fsproj"

        let xmlDoc = """
            <summary>Summary text.</summary>
            <remarks>Remarks text.</remarks>
            <param name="param1">Param1 text.</param>
            <param name="param2">Param2 text.</param>
            <typeparam name="typeParam1">TypeParam1 text.</typeparam>
            <typeparam name="typeParam2">TypeParam2 text.</typeparam>
            <returns>Returns text.</returns>
            <exception cref="T:NetCoreProject.Module1.ExceptionInModule1">ExceptionInModule1 using comment id.</exception>
            <exception cref="NetCoreProject.Module1.ExceptionInModule1">ExceptionInModule1 using global type syntax.</exception>
            <exception cref="ExceptionInModule2">ExceptionInModule2 using local type syntax.</exception>
            <exception cref="UnresolvedException">Unresolved exception.</exception>
            <seealso cref="M:NetCoreProject.Module1.Type1.Method1">Method1 using comment id.</seealso>
            <seealso cref="NetCoreProject.Module1.Type1.Method1">Method1 using global syntax.</seealso>
            <seealso cref="Type2.Method2">Method2 using local syntax.</seealso>
            <seealso cref="UnresolvedSeealso">Unresolved see also.</seealso>
            """
        let xmlDoc = List [xmlDoc]
        let xmlDocSig = "M:NetCoreProject.Module2.xmlDocTest"

        let md = MetadataItem()
        md.Source <- SourceDetail (Name="VirtualFile", Path="VirtualFile", StartLine=1)
        md.Syntax <- SyntaxDetail()
        let param1 = ApiParameter (Name="param1")
        let param2 = ApiParameter (Name="param2")
        md.Syntax.Parameters <- List [param1; param2]
        let typeParam1 = ApiParameter (Name="typeParam1")
        let typeParam2 = ApiParameter (Name="typeParam2")
        md.Syntax.TypeParameters <- List [typeParam1; typeParam2]
        md.Syntax.Return <- ApiParameter ()

        comp.ExtractXmlDoc md xmlDoc xmlDocSig

        Assert.Equal (md.Summary, "Summary text.")
        Assert.Equal (md.Remarks, "Remarks text.")
        Assert.Equal (md.Syntax.Parameters.[0].Description, "Param1 text.")
        Assert.Equal (md.Syntax.Parameters.[1].Description, "Param2 text.")
        Assert.Equal (md.Syntax.TypeParameters.[0].Description, "TypeParam1 text.")        
        Assert.Equal (md.Syntax.TypeParameters.[1].Description, "TypeParam2 text.")                
        Assert.Equal (md.Syntax.Return.Description, "Returns text.")

        for excp in md.Exceptions do
            printfn "exception Type=%s  Description=%s  CommentId=%s" excp.Type excp.Description excp.CommentId

        Assert.Equal (md.Exceptions.Count, 4)
        Assert.Equal (md.Exceptions.[0].Type, "NetCoreProject.Module1.ExceptionInModule1")
        Assert.Equal (md.Exceptions.[0].Description, "ExceptionInModule1 using comment id.")
        Assert.Equal (md.Exceptions.[0].CommentId, "T:NetCoreProject.Module1.ExceptionInModule1")
        Assert.Equal (md.Exceptions.[1].Type, "NetCoreProject.Module1.ExceptionInModule1")
        Assert.Equal (md.Exceptions.[1].Description, "ExceptionInModule1 using global type syntax.")
        Assert.Equal (md.Exceptions.[1].CommentId, "T:NetCoreProject.Module1.ExceptionInModule1")
        Assert.Equal (md.Exceptions.[2].Type, "NetCoreProject.Module2.ExceptionInModule2")
        Assert.Equal (md.Exceptions.[2].Description, "ExceptionInModule2 using local type syntax.")
        Assert.Equal (md.Exceptions.[2].CommentId, "T:NetCoreProject.Module2.ExceptionInModule2")   
        Assert.Equal (md.Exceptions.[3].Type, "UnresolvedException")
        Assert.Equal (md.Exceptions.[3].Description, "Unresolved exception.")
        Assert.True (String.IsNullOrEmpty md.Exceptions.[3].CommentId)

        for seealso in md.SeeAlsos do
            printfn "seealso LinkId=%s  LinkType=%A  CommentId=%s  AltText=%s" 
                    seealso.LinkId seealso.LinkType seealso.CommentId seealso.AltText

        Assert.Equal (md.SeeAlsos.Count, 4)
        Assert.Equal (md.SeeAlsos.[0].LinkId, "NetCoreProject.Module1.Type1.Method1(unit)")
        Assert.Equal (md.SeeAlsos.[0].LinkType, LinkType.CRef)
        Assert.Equal (md.SeeAlsos.[0].CommentId, "M:NetCoreProject.Module1.Type1.Method1")
        Assert.Equal (md.SeeAlsos.[0].AltText, "Method1 using comment id.")
        Assert.Equal (md.SeeAlsos.[1].LinkId, "NetCoreProject.Module1.Type1.Method1(unit)")
        Assert.Equal (md.SeeAlsos.[1].LinkType, LinkType.CRef)
        Assert.Equal (md.SeeAlsos.[1].CommentId, "M:NetCoreProject.Module1.Type1.Method1")        
        Assert.Equal (md.SeeAlsos.[1].AltText, "Method1 using global syntax.")
        Assert.Equal (md.SeeAlsos.[2].LinkId, "NetCoreProject.Module2.Type2.Method2(string)")
        Assert.Equal (md.SeeAlsos.[2].LinkType, LinkType.CRef)
        Assert.Equal (md.SeeAlsos.[2].CommentId, "M:NetCoreProject.Module2.Type2.Method2(System.String)")        
        Assert.Equal (md.SeeAlsos.[2].AltText, "Method2 using local syntax.")        
        Assert.Equal (md.SeeAlsos.[3].LinkId, "UnresolvedSeealso")
        Assert.Equal (md.SeeAlsos.[3].LinkType, LinkType.CRef)
        Assert.True (String.IsNullOrEmpty md.SeeAlsos.[3].CommentId)
        Assert.Equal (md.SeeAlsos.[3].AltText, "Unresolved see also.")        

    [<Fact>]
    let Module1Metadata () =
        let comp = getCompilation "TestData/NetCoreProject/NetCoreProject.fsproj"
        
        let ent = getEntity comp "NetCoreProject.Module1"  
        let md = comp.EntityMetadata ent |> Option.get
        printfn "mod: %s" (metadataItemString md)

        Assert.Equal (md.Name, "NetCoreProject.Module1")
        Assert.Equal (md.CommentId, "T:NetCoreProject.Module1")
        Assert.Equal (md.Language, SyntaxLanguage.FSharp)
        Assert.Equal (md.DisplayNames.[SyntaxLanguage.FSharp], "Module1 (mod)")
        Assert.Equal (md.DisplayNamesWithType.[SyntaxLanguage.FSharp], "NetCoreProject.Module1")
        Assert.Equal (md.DisplayQualifiedNames.[SyntaxLanguage.FSharp], "NetCoreProject.Module1")
        Assert.Equal (md.Type, MemberType.Class)
        Assert.Equal (md.AssemblyNameList, ["NetCoreProject"])
        Assert.Equal (md.NamespaceName, "NetCoreProject")
        Assert.Equal (md.Source.Path, Path.GetFullPath "TestData/NetCoreProject/Module1.fs")
        Assert.Equal (md.Syntax.Content.[SyntaxLanguage.FSharp], "module Module1")
        Assert.Equal (md.Summary.Trim(), "Module1 summary.")
        Assert.Null (md.Syntax.Return)

    [<Fact>]
    let Module1Func2Metadata () =
        let comp = getCompilation "TestData/NetCoreProject/NetCoreProject.fsproj"               
        let encEnt = getEntity comp "NetCoreProject.Module1"  
        let encMd = MetadataItem(Name=encEnt.FullName)
        let sym = encEnt.MembersFunctionsAndValues |> Seq.find (fun s -> s.DisplayName = "Func2")
        let md = comp.SymbolMetadata encEnt encMd sym |> Option.get       
        printfn "func: %s" (metadataItemString md)        

        Assert.Equal (md.Name, "NetCoreProject.Module1.Func2(string -> int)")
        // TODO: F# compiler returns wrong CommentId without module.
        Assert.Equal (md.CommentId, "M:NetCoreProject.Func2(System.String,System.Int32)") 
        Assert.Equal (md.Language, SyntaxLanguage.FSharp)
        Assert.Equal (md.DisplayNames.[SyntaxLanguage.FSharp], "val Func2: string -> int -> string * int option")
        Assert.Equal (md.DisplayNamesWithType.[SyntaxLanguage.FSharp], "val Module1.Func2: string -> int -> string * int option")
        Assert.Equal (md.DisplayQualifiedNames.[SyntaxLanguage.FSharp], "val NetCoreProject.Module1.Func2: string -> int -> string * int option")
        Assert.Equal (md.Type, MemberType.Method)
        Assert.Equal (md.Source.Path, Path.GetFullPath "TestData/NetCoreProject/Module1.fs")
        Assert.Equal (md.Syntax.Content.[SyntaxLanguage.FSharp], "val Func2: arg1:string -> arg2:int -> string * int option")
        Assert.Equal (md.Syntax.TypeParameters, [])
        Assert.Equal (md.Syntax.Parameters.Count, 2)        
        Assert.Equal (md.Syntax.Parameters.[0].Name, "arg1")        
        Assert.Equal (md.Syntax.Parameters.[0].Type, "TypeRef:Microsoft.FSharp.Core.string")        
        Assert.Equal (md.Syntax.Parameters.[0].Description, "arg1 text.")        
        Assert.Equal (md.Syntax.Parameters.[1].Name, "arg2")        
        Assert.Equal (md.Syntax.Parameters.[1].Type, "TypeRef:Microsoft.FSharp.Core.int")        
        Assert.Equal (md.Syntax.Parameters.[1].Description, "arg2 text.")                
        Assert.Equal (md.Syntax.Return.Type, "TypeRef:Microsoft.FSharp.Core.string * Microsoft.FSharp.Core.int Microsoft.FSharp.Core.option")        
        Assert.Equal (md.Syntax.Return.Description, "Returns text.")         
        Assert.Equal (md.Overload, "NetCoreProject.Module1.Func2*")        
        Assert.Equal (md.Attributes, [])   

    [<Fact>]
    let Module1Type1Metadata () =
        let comp = getCompilation "TestData/NetCoreProject/NetCoreProject.fsproj"
        
        let ent = getEntity comp "NetCoreProject.Module1.Type1"  
        let md = comp.EntityMetadata ent |> Option.get
        printfn "ent: %s" (metadataItemString md)

        Assert.Equal (md.Name, "NetCoreProject.Module1.Type1")
        Assert.Equal (md.CommentId, "T:NetCoreProject.Module1.Type1")
        Assert.Equal (md.Language, SyntaxLanguage.FSharp)
        Assert.Equal (md.DisplayNames.[SyntaxLanguage.FSharp], "Module1.Type1")
        Assert.Equal (md.DisplayNamesWithType.[SyntaxLanguage.FSharp], "NetCoreProject.Module1.Type1")
        Assert.Equal (md.DisplayQualifiedNames.[SyntaxLanguage.FSharp], "NetCoreProject.Module1.Type1")
        Assert.Equal (md.Type, MemberType.Class)
        Assert.Equal (md.AssemblyNameList, ["NetCoreProject"])
        Assert.Equal (md.NamespaceName, "NetCoreProject")
        Assert.Equal (md.Source.Path, Path.GetFullPath "TestData/NetCoreProject/Module1.fs")
        Assert.Equal (md.Syntax.Content.[SyntaxLanguage.FSharp], "type Module1.Type1 ()")
        Assert.Equal (md.Syntax.TypeParameters.Count, 0)
        Assert.Equal (md.Syntax.Parameters.Count, 0)
        Assert.Null (md.Syntax.Return)
        Assert.Equal (md.Inheritance, ["TypeRef:System.Object"])

    [<Fact>]
    let Module1Type1Method1Metadata () =
        let comp = getCompilation "TestData/NetCoreProject/NetCoreProject.fsproj"               
        let encEnt = getEntity comp "NetCoreProject.Module1.Type1"  
        let encMd = MetadataItem(Name=encEnt.FullName)
        let sym = encEnt.MembersFunctionsAndValues |> Seq.find (fun s -> s.DisplayName = "Method1")
        let md = comp.SymbolMetadata encEnt encMd sym |> Option.get       
        printfn "sym: %s" (metadataItemString md)        

        Assert.Equal (md.Name, "NetCoreProject.Module1.Type1.Method1(unit)")
        Assert.Equal (md.CommentId, "M:NetCoreProject.Module1.Type1.Method1")
        Assert.Equal (md.Language, SyntaxLanguage.FSharp)
        Assert.Equal (md.DisplayNames.[SyntaxLanguage.FSharp], "member Method1: unit -> unit")
        Assert.Equal (md.DisplayNamesWithType.[SyntaxLanguage.FSharp], "member Type1.Method1: unit -> unit")
        Assert.Equal (md.DisplayQualifiedNames.[SyntaxLanguage.FSharp], "member NetCoreProject.Module1.Type1.Method1: unit -> unit")
        Assert.Equal (md.Type, MemberType.Method)
        Assert.Equal (md.Source.Path, Path.GetFullPath "TestData/NetCoreProject/Module1.fs")
        Assert.Equal (md.Syntax.Content.[SyntaxLanguage.FSharp], "member Method1: unit -> unit")
        Assert.Equal (md.Syntax.TypeParameters, [])
        Assert.Equal (md.Syntax.Parameters, [])        
        Assert.Null (md.Syntax.Return)
        Assert.Equal (md.Overload, "NetCoreProject.Module1.Type1.Method1*")        
        Assert.Equal (md.Attributes, [])

    [<Fact>]
    let Module2Type2Method2Metadata () =
        let comp = getCompilation "TestData/NetCoreProject/NetCoreProject.fsproj"               
        let encEnt = getEntity comp "NetCoreProject.Module2.Type2"  
        let encMd = MetadataItem(Name=encEnt.FullName)
        let sym = encEnt.MembersFunctionsAndValues |> Seq.find (fun s -> s.DisplayName = "Method2")
        let md = comp.SymbolMetadata encEnt encMd sym |> Option.get       
        printfn "sym: %s" (metadataItemString md)        

        Assert.Equal (md.Name, "NetCoreProject.Module2.Type2.Method2(string)")
        Assert.Equal (md.CommentId, "M:NetCoreProject.Module2.Type2.Method2(System.String)")
        Assert.Equal (md.Language, SyntaxLanguage.FSharp)
        Assert.Equal (md.DisplayNames.[SyntaxLanguage.FSharp], "member Method2: string -> string")
        Assert.Equal (md.DisplayNamesWithType.[SyntaxLanguage.FSharp], "member Type2.Method2: string -> string")
        Assert.Equal (md.DisplayQualifiedNames.[SyntaxLanguage.FSharp], "member NetCoreProject.Module2.Type2.Method2: string -> string")
        Assert.Equal (md.Type, MemberType.Method)
        Assert.Equal (md.Source.Path, Path.GetFullPath "TestData/NetCoreProject/Module2.fs")
        Assert.Equal (md.Syntax.Content.[SyntaxLanguage.FSharp], "member Method2: arg1:string -> string")
        Assert.Equal (md.Syntax.TypeParameters, [])
        Assert.Equal (md.Syntax.Parameters.Count, 1)        
        Assert.Equal (md.Syntax.Parameters.[0].Name, "arg1")        
        Assert.Equal (md.Syntax.Parameters.[0].Type, "TypeRef:Microsoft.FSharp.Core.string")        
        Assert.Equal (md.Syntax.Parameters.[0].Description, "arg1 text.")        
        Assert.Equal (md.Syntax.Return.Type, "TypeRef:Microsoft.FSharp.Core.string")        
        Assert.Equal (md.Syntax.Return.Description, "Returns text.")        
        Assert.Equal (md.Summary, "Method2 summary.")
        Assert.Equal (md.Overload, "NetCoreProject.Module2.Type2.Method2*")        
        Assert.Equal (md.Attributes, [])

    [<Fact>]
    let Module2Type2Property2Metadata () =
        let comp = getCompilation "TestData/NetCoreProject/NetCoreProject.fsproj"               
        let encEnt = getEntity comp "NetCoreProject.Module2.Type2"  
        let encMd = MetadataItem(Name=encEnt.FullName)
        let sym = encEnt.MembersFunctionsAndValues |> Seq.find (fun s -> s.DisplayName = "Property2" && s.IsProperty)
        let md = comp.SymbolMetadata encEnt encMd sym |> Option.get       
        printfn "sym: %s" (metadataItemString md)        

        Assert.Equal (md.Name, "NetCoreProject.Module2.Type2.Property2(unit)")
        Assert.Equal (md.CommentId, "P:NetCoreProject.Module2.Type2.Property2")
        Assert.Equal (md.Language, SyntaxLanguage.FSharp)
        Assert.Equal (md.DisplayNames.[SyntaxLanguage.FSharp], "property Property2: Type1")
        Assert.Equal (md.DisplayNamesWithType.[SyntaxLanguage.FSharp], "property Type2.Property2: Type1")
        Assert.Equal (md.DisplayQualifiedNames.[SyntaxLanguage.FSharp], "property NetCoreProject.Module2.Type2.Property2: NetCoreProject.Module1.Type1")
        Assert.Equal (md.Type, MemberType.Property)
        Assert.Equal (md.Source.Path, Path.GetFullPath "TestData/NetCoreProject/Module2.fs")
        Assert.Equal (md.Syntax.Content.[SyntaxLanguage.FSharp], "property Property2: Type1 with get")
        Assert.Equal (md.Syntax.TypeParameters, [])
        Assert.Equal (md.Syntax.Parameters.Count, 0)        
        Assert.Equal (md.Syntax.Return.Type, "TypeRef:NetCoreProject.Module1.Type1")        
        Assert.Equal (md.Syntax.Return.Description, "Value text.")        
        Assert.Equal (md.Summary, "Summary for Property2.")
        Assert.Equal (md.Overload, "NetCoreProject.Module2.Type2.Property2*")        
        Assert.Equal (md.Attributes, [])

    [<Fact>]
    let Module2Type3Metadata () =
        let comp = getCompilation "TestData/NetCoreProject/NetCoreProject.fsproj"
        
        let ent = getEntity comp "NetCoreProject.Module2.Type3`1"  
        let md = comp.EntityMetadata ent |> Option.get
        printfn "ent: %s" (metadataItemString md)

        Assert.Equal (md.Name, "NetCoreProject.Module2.Type3`1")
        Assert.Equal (md.CommentId, "T:NetCoreProject.Module2.Type3`1")
        Assert.Equal (md.Language, SyntaxLanguage.FSharp)
        Assert.Equal (md.DisplayNames.[SyntaxLanguage.FSharp], "Module2.Type3<'G>")
        Assert.Equal (md.DisplayNamesWithType.[SyntaxLanguage.FSharp], "NetCoreProject.Module2.Type3`1")
        Assert.Equal (md.DisplayQualifiedNames.[SyntaxLanguage.FSharp], "NetCoreProject.Module2.Type3`1")
        Assert.Equal (md.Type, MemberType.Class)
        Assert.Equal (md.AssemblyNameList, ["NetCoreProject"])
        Assert.Equal (md.NamespaceName, "NetCoreProject")
        Assert.Equal (md.Source.Path, Path.GetFullPath "TestData/NetCoreProject/Module2.fs")
        Assert.Equal (md.Syntax.Content.[SyntaxLanguage.FSharp], "type Module2.Type3<'G> (arg1:'G)")
        Assert.Equal (md.Syntax.TypeParameters.Count, 1)
        Assert.Equal (md.Syntax.TypeParameters.[0].Name, "'G")        
        Assert.True (String.IsNullOrEmpty md.Syntax.TypeParameters.[0].Type)        
        Assert.Equal (md.Syntax.TypeParameters.[0].Description, "Generic text.")                
        Assert.Equal (md.Syntax.Parameters.Count, 1)
        Assert.Equal (md.Syntax.Parameters.[0].Name, "arg1")        
        Assert.Equal (md.Syntax.Parameters.[0].Type, "TypeRef:'G")        
        Assert.Equal (md.Syntax.Parameters.[0].Description, "arg1 text.")                
        Assert.Null (md.Syntax.Return)
        Assert.Equal (md.Inheritance, ["TypeRef:System.Object"])

    [<Fact>]
    let Module2Type3Property1Metadata () =
        let comp = getCompilation "TestData/NetCoreProject/NetCoreProject.fsproj"               
        let encEnt = getEntity comp "NetCoreProject.Module2.Type3`1"  
        let encMd = MetadataItem(Name=encEnt.FullName)
        let sym = encEnt.MembersFunctionsAndValues |> Seq.find (fun s -> s.DisplayName = "Property1" && s.IsProperty)
        let md = comp.SymbolMetadata encEnt encMd sym |> Option.get       
        printfn "sym: %s" (metadataItemString md)        

        Assert.Equal (md.Name, "NetCoreProject.Module2.Type3`1.Property1(unit)")
        Assert.Equal (md.CommentId, "P:NetCoreProject.Module2.Type3`1.Property1")
        Assert.Equal (md.Language, SyntaxLanguage.FSharp)
        Assert.Equal (md.DisplayNames.[SyntaxLanguage.FSharp], "property Property1: 'G")
        Assert.Equal (md.DisplayNamesWithType.[SyntaxLanguage.FSharp], "property Type3.Property1: 'G")
        Assert.Equal (md.DisplayQualifiedNames.[SyntaxLanguage.FSharp], "property NetCoreProject.Module2.Type3.Property1: 'G")
        Assert.Equal (md.Type, MemberType.Property)
        Assert.Equal (md.Source.Path, Path.GetFullPath "TestData/NetCoreProject/Module2.fs")
        Assert.Equal (md.Syntax.Content.[SyntaxLanguage.FSharp], "property Property1: 'G with get, set")
        Assert.Equal (md.Syntax.TypeParameters, [])
        Assert.Equal (md.Syntax.Parameters.Count, 0)        
        Assert.Equal (md.Syntax.Return.Type, "TypeRef:'G")        
        Assert.Equal (md.Summary.Trim(), "Property1 summary.")
        Assert.Equal (md.Overload, "NetCoreProject.Module2.Type3`1.Property1*")        
        Assert.Equal (md.Attributes, [])

    [<Fact>]
    let Module2Type3CtorMetadata () =
        let comp = getCompilation "TestData/NetCoreProject/NetCoreProject.fsproj"               
        let encEnt = getEntity comp "NetCoreProject.Module2.Type3`1"  
        let encMd = comp.EntityMetadata encEnt |> Option.get
        let sym = encEnt.MembersFunctionsAndValues |> Seq.find (fun s -> s.IsConstructor)
        let md = comp.SymbolMetadata encEnt encMd sym |> Option.get       
        printfn "sym: %s" (metadataItemString md)        

        Assert.Equal (md.Name, "NetCoreProject.Module2.Type3`1.#ctor('G)")
        Assert.Equal (md.CommentId, "M:NetCoreProject.Module2.Type3`1.#ctor(`0)")
        Assert.Equal (md.Language, SyntaxLanguage.FSharp)
        Assert.Equal (md.DisplayNames.[SyntaxLanguage.FSharp], "new: 'G -> Type3<'G>")
        Assert.Equal (md.DisplayNamesWithType.[SyntaxLanguage.FSharp], "new: 'G -> Type3<'G>")
        Assert.Equal (md.DisplayQualifiedNames.[SyntaxLanguage.FSharp], "new: 'G -> NetCoreProject.Module2.Type3<'G>")
        Assert.Equal (md.Type, MemberType.Constructor)
        Assert.Equal (md.Source.Path, Path.GetFullPath "TestData/NetCoreProject/Module2.fs")
        Assert.Equal (md.Syntax.Content.[SyntaxLanguage.FSharp], "new: arg1:'G -> Type3<'G>")
        Assert.Equal (md.Syntax.TypeParameters, [])
        Assert.Equal (md.Syntax.Parameters.Count, 1)        
        Assert.Equal (md.Syntax.Parameters.[0].Name, "arg1")        
        Assert.Equal (md.Syntax.Parameters.[0].Type, "TypeRef:'G")        
        Assert.Equal (md.Syntax.Parameters.[0].Description, "arg1 text.")       
        Assert.Equal (md.Syntax.Return.Type, "TypeRef:NetCoreProject.Module2.Type3`1<'G>")        
        Assert.Equal (md.Summary.Trim(), "Implicit constructor.")
        Assert.Equal (md.Overload, "NetCoreProject.Module2.Type3`1.#ctor*")        
        Assert.Equal (md.Attributes, [])

    [<Fact>]
    let NamespaceMetadata () =
        let comp = getCompilation "TestData/NetCoreProject/NetCoreProject.fsproj"               
        let md = comp.NamespaceMetadata "TestNamespace" Seq.empty |> Option.get
        printfn "ns: %s" (metadataItemString md)             

        Assert.Equal (md.Name, "TestNamespace")
        Assert.Equal (md.CommentId, "N:TestNamespace")        
        Assert.Equal (md.DisplayNames.[SyntaxLanguage.FSharp], "TestNamespace")
        Assert.Equal (md.DisplayNamesWithType.[SyntaxLanguage.FSharp], "TestNamespace")
        Assert.Equal (md.DisplayQualifiedNames.[SyntaxLanguage.FSharp], "TestNamespace")
        Assert.Equal (md.AssemblyNameList, ["NetCoreProject"])        
        Assert.Equal (md.Type, MemberType.Namespace)

    [<Fact>]
    let AssemblyMetadata () =
        let comp = getCompilation "TestData/NetCoreProject/NetCoreProject.fsproj"               
        let md = comp.AssemblyMetadata()
        printfn "asm: %s" (metadataItemString md)             

        Assert.Equal (md.Name, "NetCoreProject")
        Assert.Equal (md.Language, SyntaxLanguage.FSharp)
        Assert.Equal (md.DisplayNames.[SyntaxLanguage.FSharp], "NetCoreProject")
        Assert.Equal (md.DisplayNamesWithType.[SyntaxLanguage.FSharp], "NetCoreProject")
        Assert.Equal (md.DisplayQualifiedNames.[SyntaxLanguage.FSharp], "NetCoreProject")
        Assert.Equal (md.Type, MemberType.Assembly)

    [<Fact>]
    let ExtractMetadata () =
        let comp = getCompilation "TestData/NetCoreProject/NetCoreProject.fsproj"               
        let bc = comp.GetBuildController() 
        let ips = 
            { new IInputParameters with
                member __.Options = ExtractMetadataOptions()
                member __.Files = Seq.empty
                member __.HasChanged _ = true
                member __.Key = ""
                member __.Cache = null
                member __.BuildInfo = null
            }
        let md = bc.ExtractMetadata ips
        ignore md

