// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference.FSharp
open System
open System.IO
open System.Collections.Generic
open System.Text.RegularExpressions
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.DocAsCode.Metadata.ManagedReference
open Microsoft.DocAsCode.DataContracts.ManagedReference
open Microsoft.DocAsCode.DataContracts.Common


/// List extensions
module List =

    /// Concatenates a list of lists with the specified separator. 
    let rec concatWith sep lst =
        match lst with
        | f::s::t -> f @ sep @ concatWith sep (s::t)
        | [l] -> l
        | [] -> []


/// The names associated with an F# symbol or entity.
/// Useful for generating a MetadataItem or LinkItem.
type private SymbolNames = {
    Name:                   string
    DisplayName:            string
    DisplayNameWithType:    string
    DisplayQualifiedName:   string
    DisplaySuffix:          string option   
    XmlDocSig:              string
} with
    member this.DisplayNameWithSuffix =
        match this.DisplaySuffix with
        | Some suffix -> this.DisplayName + suffix
        | None -> this.DisplayName


/// <summary>An F# compilation of an F# project. Can extract metadata from the F# project.</summary>
/// <param name="compilation">The compilation output from the F# compiler service.</param>
/// <param name="projPath">The path to the F# project file.</param>
type FSharpCompilation (compilation: FSharpCheckProjectResults, projPath: string) =
    inherit AbstractCompilation()

    /// name of this assembly 
    let assemblyName, assemblyQualifiedName =
        match Seq.tryHead compilation.AssemblySignature.Entities with
        | Some ent when ent.Assembly.QualifiedName.Length > 0 ->
            ent.Assembly.SimpleName, ent.Assembly.QualifiedName
        | Some ent ->
            ent.Assembly.SimpleName, ent.Assembly.SimpleName
        | None ->
            // guess assembly name if it is empty
            let filename = Path.GetFileNameWithoutExtension projPath
            filename, filename

    /// all (nested) modules within the specified entity
    let rec containedModules (ents: seq<FSharpEntity>) = seq {
        for ent in ents do
            if ent.IsFSharpModule then
                yield ent
                yield! containedModules ent.NestedEntities        
    }

    /// all (nested) modules within this F# compilation
    let allModules =
        containedModules compilation.AssemblySignature.Entities

    /// all (nested) namespaces within the specified entity
    let rec containedNamespaces (ents: seq<FSharpEntity>) = seq {
        for ent in ents do
            match ent.Namespace with
            | Some ns -> yield ns
            | None -> ()
            yield! containedNamespaces ent.PublicNestedEntities
    }

    /// namespaces defined within this assembly
    let allNamespaces = 
        containedNamespaces compilation.AssemblySignature.Entities
        |> Set.ofSeq

    /// references used within this assembly
    let mutable references = Dictionary<string, ReferenceItem>()

    /// visibility filter
    let mutable filter = ConfigFilterRule()

    /// adds a reference
    let addReference name (ref: ReferenceItem) =
        if references.ContainsKey name then
            references.[name].Merge ref
        else
            references.[name] <- ref

    /// Adds a reference for the specified MetadataItem.
    let addReferenceFromMetadata (md: MetadataItem) =
        let refParts = 
            [LinkItem(Name=md.Name,
                      DisplayName=md.DisplayNames.[SyntaxLanguage.FSharp],
                      DisplayNamesWithType=md.DisplayNamesWithType.[SyntaxLanguage.FSharp],
                      DisplayQualifiedNames=md.DisplayQualifiedNames.[SyntaxLanguage.FSharp])]
        let ref = ReferenceItem(CommentId=md.CommentId,
                                Parts=SortedList(Map[SyntaxLanguage.FSharp, List(refParts)]))            
        addReference md.Name ref

    /// Creates a SourceDetail from an F# compiler range.
    let srcDetail name (declLoc: Range.range) =
        SourceDetail(Name=name, Path=declLoc.FileName, StartLine=declLoc.StartLine)
        
    /// LinkItems for an F# type reference.
    let rec typeRefParts (typ: FSharpType) : LinkItem list =
        let postfixTypes = ["option"; "list"]
        let literal s = LinkItem(DisplayName=s, DisplayNamesWithType=s, DisplayQualifiedNames=s)

        if typ.HasTypeDefinition then
            let td = typ.TypeDefinition
            let trimmedAp =
                if td.AccessPath.StartsWith("Microsoft.FSharp.Core", StringComparison.Ordinal) then ""
                elif td.AccessPath.StartsWith("Microsoft.FSharp.Collections", StringComparison.Ordinal) then ""
                elif td.AccessPath.StartsWith("Microsoft.FSharp", StringComparison.Ordinal) then td.AccessPath.["Microsoft.".Length..] + "."
                else td.AccessPath + "."
            let isPostfix = 
                (postfixTypes |> List.contains (trimmedAp + td.DisplayName)) || td.IsArrayType || td.IsByRef
            let baseRef = 
                if td.IsArrayType then ["[" + String.replicate (td.ArrayRank-1) "," + "]" |> literal]
                else
                    let name =
                        if td.IsFSharpAbbreviation then td.AccessPath + "." + td.DisplayName
                        elif td.IsByRef then "byref"
                        else td.FullName
                    [LinkItem(Name=name, DisplayName=td.DisplayName, DisplayNamesWithType=td.DisplayName,
                              DisplayQualifiedNames=(if isPostfix then td.DisplayName else trimmedAp + td.DisplayName))]
            let genRef = 
                typ.GenericArguments
                |> Seq.filter (fun ga -> not ga.HasTypeDefinition || ga.TypeDefinition.DisplayName <> "MeasureOne")
                |> Seq.map typeRefParts
                |> List.ofSeq
                |> List.concatWith [literal ","]
            if Seq.isEmpty genRef then baseRef
            elif isPostfix then genRef @ [literal " "] @ baseRef
            else baseRef @ [literal "<"] @ genRef @ [literal ">"]
        elif typ.IsFunctionType then
            match List.ofSeq typ.GenericArguments with
            | [first; second] ->
                let bl, br = if first.IsFunctionType then [literal "("], [literal ")"] else [], []
                bl @ typeRefParts first @ br @ [literal " -> "] @ typeRefParts second
            | _ ->
                failwithf "expected two generic arguments for function type but got %A" typ.GenericArguments
        elif typ.IsGenericParameter then
            let marker = if typ.GenericParameter.IsSolveAtCompileTime then "^" else "'"
            [literal (marker + typ.GenericParameter.Name)]
        elif typ.IsTupleType then
            typ.GenericArguments |> Seq.map typeRefParts |> List.ofSeq |> List.concatWith [literal " * "]
        else            
            // TODO: fix product measures
            Log.warning "typeRefParts: unidentifiable %A" typ
            [literal (sprintf "%A" typ)] 
       
    /// A reference to an F# type.
    let typeRef (typ: FSharpType) =
        let parts = typeRefParts typ
        let name = 
            parts 
            |> List.map (fun li -> if li.Name <> null then li.Name else li.DisplayQualifiedNames) 
            |> String.concat ""
        // Parts must consist of more than one element, otherwise the link name is not honored.
        let parts =
            if List.length parts > 1 then parts
            else [LinkItem()] @ parts @ [LinkItem()]
        let ri = ReferenceItem(Parts=SortedList(Map[SyntaxLanguage.FSharp, List(parts);
                                                    SyntaxLanguage.CSharp, List(parts)]))
        // References to types must not have the name of the type they reference, as this would clash with the 
        // references for the TOC, that use special display names including the containing module.
        let refName = "TypeRef:" + name
        addReference refName ri
        refName       

    /// F# syntax for an F# type.
    let typeSyntax fullTypes (typ: FSharpType) =
        typeRefParts typ
        |> List.map (fun li -> if fullTypes then li.DisplayQualifiedNames else li.DisplayName)
        |> String.concat ""

    /// F# syntax for a parameter.
    let paramSyntax withNames fullTypes (p: FSharpParameter) =
        if withNames && p.DisplayName.Length > 0 then
            sprintf "%s:%s" p.DisplayName (typeSyntax fullTypes p.Type)    
        else
            typeSyntax fullTypes p.Type

    /// F# syntax for a parameter group.
    let paramGroupSyntax withNames fullTypes (pg: IList<FSharpParameter>) =
        match List.ofSeq pg with
        | [] -> "unit"
        | [p] -> paramSyntax withNames fullTypes p
        | pg -> pg |> List.map (paramSyntax withNames fullTypes) |> String.concat " * "

    /// F# syntax for curried parameter groups.
    let curriedParamSyntax withNames fullTypes (cpgs: IList<IList<FSharpParameter>>) =
        cpgs |> Seq.map (paramGroupSyntax withNames fullTypes) |> String.concat " -> "

    let substGenericParameters (text: string) =
        Regex.Matches(text, @"\?\d+")
        |> Seq.cast<Match>
        |> Seq.map (fun m -> m.Value)
        |> Seq.distinct
        |> Seq.mapFold (fun alias name -> (name, sprintf "%c" alias), alias + char 1) 'a'
        |> fst
        |> Seq.fold (fun (text: string) (orig, repl) -> text.Replace(orig, repl)) text

    /// Return value as F# and default syntax language item.
    let syn value =
        SortedList(Map[SyntaxLanguage.FSharp, value
                       SyntaxLanguage.Default, value])

    /// Full name of an F# type.
    let typeFullName (t: FSharpType) =
        if t.IsAbbreviation then
            t.TypeDefinition.AccessPath + "." + t.TypeDefinition.DisplayName
        else
            t.TypeDefinition.FullName

    /// Qualified name of an F# entity.
    let entityQualifiedName (ent: FSharpEntity) =
        if ent.IsFSharpAbbreviation then
            let nsPart =
                allNamespaces
                |> Seq.filter ent.AccessPath.StartsWith
                |> Seq.maxBy (fun ns -> ns.Length)
            let modPart = ent.AccessPath.[nsPart.Length+1..]
            sprintf "%s.%s+%s" nsPart (modPart.Replace('.', '+')) ent.DisplayName
        else
            ent.QualifiedName

    /// Namespace of the specified F# entity.
    let entityNamespace (ent: FSharpEntity) =
        match ent.Namespace with
        | Some ns -> ns
        | None ->
            let pos = (entityQualifiedName ent).LastIndexOf('.')
            if pos = -1 then ""
            else (entityQualifiedName ent).[0 .. pos-1]

    /// Resolve module path, i.e. remove "Module"-suffix if necessary.                
    let rec resolveModulePath fullName =
        match allModules |> Seq.tryFind (fun ent -> ent.IsFSharpModule && ent.FullName = fullName) with
        | Some ent ->
            let ns = entityNamespace ent
            if ent.AccessPath.Length > ns.Length then
                resolveModulePath ent.AccessPath + "." + ent.DisplayName
            else
                ns + "." + ent.DisplayName
        | None -> fullName

    /// Syntax string for the fields of an F# union case.
    let unionCaseFieldsSyntax fullType (cfs: seq<FSharpField>) = 
        cfs
        |> Seq.map (fun cf -> 
            if cf.Name.Length > 0 && cf.Name <> "Item" then
                sprintf "%s: %s" cf.Name (typeSyntax fullType cf.FieldType)
            else
                typeSyntax fullType cf.FieldType)
        |> String.concat " * "
        |> fun s -> if s.Length > 0 then " of " + s else ""

    /// Generates the names of an F# entity used for a MetadataItem and LinkItem.
    let entityNames (ent: FSharpEntity) : SymbolNames =
        let name =
            if ent.IsFSharpAbbreviation then ent.AccessPath + "." + ent.DisplayName
            else ent.FullName    
        let genStr =
            ent.GenericParameters
            |> Seq.map (fun gp -> "'" + gp.Name)
            |> String.concat ", "
            |> fun s -> if s.Length > 0 then "<" + s + ">" else s
        let displayGenName = ent.DisplayName + genStr
        let ns = entityNamespace ent           
        let dispName =
            if ent.AccessPath.Length > ns.Length then
                // entity is located within an F# module
                let ap = resolveModulePath ent.AccessPath
                ap.[ns.Length+1..] + "." + displayGenName
            else
                displayGenName                    
        let dispSuffix =
            if ent.IsFSharpModule then Some " (mod)"
            elif ent.IsFSharpRecord then Some " (rec)"
            elif ent.IsFSharpUnion then Some " (union)"
            elif ent.IsEnum then None
            elif ent.IsFSharpExceptionDeclaration then None
            elif ent.IsFSharpAbbreviation then Some " (abrv)"
            elif ent.IsMeasure then Some " (meas)"
            elif ent.IsDelegate then None
            elif ent.IsClass then None
            elif ent.IsInterface then None
            elif ent.IsValueType then None
            else None
        {
            Name = name     
            DisplayName = dispName 
            DisplayNameWithType = name 
            DisplayQualifiedName = name
            DisplaySuffix = dispSuffix            
            XmlDocSig = ent.XmlDocSig                  
        }    

    /// Generates the names of an F# symbol used for a MetadataItem and LinkItem.
    let symbolNames (sym: FSharpSymbol) (encEnt: FSharpEntity) : SymbolNames =
        let dispName = sym.DisplayName
        let nameWithType = encEnt.DisplayName + "." + sym.DisplayName
        let fullName = sym.FullName
        let encEntName = (entityNames encEnt).Name

        match sym with
        | :? FSharpField as field ->   
            {
                Name = encEntName + "." + sym.DisplayName     
                DisplayName = sprintf "val %s: %s" dispName (typeSyntax false field.FieldType) 
                DisplayNameWithType = sprintf "val %s: %s" nameWithType (typeSyntax false field.FieldType)
                DisplayQualifiedName = sprintf "val %s: %s" fullName (typeSyntax true field.FieldType) 
                DisplaySuffix = None
                XmlDocSig = field.XmlDocSig
            }
        | :? FSharpUnionCase as case ->
            {
                Name = encEntName + "." + sym.DisplayName     
                DisplayName = sprintf "%s%s" dispName (unionCaseFieldsSyntax false case.UnionCaseFields) 
                DisplayNameWithType = sprintf "%s%s" nameWithType (unionCaseFieldsSyntax false case.UnionCaseFields) 
                DisplayQualifiedName = sprintf "%s%s" fullName (unionCaseFieldsSyntax true case.UnionCaseFields)                
                DisplaySuffix = None
                XmlDocSig = case.XmlDocSig
            }
        | :? FSharpMemberOrFunctionOrValue as mem ->
            let logicalName = 
                mem.LogicalEnclosingEntity.AccessPath + "." + (entityNames mem.LogicalEnclosingEntity).DisplayName
            let iasName =
                if mem.IsOverrideOrExplicitInterfaceImplementation then 
                    match Seq.tryHead mem.ImplementedAbstractSignatures with
                    | Some ias -> ias.DeclaringType.TypeDefinition.FullName + "." 
                    | _ -> ""
                 else ""                
            let baseName = 
                encEntName + "." + 
                (if mem.IsExtensionMember then "___" + logicalName + "." else "") +
                iasName +
                (if mem.IsConstructor then "#ctor" else mem.DisplayName)
            let name =
                if mem.CompiledName.StartsWith("op_Explicit") then
                    encEntName + "->" + (typeSyntax true mem.ReturnParameter.Type)
                else
                    baseName + "(" + curriedParamSyntax false true mem.CurriedParameterGroups + ")"                
            let nonIndexProp = mem.IsProperty && mem.CurriedParameterGroups.[0].Count = 0
            let arrow = if mem.CurriedParameterGroups.Count > 0 && not nonIndexProp then " -> " else ""
            let dispParams = if nonIndexProp then "" else curriedParamSyntax false false mem.CurriedParameterGroups
            let fullParams = if nonIndexProp then "" else curriedParamSyntax false true mem.CurriedParameterGroups
            let dispType = dispParams + arrow + (typeSyntax false mem.ReturnParameter.Type)
            let fullType = fullParams + arrow + (typeSyntax true mem.ReturnParameter.Type)
            let mods = 
                [if not mem.IsInstanceMember then yield "static "
                 if mem.IsDispatchSlot then yield "abstract "]
                |> String.concat ""            
            let ifType fullType = 
                if mem.IsOverrideOrExplicitInterfaceImplementation then
                    match Seq.tryHead mem.ImplementedAbstractSignatures with
                    | Some ias when ias.DeclaringType.TypeDefinition.IsInterface -> 
                        sprintf "interface %s with " (typeSyntax fullType ias.DeclaringType)
                    | Some ias when ias.DeclaringType.TypeDefinition.FullName = encEnt.FullName -> 
                        "default "
                    | Some _ -> "override "
                    | None -> ""
                else ""
            let isEvent = 
                mem.Attributes |> Seq.exists (fun a -> a.AttributeType.DisplayName = "CLIEventAttribute")
            let ifDispType = ifType false
            let ifFullType = ifType true
            if mem.IsConstructor then         
                {
                    Name = name
                    DisplayName = sprintf "new: %s" dispType 
                    DisplayNameWithType = sprintf "new: %s" dispType 
                    DisplayQualifiedName = sprintf "new: %s" fullType 
                    DisplaySuffix = None
                    XmlDocSig = mem.XmlDocSig
                }
            elif mem.IsExtensionMember then 
                {
                    Name = name
                    DisplayName = sprintf "extension %s.%s: %s" logicalName dispName dispType 
                    DisplayNameWithType = sprintf "extension %s.%s: %s" logicalName dispName dispType 
                    DisplayQualifiedName = sprintf "extension %s: %s" mem.FullName fullType 
                    DisplaySuffix = None
                    XmlDocSig = mem.XmlDocSig
                }           
            elif isEvent then
                {
                    Name = name
                    DisplayName = sprintf "%s%sevent %s: %s" ifDispType mods dispName dispType
                    DisplayNameWithType = sprintf "%s%sevent %s: %s" ifDispType mods nameWithType dispType 
                    DisplayQualifiedName = sprintf "%s%sevent %s: %s" ifFullType mods fullName fullType
                    DisplaySuffix = None
                    XmlDocSig = mem.XmlDocSig
                }                         
            elif mem.IsProperty then
                {
                    Name = name
                    DisplayName = sprintf "%s%sproperty %s: %s" ifDispType mods dispName dispType
                    DisplayNameWithType = sprintf "%s%sproperty %s: %s" ifDispType mods nameWithType dispType 
                    DisplayQualifiedName = sprintf "%s%sproperty %s: %s" ifFullType mods fullName fullType
                    DisplaySuffix = None
                    XmlDocSig = mem.XmlDocSig
                }        
            elif mem.IsMember then
                {
                    Name = name
                    DisplayName = sprintf "%s%smember %s: %s" ifDispType mods dispName dispType
                    DisplayNameWithType = sprintf "%s%smember %s: %s" ifDispType mods nameWithType dispType 
                    DisplayQualifiedName = sprintf "%s%smember %s: %s" ifFullType mods fullName fullType                    
                    DisplaySuffix = None
                    XmlDocSig = mem.XmlDocSig
                }       
            elif mem.IsActivePattern then
                if mem.ReturnParameter.Type.HasTypeDefinition && 
                   mem.ReturnParameter.Type.TypeDefinition.TryFullName 
                   |> Option.forall (fun fn -> fn.StartsWith "Microsoft.FSharp.Core.FSharpChoice") then            
                    {
                        Name = name                  
                        DisplayName = sprintf "val %s: %s" dispName dispParams 
                        DisplayNameWithType = sprintf "val %s: %s" nameWithType dispParams
                        DisplayQualifiedName = sprintf "val %s: %s" fullName fullParams    
                        DisplaySuffix = None
                        XmlDocSig = mem.XmlDocSig                    
                    }   
                else
                    {
                        Name = name                  
                        DisplayName = sprintf "val %s: %s" dispName dispType 
                        DisplayNameWithType = sprintf "val %s: %s" nameWithType dispType
                        DisplayQualifiedName = sprintf "val %s: %s" fullName fullType                       
                        DisplaySuffix = None
                        XmlDocSig = mem.XmlDocSig
                    }   
            elif mem.FullType.IsFunctionType then
                {
                    Name = name     
                    DisplayName = sprintf "val %s: %s" dispName dispType 
                    DisplayNameWithType = sprintf "val %s: %s" nameWithType dispType
                    DisplayQualifiedName = sprintf "val %s: %s" fullName fullType           
                    DisplaySuffix = None
                    XmlDocSig = mem.XmlDocSig
                }     
            else
                {
                    Name = name     
                    DisplayName = sprintf "val %s" dispName
                    DisplayNameWithType = sprintf "val %s: %s" nameWithType (typeSyntax false mem.FullType)
                    DisplayQualifiedName =  sprintf "val %s: %s" fullName (typeSyntax true mem.FullType)                   
                    DisplaySuffix = None
                    XmlDocSig = mem.XmlDocSig
                }     
        | _ -> failwithf "Unsupported symbol: %A" sym

    /// Returns the reference string to an F# entity or symbol.
    let symbolRef (names: SymbolNames) =
        let parts = [LinkItem(Name=names.Name, DisplayName=names.DisplayNameWithSuffix, DisplayNamesWithType=names.DisplayNameWithType,
                              DisplayQualifiedNames=names.DisplayQualifiedName)]
        let ri = ReferenceItem(Parts=SortedList(Map[SyntaxLanguage.FSharp, List(parts);
                                                    SyntaxLanguage.CSharp, List(parts)]))
        addReference names.Name ri
        names.Name              

    /// Returns a reference to a literal name (i.e. without linking to something).
    let literalRef (name: string) =
        let parts = [LinkItem(DisplayName=name, DisplayNamesWithType=name, DisplayQualifiedNames=name)]
        let ri = ReferenceItem(Parts=SortedList(Map[SyntaxLanguage.FSharp, List(parts);
                                                    SyntaxLanguage.CSharp, List(parts)]))
        addReference name ri
        name                                                    

    /// A sequence of the names of all symbols and entities within the specified F# entity.
    let rec enclosedSymbolNames (ent: FSharpEntity) = seq {
        yield entityNames ent
        if not ent.IsFSharpAbbreviation then
            for mem in ent.MembersFunctionsAndValues do
                if mem.Accessibility.IsPublic then
                    yield symbolNames mem ent
            for field in ent.FSharpFields do 
                if field.Accessibility.IsPublic then
                    yield symbolNames field ent
            for case in ent.UnionCases do 
                if case.Accessibility.IsPublic then
                    yield symbolNames case ent
        for subEnt in ent.PublicNestedEntities do
            yield! enclosedSymbolNames subEnt
    }

    /// A sequences of all symbol names within this compilation.
    let allSymbolNames =
        compilation.AssemblySignature.Entities
        |> Seq.collect enclosedSymbolNames
        |> Seq.cache

    /// Returns true, if specified F# attribute is allowed by attribute filter.
    let attrAllowedByFilter (attr: FSharpAttribute) =
        let filterData = SymbolFilterData()
        filterData.Id <- attr.AttributeType.FullName
        filterData.Kind <- Nullable ExtendedSymbolKind.Class
        filterData.Attributes <- Seq.empty
        filter.CanVisitAttribute filterData

    /// Extract MetadataItem for an F# attribute.
    let attrMetadata (attr: FSharpAttribute) =
        let ai = AttributeInfo()
        ai.Type <- attr.AttributeType.FullName
        ai.Arguments <- 
            List(attr.ConstructorArguments |> Seq.map (fun (t,v) -> ArgumentInfo(Type=typeFullName t, Value=v)))
        ai.NamedArguments <-
            List(attr.NamedArguments |> Seq.map (fun (t,n,_,v) -> NamedArgumentInfo(Type=typeFullName t, Name=n, Value=v)))
        if attrAllowedByFilter attr then Some ai
        else None

    /// F# syntax for specified attributes.
    let attrsSyntax withNewline (attrs: seq<FSharpAttribute>) = 
        let newline = if withNewline then "\n" else ""
        let attrsStr =
            attrs
            |> Seq.filter attrAllowedByFilter
            |> Seq.map (fun a -> 
                let dispName = 
                    if a.AttributeType.DisplayName.EndsWith("Attribute", StringComparison.Ordinal) then
                        a.AttributeType.DisplayName.[0 .. a.AttributeType.DisplayName.Length-"Attribute".Length-1]
                    else
                        a.AttributeType.DisplayName
                let argStr = 
                    [for (_,value) in a.ConstructorArguments do yield sprintf "%A" value
                     for (_,name,_,value) in a.NamedArguments do yield sprintf "%s=%A" name value]
                    |> String.concat ", " 
                if argStr.Length > 0 then sprintf "%s(%s)" dispName argStr
                else dispName)
            |> String.concat "; "
        if attrsStr.Length > 0 then "[<" + attrsStr + ">]" + newline
        else ""        

    /// AttributeFilterData for F# attributes.
    let attrsFilterData (attrs: seq<FSharpAttribute>) =
        attrs
        |> Seq.map (fun attr ->
            let data = AttributeFilterData()
            data.Id <- attr.AttributeType.FullName
            data.ConstructorArguments <- 
                attr.ConstructorArguments 
                |> Seq.map (fun (_,value) -> sprintf "%A" value)
            data.ConstructorNamedArguments <-
                attr.NamedArguments 
                |> Seq.map (fun (_,name,_,value) -> name, sprintf "%A" value)
                |> Map.ofSeq
            data)

    /// True if specified type is unit type.
    let rec isUnitType (typ: FSharpType) =
        if typ.HasTypeDefinition then 
            if typ.TypeDefinition.IsFSharpAbbreviation then
                isUnitType typ.TypeDefinition.AbbreviatedType
            else 
                match typ.TypeDefinition.TryFullName with
                | Some name -> name = "Microsoft.FSharp.Core.Unit"
                | _ -> false
        else false        

    /// Metadata for an F# parameter.
    let paramMetadata (p: FSharpParameter) =                    
        ApiParameter(Name=p.DisplayName, Type=typeRef p.Type, Attributes=List(p.Attributes |> Seq.choose attrMetadata))

    /// Metadata for an F# generic parameter.
    let genericParamMetadata (gp: FSharpGenericParameter) =
        let prefix = if gp.IsSolveAtCompileTime then "^" else "'"
        ApiParameter(Name=prefix + gp.DisplayName)

    /// Metadata for F# curried parameters.
    let curriedParamsMetadata (cpgs: IList<IList<FSharpParameter>>) =
        List(cpgs |> Seq.collect (Seq.map paramMetadata))

    /// The inheritance hierarchy of the specified F# type returned as references.
    let rec inheritanceHierarchy (t: FSharpType option) = seq {
        match t with
        | Some t when t.IsAbbreviation ->
            yield! inheritanceHierarchy (Some t.AbbreviatedType)
        | Some t ->
            yield! inheritanceHierarchy t.BaseType
            yield typeRef t
        | None -> ()
    }

    /// Inhertitable members of the specified F# type returned as references.
    let inhertiableMembers (t: FSharpType) = 
        t.TypeDefinition.MembersFunctionsAndValues
        |> Seq.filter (fun mem -> mem.Accessibility.IsPublic && not mem.IsExtensionMember && 
                                    not mem.IsConstructor && not mem.IsPropertyGetterMethod && 
                                    not mem.IsPropertySetterMethod &&
                                    (mem.IsProperty || mem.IsMember))
        |> Seq.map (fun mem -> symbolRef (symbolNames mem mem.EnclosingEntity.Value))

    /// All members (transitively) inherited by the specified F# type returned as references.
    let rec inheritedMembers filter (t: FSharpType option) = seq {
        match t with
        | Some t when filter |> List.contains t.TypeDefinition.DisplayName -> ()
        | Some t when t.IsAbbreviation ->
            yield! inheritedMembers filter (Some t.AbbreviatedType)
        | Some t ->
            yield! inheritedMembers filter t.BaseType
            yield! inhertiableMembers t
        | None -> ()
    }

    /// Extracts documentation from XML docstrings and adds it to the specified MetadataItem.
    member internal __.ExtractXmlDoc (md: MetadataItem) (xmlDoc: IList<string>) (xmlDocSig: string) =
        
        /// Resolves a cref within the XML documenation.
        let resolveCRef xmlRef =
            let result = 
                if Regex.Match(xmlRef, @"^[A-Z]:").Success then
                    // If an XML comment id of the form "?:xxx" was specified, then we perform an exact 
                    // search over all comment ids.
                    allSymbolNames |> Seq.tryFind (fun s -> s.XmlDocSig = xmlRef)
                else
                    // Otherwise we assume that a (partial) identifier was specified.
                    let isMethodRef = xmlRef.Contains "("
                    // Remove type identifier and argument list of own XML comment id.
                    let pp = md.CommentId.[2..]
                    let pp = if pp.Contains "(" then pp.[0 .. pp.IndexOf('(')-1] else pp
                    let parts = pp.Split '.'                   
                    // Try prefixed identifier.
                    [parts.Length .. -1 .. 0]
                    |> Seq.tryPick (fun numParts ->
                        let prefixedRef = 
                            Array.append parts.[0 .. numParts-1] [|xmlRef|] |> String.concat "."
                        allSymbolNames |> Seq.tryFind (fun s ->
                            let cand = s.XmlDocSig.[2..]
                            let cand =
                                if not isMethodRef && cand.Contains "(" then cand.[0 .. cand.IndexOf("(")-1]
                                else cand
                            cand = prefixedRef))
            match result with
            | Some s -> CRefTarget(Id=symbolRef s, CommentId=s.XmlDocSig)
            | None -> 
                Log.warning "Cross reference \"%s\" for %s defined in %s Line %d could not be resolved."
                            xmlRef md.Source.Name md.Source.Path md.Source.StartLine
                CRefTarget(Id=literalRef xmlRef)

        md.CommentId <- xmlDocSig
        md.RawComment <- xmlDoc |> String.concat "\n"

        if Regex.Match(md.RawComment, @"</.+>").Success then
            // If comment contains tags, try to process it as XML documentation comment.
            let fullXml = sprintf "<member name=\"%s\">%s</member>" md.CommentId md.RawComment
            let context = 
                TripleSlashCommentParserContext(PreserveRawInlineComments=false,
                                                ResolveCRef=(fun cRef -> resolveCRef cRef),
                                                Source=md.Source)
            match TripleSlashCommentModel.CreateModel(fullXml, SyntaxLanguage.FSharp, context) with
            | null -> 
                Log.warning "XML triple-slash-comment parsing error for %s defined in %s Line %d, treating raw text as summary."
                            md.Source.Name md.Source.Path md.Source.StartLine
                md.Summary <- md.RawComment
            | cm ->
                md.CommentModel <- cm
                md.Summary <- cm.Summary
                md.Remarks <- cm.Remarks
                md.Exceptions <- cm.Exceptions
                md.Sees <- cm.Sees
                md.SeeAlsos <- cm.SeeAlsos
                md.Examples <- cm.Examples
                md.IsInheritDoc <- cm.IsInheritDoc
                if not (isNull md.Syntax) then
                    if not (isNull md.Syntax.Parameters) then
                        for pmd in md.Syntax.Parameters do
                            if cm.Parameters.ContainsKey pmd.Name then
                                pmd.Description <- cm.Parameters.[pmd.Name]
                    if not (isNull md.Syntax.TypeParameters) then
                        for pmd in md.Syntax.TypeParameters do
                            if cm.TypeParameters.ContainsKey pmd.Name then
                                pmd.Description <- cm.TypeParameters.[pmd.Name]
                    if not (isNull md.Syntax.Return) then
                        md.Syntax.Return.Description <- cm.Returns
        elif md.RawComment.Length > 0 then
            /// Otherwise treat whole comment as summary.
            md.Summary <- md.RawComment

    /// Extract MetadataItem for an F# symbol.
    member internal this.SymbolMetadata (encEnt: FSharpEntity) (encMd: MetadataItem) (sym: FSharpSymbol) =
        let names = symbolNames sym encEnt
        let dispName = sym.DisplayName
        let nameWithType = encEnt.DisplayName + "." + sym.DisplayName
        let fullName = sym.FullName

        // extract information common to all symbol types
        let symMd = MetadataItem()
        symMd.Syntax <- SyntaxDetail()
        symMd.Name <- names.Name
        symMd.Language <- SyntaxLanguage.FSharp
        symMd.DisplayNames <- names.DisplayName |> syn
        symMd.DisplayNamesWithType <- names.DisplayNameWithType |> syn
        symMd.DisplayQualifiedNames <- names.DisplayQualifiedName |> syn
        symMd.NamespaceName <- encMd.NamespaceName
        symMd.AssemblyNameList <- encMd.AssemblyNameList
        match sym.DeclarationLocation with
        | Some dl -> symMd.Source <- srcDetail symMd.Name dl
        | None -> ()

        let filterData = SymbolFilterData()

        // extract type-specific information
        let mutable skip = false
        match sym with
        | :? FSharpField as field when field.Accessibility.IsPublic && field.DisplayName <> "value__"  ->   
            // F# field
            symMd.Type <- MemberType.Field
            symMd.Syntax.Content <- 
                match field.LiteralValue with
                | Some lv -> sprintf "val %s = %A" dispName lv 
                | None -> sprintf "val %s: %s" dispName (typeSyntax false field.FieldType)                 
                |> syn
            symMd.Syntax.Return <- 
                ApiParameter(Type=typeRef field.FieldType, Attributes=List(field.FieldAttributes |> Seq.choose attrMetadata))
            this.ExtractXmlDoc symMd field.XmlDoc field.XmlDocSig
            
            filterData.Kind <- Nullable ExtendedSymbolKind.Field
            filterData.Attributes <- attrsFilterData field.PropertyAttributes

        | :? FSharpUnionCase as case when case.Accessibility.IsPublic ->
            // F# union case
            symMd.Type <- MemberType.Property
            symMd.Syntax.Content <- 
                sprintf "| %s%s" dispName (unionCaseFieldsSyntax false case.UnionCaseFields) |> syn
            symMd.Syntax.Parameters <-
                case.UnionCaseFields
                |> Seq.map (fun f -> ApiParameter(Name=(if f.Name <> "Item" then f.Name else ""), 
                                                    Type=typeRef f.FieldType))
                |> List
            this.ExtractXmlDoc symMd case.XmlDoc case.XmlDocSig

            filterData.Kind <- Nullable ExtendedSymbolKind.Member
            filterData.Attributes <- attrsFilterData case.Attributes

        | :? FSharpMemberOrFunctionOrValue as mem when 
                mem.Accessibility.IsPublic && 
                not (mem.IsPropertyGetterMethod || mem.IsPropertySetterMethod ||
                     mem.IsEventAddMethod || mem.IsEventRemoveMethod) ->
            // F# member of module, class or interface 
            // (module function, module value, constructor, method, property, event)
            let logicalName = 
                mem.LogicalEnclosingEntity.AccessPath + "." + (entityNames mem.LogicalEnclosingEntity).DisplayName
            let baseName = 
                encMd.Name + "." + 
                (if mem.IsExtensionMember then "___" + logicalName + "." else "") +
                (match Seq.tryHead mem.ImplementedAbstractSignatures with
                    | Some ias -> ias.DeclaringType.TypeDefinition.FullName + "." 
                    | None -> "") +
                (if mem.IsConstructor then "#ctor" else mem.DisplayName)
            symMd.Syntax.Parameters <- curriedParamsMetadata mem.CurriedParameterGroups
            symMd.Syntax.TypeParameters <- List(mem.GenericParameters |> Seq.map genericParamMetadata)
            if not (isUnitType mem.ReturnParameter.Type) then 
                symMd.Syntax.Return <- paramMetadata mem.ReturnParameter
            symMd.IsExplicitInterfaceImplementation <- mem.IsExplicitInterfaceImplementation
            symMd.Attributes <- List(mem.Attributes |> Seq.choose attrMetadata)
            this.ExtractXmlDoc symMd mem.XmlDoc mem.XmlDocSig

            // add overload reference
            if mem.FullType.IsFunctionType || mem.IsProperty then
                symMd.Overload <- baseName + "*"
                let refParts = 
                    [LinkItem(DisplayName=dispName, DisplayNamesWithType=nameWithType, DisplayQualifiedNames=fullName)]
                let ref = ReferenceItem(CommentId = "Overload:" + symMd.Overload,
                                        Parts = SortedList(Map[SyntaxLanguage.FSharp, List(refParts)]))            
                addReference symMd.Overload ref

            // add override reference
            match Seq.tryHead mem.ImplementedAbstractSignatures with
            | Some ias -> 
                // Determining the overridden member of the base class or interface is non-trivial,
                // because it requires substitution and matching of generic parameters and arguments.
                // Since we cannot do it reliably, for now we just choose the candidate member that
                // best matches the remaining (non-generic) parameters.
                let candMems = 
                    ias.DeclaringType.TypeDefinition.MembersFunctionsAndValues
                    |> Seq.filter (fun iasMem -> iasMem.IsInstanceMember && iasMem.IsProperty = mem.IsProperty && 
                                                 not (iasMem.IsPropertyGetterMethod || iasMem.IsPropertySetterMethod ||
                                                      iasMem.IsEventAddMethod || iasMem.IsEventRemoveMethod) &&
                                                 iasMem.DisplayName = mem.DisplayName)
                let matchingParmsCount (cpgs: IList<IList<FSharpParameter>>) =
                    Seq.zip cpgs mem.CurriedParameterGroups |> Seq.sumBy (fun (cpg, memCpg) ->
                        Seq.zip cpg memCpg |> Seq.sumBy (fun (p, memP) ->
                            if p.IsOptionalArg = memP.IsOptionalArg && p.IsOutArg = memP.IsOutArg &&
                               p.IsParamArrayArg = memP.IsParamArrayArg &&
                               typeSyntax true p.Type = typeSyntax true memP.Type then 1
                            else 0))
                let bestMatchingCount = 
                    candMems 
                    |> Seq.groupBy (fun cm -> matchingParmsCount cm.CurriedParameterGroups) 
                    |> Seq.sortByDescending fst
                match Seq.tryHead bestMatchingCount with
                | Some (_, bestMatching) when Seq.length bestMatching = 1 ->
                    let overridden = Seq.exactlyOne bestMatching
                    symMd.Overridden <- symbolRef (symbolNames overridden overridden.EnclosingEntity.Value)
                | Some (_, bestMatching) ->
                    Log.verbose "Cannot uniquely determine what member is overriden by %s" symMd.Name
                    let overridden = Seq.head bestMatching
                    symMd.Overridden <- symbolRef (symbolNames overridden overridden.EnclosingEntity.Value)
                | None ->
                    Log.verbose "Cannot determine what member is overridden by %s" symMd.Name
            | None -> ()          

            // generate names and syntax
            let nonIndexProp = mem.IsProperty && mem.CurriedParameterGroups.[0].Count = 0
            let arrow = if mem.CurriedParameterGroups.Count > 0 && not nonIndexProp then " -> " else ""
            let dispParams = if nonIndexProp then "" else curriedParamSyntax false false mem.CurriedParameterGroups
            let syntaxParams = if nonIndexProp then "" else curriedParamSyntax true false mem.CurriedParameterGroups
            let dispType = dispParams + arrow + (typeSyntax false mem.ReturnParameter.Type)
            let syntaxType = syntaxParams + arrow + (typeSyntax false mem.ReturnParameter.Type)
            let propertyOps =
                [if mem.HasGetterMethod then yield "get"
                 if mem.HasSetterMethod then yield "set"]
                |> String.concat ", "
            let mods = 
                [if not mem.IsInstanceMember then yield "static "
                 if mem.IsDispatchSlot then yield "abstract "]
                |> String.concat ""            
            let ifType fullType = 
                match Seq.tryHead mem.ImplementedAbstractSignatures with
                | Some ias when ias.DeclaringType.TypeDefinition.IsInterface -> 
                    sprintf "interface %s with " (typeSyntax fullType ias.DeclaringType)
                | Some ias when ias.DeclaringType.TypeDefinition.FullName = encEnt.FullName -> 
                    "default "
                | Some _ -> "override "
                | None -> ""
            let isEvent = 
                mem.Attributes |> Seq.exists (fun a -> a.AttributeType.DisplayName = "CLIEventAttribute")
            let ifDispType = ifType false
            let atrStr = attrsSyntax true mem.Attributes   
            if mem.IsConstructor then 
                symMd.Type <- MemberType.Constructor
                symMd.Syntax.Content <- sprintf "new: %s" syntaxType |> syn
                if mem.IsImplicitConstructor then
                    // Copy documentation for parameters and return value from enclosing type.
                    // This is necessary, since F# has no way to explicitly provide XML
                    // documentation for the implicit constructor.
                    symMd.Summary <- "Implicit constructor."
                    if encMd.CommentModel <> null then
                        for p in symMd.Syntax.Parameters do
                            if encMd.CommentModel.Parameters.ContainsKey p.Name then
                                p.Description <- encMd.CommentModel.Parameters.[p.Name]
                        symMd.Syntax.Return.Description <- encMd.CommentModel.Returns
                filterData.Kind <- Nullable ExtendedSymbolKind.Member
            elif mem.IsExtensionMember then 
                symMd.Type <- if mem.IsProperty then MemberType.Property else MemberType.Method
                symMd.IsExtensionMethod <- true
                if mem.IsProperty then
                    symMd.Syntax.Content <- sprintf "%sextension %s.%s: %s with %s" atrStr logicalName dispName dispType propertyOps |> syn
                else
                    symMd.Syntax.Content <- sprintf "%sextension %s.%s: %s" atrStr logicalName dispName syntaxType |> syn
                filterData.Kind <- Nullable ExtendedSymbolKind.Member                    
            elif isEvent then
                symMd.Type <- MemberType.Event
                let evtType = mem.ReturnParameter.Type.GenericArguments.[0]
                let dispType = typeSyntax false evtType
                symMd.Syntax.Content <- sprintf "%s%s%sevent %s: %s" atrStr ifDispType mods dispName dispType |> syn                    
                symMd.Syntax.Return <- ApiParameter(Type=typeRef evtType)                    
                filterData.Kind <- Nullable ExtendedSymbolKind.Event
            elif mem.IsProperty then
                symMd.Type <- MemberType.Property
                symMd.Syntax.Content <- sprintf "%s%s%sproperty %s: %s with %s" atrStr ifDispType mods dispName dispType propertyOps |> syn                    
                filterData.Kind <- Nullable ExtendedSymbolKind.Property
            elif mem.IsMember then
                symMd.Type <- if mem.CompiledName.StartsWith("op_") then MemberType.Operator else MemberType.Method
                symMd.Syntax.Content <- sprintf "%s%s%smember %s: %s" atrStr ifDispType mods dispName syntaxType |> syn
                if encEnt.IsDelegate && mem.DisplayName = "Invoke" && encMd.CommentModel <> null then
                    for p in symMd.Syntax.Parameters do
                        if encMd.CommentModel.Parameters.ContainsKey p.Name then
                            p.Description <- encMd.CommentModel.Parameters.[p.Name]
                    symMd.Syntax.Return.Description <- encMd.CommentModel.Returns
                filterData.Kind <- Nullable ExtendedSymbolKind.Method
            elif mem.IsActivePattern then
                symMd.Type <- MemberType.Method               
                if mem.ReturnParameter.Type.HasTypeDefinition && 
                   mem.ReturnParameter.Type.TypeDefinition.TryFullName 
                   |> Option.forall (fun fn -> fn.StartsWith "Microsoft.FSharp.Core.FSharpChoice") then
                    // active pattern with multiple choices
                    let choiceNames = mem.DisplayName.Trim('|', '(', ')', ' ').Split('|')
                    let choiceTypes = mem.ReturnParameter.Type.GenericArguments                    
                    let choiceSyntax =
                        Seq.zip choiceNames choiceTypes
                        |> Seq.map (fun (choiceName, choiceType) ->
                            if isUnitType choiceType then
                                sprintf "    | %s" choiceName
                            else
                                sprintf "    | %s of %s" choiceName (typeSyntax false choiceType))
                        |> String.concat "\n"                   
                    symMd.Syntax.Content <- sprintf "%sval %s: %s ->\n%s" atrStr dispName syntaxParams choiceSyntax |> syn                
                else
                    // active pattern with single choice
                    symMd.Syntax.Content <- sprintf "%sval %s: %s" atrStr dispName syntaxType |> syn                
                filterData.Kind <- Nullable ExtendedSymbolKind.Member
            elif mem.FullType.IsFunctionType then
                symMd.Type <- MemberType.Method
                symMd.Syntax.Content <- sprintf "%sval %s: %s" atrStr dispName syntaxType |> syn
                filterData.Kind <- Nullable ExtendedSymbolKind.Method
            else
                symMd.Type <- MemberType.Field
                symMd.Syntax.Content <- sprintf "%sval %s: %s" atrStr dispName (typeSyntax false mem.FullType) |> syn
                filterData.Kind <- Nullable ExtendedSymbolKind.Field

            filterData.Attributes <- attrsFilterData mem.Attributes
        | _ -> skip <- true

        filterData.Id <- symMd.Name
        if not skip && filter.CanVisitApi filterData then Some symMd
        else None


    /// Creates a MetadataItem for an F# entity.
    member internal this.EntityMetadata (ent: FSharpEntity) =
        let md = MetadataItem(Syntax=SyntaxDetail(), Items=List())
        let names = entityNames ent   
        let dispName = names.DisplayName     

        // map F# type to DocFX member type
        let typ, extendedSymbolKind =
            if ent.IsFSharpModule then MemberType.Class, ExtendedSymbolKind.Class
            elif ent.IsFSharpRecord then MemberType.Class, ExtendedSymbolKind.Class
            elif ent.IsFSharpUnion then MemberType.Class, ExtendedSymbolKind.Class
            elif ent.IsEnum then MemberType.Enum, ExtendedSymbolKind.Enum
            elif ent.IsFSharpExceptionDeclaration then MemberType.Class, ExtendedSymbolKind.Class
            elif ent.IsFSharpAbbreviation then MemberType.Class, ExtendedSymbolKind.Class
            elif ent.IsMeasure then MemberType.Class, ExtendedSymbolKind.Class
            elif ent.IsDelegate then MemberType.Delegate, ExtendedSymbolKind.Delegate
            elif ent.IsClass then MemberType.Class, ExtendedSymbolKind.Class
            elif ent.IsInterface then MemberType.Interface, ExtendedSymbolKind.Interface
            elif ent.IsValueType then MemberType.Struct, ExtendedSymbolKind.Struct
            elif ent.IsOpaque then MemberType.Class, ExtendedSymbolKind.Class
            else failwithf "entity %A has unsupported type" ent            

        // extract basic information
        md.Name <- names.Name
        md.Source <- srcDetail md.Name ent.DeclarationLocation
        md.Attributes <- List(ent.Attributes |> Seq.choose attrMetadata)
        md.NamespaceName <- entityNamespace ent
        md.AssemblyNameList <- List([assemblyName])
        md.Type <- typ
        md.Language <- SyntaxLanguage.FSharp        
        md.DisplayNames <- names.DisplayNameWithSuffix |> syn
        md.DisplayNamesWithType <- names.DisplayNameWithType |> syn
        md.DisplayQualifiedNames <- names.DisplayQualifiedName |> syn
        md.Syntax.TypeParameters <- List(ent.GenericParameters |> Seq.map genericParamMetadata)        
        this.ExtractXmlDoc md ent.XmlDoc ent.XmlDocSig

        // extract inheritance and implementation information
        let filteredIfs = ["Object"; "ValueType"]
        let fsFilteredIfs = 
            filteredIfs @ ["IEquatable"; "IComparable"; "ISerializable"; "Exception";
                           "IStructuralEquatable"; "IStructuralComparable"]
        if not ent.IsFSharpModule && not ent.IsFSharpAbbreviation && not ent.IsMeasure && not ent.IsDelegate then
            md.Inheritance <- List(inheritanceHierarchy ent.BaseType)
            if ent.IsFSharpRecord || ent.IsFSharpUnion || ent.IsFSharpExceptionDeclaration then
                md.InheritedMembers <- List(inheritedMembers fsFilteredIfs ent.BaseType |> Seq.sort)
                md.Implements <- List(ent.AllInterfaces 
                                      |> Seq.filter (fun i -> not (fsFilteredIfs |> List.contains i.TypeDefinition.DisplayName))
                                      |> Seq.map typeRef |> Seq.sort)
            elif ent.IsInterface then
                let implIfs = ent.AllInterfaces |> Seq.filter (fun ift -> ift.TypeDefinition.FullName <> ent.FullName)
                md.InheritedMembers <- List(implIfs |> Seq.collect inhertiableMembers |> Seq.sort)
                md.Implements <- List(implIfs |> Seq.map typeRef |> Seq.sort)
            else
                md.InheritedMembers <- List(inheritedMembers filteredIfs ent.BaseType |> Seq.sort)
                md.Implements <- List(ent.AllInterfaces |> Seq.map typeRef |> Seq.sort)

        // gather metadata for contained members, fields, functions, union cases, etc.
        let getMds syms = syms |> Seq.choose (this.SymbolMetadata ent md) |> Seq.cache
        let memberMds = getMds ent.MembersFunctionsAndValues
        let fieldMds = getMds ent.FSharpFields
        let caseMds = getMds ent.UnionCases
        if not ent.IsFSharpAbbreviation then
            md.Items.AddRange(memberMds)
            md.Items.AddRange(fieldMds)
            md.Items.AddRange(caseMds) 

        // generate F# declaration syntax
        let atrStr = attrsSyntax true ent.Attributes
        let defStr (mds: seq<MetadataItem>) =
            mds
            |> Seq.map (fun md -> "\n    " + md.Syntax.Content.[SyntaxLanguage.FSharp])
            |> String.concat ""
        let inheritStr =
            match ent.BaseType with
            | Some bt when bt.TypeDefinition.DisplayName <> "obj" && bt.TypeDefinition.DisplayName <> "exn" -> 
                "\n    inherit " + typeSyntax false bt
            | _ -> ""
        let ifStr filter =
            ent.AllInterfaces
            |> Seq.filter (fun itf -> not ent.IsInterface || itf.TypeDefinition.FullName <> ent.FullName)
            |> Seq.filter (fun itf -> not (filter |> List.contains itf.TypeDefinition.DisplayName))
            |> Seq.map (fun itf -> "\n    " + (if ent.IsInterface then "inherit " else "interface ") + typeSyntax false itf)
            |> String.concat ""
        if ent.IsClass || (ent.IsValueType && not ent.IsEnum) then
            md.Syntax.Content <- sprintf "%stype %s%s%s" atrStr dispName inheritStr (ifStr []) |> syn
            // generate syntax from primary constructor and copy documentation
            for mem in ent.MembersFunctionsAndValues do
                if mem.IsImplicitConstructor && mem.Accessibility.IsPublic then
                    let pars = 
                        mem.CurriedParameterGroups.[0]
                        |> Seq.map (paramSyntax true false) 
                        |> String.concat ", "
                    md.Syntax.Content <- sprintf "%stype %s (%s)%s%s" atrStr dispName pars inheritStr (ifStr []) |> syn
                    md.Syntax.Parameters <- curriedParamsMetadata mem.CurriedParameterGroups
                    if md.CommentModel <> null then
                        for p in md.Syntax.Parameters do
                            if md.CommentModel.Parameters.ContainsKey p.Name then
                                p.Description <- md.CommentModel.Parameters.[p.Name]
        elif ent.IsInterface then
            md.Syntax.Content <- sprintf "%sinterface %s%s%s" atrStr dispName inheritStr (ifStr fsFilteredIfs) |> syn
        elif ent.IsFSharpModule then
            md.Syntax.Content <- sprintf "%smodule %s" atrStr dispName |> syn
        elif ent.IsFSharpRecord then
            md.Syntax.Content <- sprintf "%srecord %s%s%s" atrStr dispName (ifStr fsFilteredIfs) (defStr fieldMds) |> syn
        elif ent.IsFSharpUnion then
            md.Syntax.Content <- sprintf "%sunion %s%s%s" atrStr dispName (ifStr fsFilteredIfs) (defStr caseMds) |> syn
        elif ent.IsEnum then
            md.Syntax.Content <- sprintf "%senum %s%s" atrStr dispName (defStr fieldMds) |> syn
        elif ent.IsFSharpExceptionDeclaration then
            md.Syntax.Content <- sprintf "%sexception %s%s" atrStr dispName (defStr fieldMds) |> syn        
        elif ent.IsDelegate then
            let args = 
                ent.FSharpDelegateSignature.DelegateArguments
                |> Seq.map (function
                            | Some name, typ -> sprintf "%s:%s" name (typeSyntax false typ)
                            | None, typ -> typeSyntax false typ)
                |> String.concat " * "
            let ret = ent.FSharpDelegateSignature.DelegateReturnType |> typeSyntax false
            md.Syntax.Content <- sprintf "%stype %s = delegate of %s -> %s" atrStr dispName args ret |> syn
        elif ent.IsFSharpAbbreviation then
            md.Syntax.Content <- sprintf "%stype %s = %s" atrStr dispName (typeSyntax false ent.AbbreviatedType) |> syn
    
        // prepare filter data
        let filterData = SymbolFilterData()
        filterData.Id <- md.Name     
        filterData.Kind <- Nullable extendedSymbolKind
        filterData.Attributes <- attrsFilterData ent.Attributes              

        // check if entity should be filtered out
        if ent.IsFSharpModule && Seq.isEmpty md.Items then
            // skip modules that only contain nested types
            None
        elif not (filter.CanVisitApi filterData) then
            // filtered out by user specified filter
            None
        else
            addReferenceFromMetadata md
            Some md   

    /// Generate MetadataItem for namespace containing the specified entities.    
    member internal __.NamespaceMetadata name (containedMds: seq<MetadataItem>) =
        let md = MetadataItem()

        // create namespace metadata
        md.Type <- MemberType.Namespace
        md.Name <- name
        md.CommentId <- "N:" + name
        md.DisplayNames <- name |> syn
        md.DisplayNamesWithType <- name |> syn
        md.DisplayQualifiedNames <- name |> syn
        md.AssemblyNameList <- List([assemblyName])
        md.Items <- List(containedMds)

        // prepare filter data
        let filterData = SymbolFilterData()
        filterData.Id <- md.Name     
        filterData.Kind <- Nullable ExtendedSymbolKind.Namespace
        filterData.Attributes <- Seq.empty   

        if filter.CanVisitApi filterData then
            addReferenceFromMetadata md
            Some md
        else
            None

    /// Metadata for the assembly of this F# compilation.
    member internal this.AssemblyMetadata () =
        let md = MetadataItem()

        // basic assembly information
        md.Type <- MemberType.Assembly
        md.Name <- assemblyName
        md.DisplayNames <- assemblyName |> syn
        md.DisplayNamesWithType <- assemblyName |> syn
        md.DisplayQualifiedNames <- assemblyQualifiedName |> syn
        md.Language <- SyntaxLanguage.FSharp
        md.References <- references  // DocFX uses a global reference list for each assembly

        // Extract metadata for all F# entities within this compilation.
        let topLevelMds = List()
        let toProcess = Stack(compilation.AssemblySignature.Entities)
        while toProcess.Count > 0 do
            let ent = toProcess.Pop()
            if ent.Accessibility.IsPublic then
                if (entityNamespace ent).Length > 0 then
                    this.EntityMetadata ent |> Option.iter topLevelMds.Add 
                    for subEnt in ent.PublicNestedEntities do
                        toProcess.Push subEnt
                else
                    Log.warning "Skipping F# type or module %s becase it is not within a namespace." 
                                ent.DisplayName

        // Group F# entities by namespace and create namespace metadata.
        md.Items <-
            topLevelMds
            |> Seq.groupBy (fun md -> md.NamespaceName)
            |> Seq.choose (fun (nsName, mds) -> this.NamespaceMetadata nsName mds)                
            |> List
        md

    /// Generates a MetadataItem for the assembly corresponding to this F# compilation.
    /// <param name="parameters">Build parameters.</param>
    member this.ExtractMetadata (parameters: IInputParameters) =
        Log.verbose "Extracting F# metadata for assembly %s" assemblyName

        filter <- ConfigFilterRule.LoadWithDefaults parameters.Options.FilterConfigFile
        references <- Dictionary<string, ReferenceItem>()
        let md = this.AssemblyMetadata()

        Log.verbose "F# metadata for assembly %s extracted" assemblyName
        md

    override this.GetBuildController() =
        FSharpBuildController (this) :> IBuildController

    member internal __.Compilation = compilation


/// <summary>Build controller for an F# compilation.</summary>
/// <param name="compilation">the F# compilation</param>
and FSharpBuildController (compilation: FSharpCompilation) =
    interface IBuildController with
        member __.ExtractMetadata parameters =
            compilation.ExtractMetadata parameters

