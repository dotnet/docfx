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
open System.Reflection
open System
open System.Reflection.Metadata


/// List extensions
module List =

    /// Concatenates a list of lists with the specified separator. 
    let rec concatWith sep lst =
        match lst with
        | f::s::t -> f @ sep @ concatWith sep (s::t)
        | [l] -> l
        | [] -> []


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
    let rec containedModules (ents: FSharpEntity seq) = seq {
        for ent in ents do
            if ent.IsFSharpModule then
                yield ent
                yield! containedModules ent.NestedEntities        
    }

    /// all (nested) modules within this F# compilation
    let allModules =
        containedModules compilation.AssemblySignature.Entities

    /// references used within this assembly
    let mutable references = Dictionary<string, ReferenceItem>()

    /// namespaces defined within this assembly
    let mutable knownNamespaces = HashSet<string>()

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
        let postfixTypes = ["option"; "FSharp.Collections.list"]
        let literal s = LinkItem(DisplayName=s, DisplayNamesWithType=s, DisplayQualifiedNames=s)

        if typ.HasTypeDefinition then
            let td = typ.TypeDefinition
            let trimmedAp =
                if td.AccessPath.StartsWith("Microsoft.FSharp.Core") then ""
                elif td.AccessPath.StartsWith("Microsoft.FSharp") then td.AccessPath.["Microsoft.".Length..] + "."
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
        SortedList(Map[SyntaxLanguage.FSharp, substGenericParameters value
                       SyntaxLanguage.Default, substGenericParameters value])

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
                knownNamespaces
                |> Seq.filter ent.AccessPath.StartsWith
                |> Seq.maxBy (fun ns -> ns.Length)
            let modPart = ent.AccessPath.[nsPart.Length+1..]
            sprintf "%s.%s+%s" nsPart (modPart.Replace('.', '+')) ent.DisplayName
        else
            ent.QualifiedName

    /// Namespace of the specified F# entity.
    let entityNamespace (ent: FSharpEntity) =
        match ent.Namespace with
        | Some ns -> 
            knownNamespaces.Add ns |> ignore
            ns
        | None ->
            let pos = (entityQualifiedName ent).LastIndexOf('.')
            if pos = -1 then ""
            else (entityQualifiedName ent).[0 .. pos-1]

    /// Resolve module path, i.e. remove "Module"-suffix if necessary.                
    let rec resolveModulePath fullName =
        let ent =
            allModules 
            |> Seq.find (fun ent -> ent.IsFSharpModule && ent.FullName = fullName)
        let ns = entityNamespace ent
        if ent.AccessPath.Length > ns.Length then
            resolveModulePath ent.AccessPath + "." + ent.DisplayName
        else
            ns + "." + ent.DisplayName

    /// Display name of an F# entity.
    let entityDisplayName (ent: FSharpEntity) =
        let genStr =
            ent.GenericParameters
            |> Seq.map (fun gp -> "'" + gp.Name)
            |> String.concat ", "
            |> fun s -> if s.Length > 0 then "<" + s + ">" else s
        let displayGenName = ent.DisplayName + genStr
        let ns = entityNamespace ent
        if ent.AccessPath.Length > ns.Length then
            // entity is located within an F# module
            let ap = resolveModulePath ent.AccessPath
            ap.[ns.Length+1..] + "." + displayGenName
        else
            displayGenName

    /// Reference to an (non-extension) F# member.
    let memberRef (mem: FSharpMemberOrFunctionOrValue) =
        if mem.IsExtensionMember then failwithf "%A must not be an extension" mem
        let ent = mem.EnclosingEntity.Value

        // generate name
        let iasName =
            if mem.IsOverrideOrExplicitInterfaceImplementation then 
                match Seq.tryHead mem.ImplementedAbstractSignatures with
                | Some ias -> ias.DeclaringType.TypeDefinition.FullName + "." 
                | _ -> ""
             else ""
        let baseName = ent.FullName + "." + iasName + mem.DisplayName
        let name = baseName + "(" + curriedParamSyntax false true mem.CurriedParameterGroups + ")"
        let dispName = mem.DisplayName
        let nameWithType = ent.DisplayName + "." + mem.DisplayName
        let fullName = mem.FullName

        // generate display names
        let typeStr = 
            if mem.IsProperty then "property"
            elif mem.IsMember then "member"
            else failwithf "%A must be method or property" mem 
        let nonIndexProp = mem.IsProperty && mem.CurriedParameterGroups.[0].Count = 0
        let arrow = if mem.CurriedParameterGroups.Count > 0 && not nonIndexProp then " -> " else ""
        let dispType = 
            (if nonIndexProp then "" else curriedParamSyntax false false mem.CurriedParameterGroups) + 
            arrow + (typeSyntax false mem.ReturnParameter.Type)
        let fullType = 
            (if nonIndexProp then "" else curriedParamSyntax false true mem.CurriedParameterGroups) + 
            arrow + (typeSyntax true mem.ReturnParameter.Type)
        let ifDispType, ifFullType =
            if mem.IsExplicitInterfaceImplementation then
                let ias = mem.ImplementedAbstractSignatures.[0]
                sprintf "interface %s with " (typeSyntax false ias.DeclaringType),
                sprintf "interface %s with " (typeSyntax true ias.DeclaringType)
            else "", ""
        let parts =
            [LinkItem(Name=name, 
                      DisplayName=sprintf "%s%s %s: %s" ifDispType typeStr dispName dispType,
                      DisplayNamesWithType=sprintf "%s%s %s: %s" ifDispType typeStr nameWithType dispType,
                      DisplayQualifiedNames=sprintf "%s%s %s: %s" ifFullType typeStr fullName fullType)]               

        // generate ReferenceItem and add reference
        let ri = ReferenceItem(Parts=SortedList(Map[SyntaxLanguage.FSharp, List(parts);
                                                    SyntaxLanguage.CSharp, List(parts)]))
        addReference name ri
        name

    /// Extract MetadataItem for an F# attribute.
    let attrMetadata (attr: FSharpAttribute) =
        let ai = AttributeInfo()
        ai.Type <- attr.AttributeType.FullName
        ai.Arguments <- 
            List(attr.ConstructorArguments |> Seq.map (fun (t,v) -> ArgumentInfo(Type=typeFullName t, Value=v)))
        ai.NamedArguments <-
            List(attr.NamedArguments |> Seq.map (fun (t,n,_,v) -> NamedArgumentInfo(Type=typeFullName t, Name=n, Value=v)))
        ai

    /// F# syntax for specified attributes.
    let attrsSyntax withNewline (attrs: seq<FSharpAttribute>) = 
        let newline = if withNewline then "\n" else ""
        let attrsStr =
            attrs
            |> Seq.map (fun a -> 
                let dispName = 
                    if a.AttributeType.DisplayName.EndsWith("Attribute") then
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

    /// True if specified type is unit type.
    let isUnitType (typ: FSharpType) =
        if typ.HasTypeDefinition then typ.TypeDefinition.DisplayName = "unit"
        else false

    /// True if specified parameter has unit type.
    let isUnit (p: FSharpParameter) =
        isUnitType p.Type

    /// Metadata for an F# parameter.
    let paramMetadata (p: FSharpParameter) =                    
        ApiParameter(Name=p.DisplayName, Type=typeRef p.Type, Attributes=List(p.Attributes |> Seq.map attrMetadata))

    /// Metadata for an F# generic parameter.
    let genericParamMetadata (gp: FSharpGenericParameter) =
        let prefix = if gp.IsSolveAtCompileTime then "^" else "'"
        ApiParameter(Name=prefix + gp.DisplayName)

    /// Metadata for F# curried parameters.
    let curriedParamsMetadata (cpgs: IList<IList<FSharpParameter>>) =
        List(cpgs |> Seq.collect (Seq.map paramMetadata))

    /// Extracts documentation from XML docstrings and adds it to the specified MetadataItem.
    let extractXmlDoc (md: MetadataItem) (xmlDoc: IList<string>) (xmlDocSig: string) =
        md.CommentId <- xmlDocSig
        md.RawComment <- xmlDoc |> String.concat "\n"

        if Regex.Match(md.RawComment, @"</.+>").Success then
            // If comment contains tags, try to process it as XML documentation comment.
            let fullXml = sprintf "<member name=\"%s\">%s</member>" md.CommentId md.RawComment
            let addDocRef a b =
                // TODO: add reference
                Log.warning "XmlDoc AddRef: %s %s" a b
            let context = 
                TripleSlashCommentParserContext(PreserveRawInlineComments=true,
                                                AddReferenceDelegate=(fun a b -> addDocRef a b),
                                                Source=md.Source)
            match TripleSlashCommentModel.CreateModel(fullXml, SyntaxLanguage.FSharp, context) with
            | null -> 
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
                if md.Syntax <> null then
                    if md.Syntax.Parameters <> null then
                        for pmd in md.Syntax.Parameters do
                            if cm.Parameters.ContainsKey pmd.Name then
                                pmd.Description <- cm.Parameters.[pmd.Name]
                    if md.Syntax.TypeParameters <> null then
                        for pmd in md.Syntax.TypeParameters do
                            if cm.TypeParameters.ContainsKey pmd.Name then
                                pmd.Description <- cm.TypeParameters.[pmd.Name]
                    if md.Syntax.Return <> null then
                        md.Syntax.Return.Description <- cm.Returns
        elif md.RawComment.Length > 0 then
            /// Otherwise treat whole comment as summary.
            md.Summary <- md.RawComment

    /// Extract MetadataItem for an F# symbol.
    let symbolMetadata (encEnt: FSharpEntity) (encMd: MetadataItem) (sym: FSharpSymbol) =
        let dispName = sym.DisplayName
        let nameWithType = encEnt.DisplayName + "." + sym.DisplayName
        let fullName = sym.FullName

        // extract information common to all symbol types
        let symMd = MetadataItem()
        symMd.Syntax <- SyntaxDetail()
        symMd.Name <- encMd.Name + "." + sym.DisplayName
        symMd.NamespaceName <- encMd.NamespaceName
        symMd.AssemblyNameList <- encMd.AssemblyNameList
        match sym.DeclarationLocation with
        | Some dl -> symMd.Source <- srcDetail symMd.Name dl
        | None -> ()

        // extract type-specific information
        match sym with
        | :? FSharpField as field when field.Accessibility.IsPublic && field.DisplayName <> "value__"  ->   
            // F# field
            symMd.Type <- MemberType.Field
            symMd.DisplayNames <- 
                sprintf "val %s: %s" dispName (typeSyntax false field.FieldType) |> syn
            symMd.DisplayNamesWithType <- 
                sprintf "val %s: %s" nameWithType (typeSyntax false field.FieldType) |> syn
            symMd.DisplayQualifiedNames <-
                sprintf "val %s: %s" fullName (typeSyntax true field.FieldType) |> syn
            symMd.Syntax.Content <- 
                match field.LiteralValue with
                | Some lv -> sprintf "val %s = %A" dispName lv 
                | None -> sprintf "val %s: %s" dispName (typeSyntax false field.FieldType)                 
                |> syn
            symMd.Syntax.Return <- 
                ApiParameter(Type=typeRef field.FieldType, Attributes=List(field.FieldAttributes |> Seq.map attrMetadata))
            extractXmlDoc symMd field.XmlDoc field.XmlDocSig
            Some symMd

        | :? FSharpUnionCase as case when case.Accessibility.IsPublic ->
            // F# union case
            let caseFieldsStr fullType (cfs: seq<FSharpField>) = 
                cfs
                |> Seq.map (fun cf -> 
                    if cf.Name.Length > 0 && cf.Name <> "Item" then
                        sprintf "%s: %s" cf.Name (typeSyntax fullType cf.FieldType)
                    else
                        typeSyntax fullType cf.FieldType)
                |> String.concat " * "
                |> fun s -> if s.Length > 0 then " of " + s else ""
            symMd.Type <- MemberType.Property
            symMd.DisplayNames <- 
                sprintf "%s%s" dispName (caseFieldsStr false case.UnionCaseFields) |> syn
            symMd.DisplayNamesWithType <- 
                sprintf "%s%s" nameWithType (caseFieldsStr false case.UnionCaseFields) |> syn
            symMd.DisplayQualifiedNames <-
                sprintf "%s%s" fullName (caseFieldsStr true case.UnionCaseFields) |> syn
            symMd.Syntax.Content <- 
                sprintf "| %s%s" dispName (caseFieldsStr false case.UnionCaseFields) |> syn
            symMd.Syntax.Parameters <-
                case.UnionCaseFields
                |> Seq.map (fun f -> ApiParameter(Name=(if f.Name <> "Item" then f.Name else ""), 
                                                    Type=typeRef f.FieldType))
                |> List
            extractXmlDoc symMd case.XmlDoc case.XmlDocSig
            Some symMd   

        | :? FSharpMemberOrFunctionOrValue as mem when 
                mem.Accessibility.IsPublic && 
                not (mem.IsPropertyGetterMethod || mem.IsPropertySetterMethod ||
                     mem.IsEventAddMethod || mem.IsEventRemoveMethod) ->
            // F# member of module, class or interface 
            // (module function, module value, constructor, method, property, event)
            let logicalName = 
                mem.LogicalEnclosingEntity.AccessPath + "." + entityDisplayName mem.LogicalEnclosingEntity
            let baseName = 
                encMd.Name + "." + 
                (if mem.IsExtensionMember then "___" + logicalName + "." else "") +
                (match Seq.tryHead mem.ImplementedAbstractSignatures with
                    | Some ias -> ias.DeclaringType.TypeDefinition.FullName + "." 
                    | None -> "") +
                (if mem.IsConstructor then "#ctor" else mem.DisplayName)
            symMd.Name <- baseName + "(" + curriedParamSyntax false true mem.CurriedParameterGroups + ")"
            symMd.Syntax.Parameters <- curriedParamsMetadata mem.CurriedParameterGroups
            symMd.Syntax.TypeParameters <- List(mem.GenericParameters |> Seq.map genericParamMetadata)
            if not (isUnit mem.ReturnParameter) then 
                symMd.Syntax.Return <- paramMetadata mem.ReturnParameter
            symMd.IsExplicitInterfaceImplementation <- mem.IsExplicitInterfaceImplementation
            symMd.Attributes <- List(mem.Attributes |> Seq.map attrMetadata)
            extractXmlDoc symMd mem.XmlDoc mem.XmlDocSig

            // add overload reference
            if mem.FullType.IsFunctionType then
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
                    symMd.Overridden <- memberRef (Seq.exactlyOne bestMatching)
                | Some (_, bestMatching) ->
                    Log.verbose "Cannot uniquely determine what member is overriden by %s" symMd.Name
                    symMd.Overridden <- memberRef (Seq.head bestMatching)
                | None ->
                    Log.verbose "Cannot determine what member is overridden by %s" symMd.Name
            | None -> ()          

            // generate names and syntax
            let nonIndexProp = mem.IsProperty && mem.CurriedParameterGroups.[0].Count = 0
            let arrow = if mem.CurriedParameterGroups.Count > 0 && not nonIndexProp then " -> " else ""
            let dispParams = if nonIndexProp then "" else curriedParamSyntax false false mem.CurriedParameterGroups
            let fullParams = if nonIndexProp then "" else curriedParamSyntax false true mem.CurriedParameterGroups
            let syntaxParams = if nonIndexProp then "" else curriedParamSyntax true false mem.CurriedParameterGroups
            let dispType = dispParams + arrow + (typeSyntax false mem.ReturnParameter.Type)
            let fullType = fullParams + arrow + (typeSyntax true mem.ReturnParameter.Type)
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
            let ifFullType = ifType true
            let atrStr = attrsSyntax true mem.Attributes   
            if mem.IsConstructor then 
                symMd.Type <- MemberType.Constructor
                symMd.DisplayNames <- sprintf "new: %s" dispType |> syn
                symMd.DisplayNamesWithType <- sprintf "new: %s" dispType |> syn
                symMd.DisplayQualifiedNames <- sprintf "new: %s" fullType |> syn
                symMd.Syntax.Content <- sprintf "new: %s" syntaxType |> syn
                if mem.IsImplicitConstructor && encMd.CommentModel <> null then
                    for p in symMd.Syntax.Parameters do
                        if encMd.CommentModel.Parameters.ContainsKey p.Name then
                            p.Description <- encMd.CommentModel.Parameters.[p.Name]
            elif mem.IsExtensionMember then 
                symMd.Type <- if mem.IsProperty then MemberType.Property else MemberType.Method
                symMd.IsExtensionMethod <- true
                symMd.DisplayNames <- sprintf "extension %s.%s: %s" logicalName dispName dispType |> syn
                symMd.DisplayNamesWithType <- sprintf "extension %s.%s: %s" logicalName dispName dispType |> syn
                symMd.DisplayQualifiedNames <- sprintf "extension %s: %s" mem.FullName fullType |> syn 
                if mem.IsProperty then
                    symMd.Syntax.Content <- sprintf "%sextension %s.%s: %s with %s" atrStr logicalName dispName dispType propertyOps |> syn
                else
                    symMd.Syntax.Content <- sprintf "%sextension %s.%s: %s" atrStr logicalName dispName syntaxType |> syn
            elif isEvent then
                symMd.Type <- MemberType.Event
                let evtType = mem.ReturnParameter.Type.GenericArguments.[0]
                let dispType = typeSyntax false evtType
                let fullType = typeSyntax true evtType
                symMd.DisplayNames <- sprintf "%s%sevent %s: %s" ifDispType mods dispName dispType |> syn
                symMd.DisplayNamesWithType <- sprintf "%s%sevent %s: %s" ifDispType mods nameWithType dispType |> syn
                symMd.DisplayQualifiedNames <- sprintf "%s%sevent %s: %s" ifFullType mods fullName fullType |> syn
                symMd.Syntax.Content <- sprintf "%s%s%sevent %s: %s" atrStr ifDispType mods dispName dispType |> syn                    
                symMd.Syntax.Return <- ApiParameter(Type=typeRef evtType)                    
            elif mem.IsProperty then
                symMd.Type <- MemberType.Property
                symMd.DisplayNames <- sprintf "%s%sproperty %s: %s" ifDispType mods dispName dispType |> syn
                symMd.DisplayNamesWithType <- sprintf "%s%sproperty %s: %s" ifDispType mods nameWithType dispType |> syn
                symMd.DisplayQualifiedNames <- sprintf "%s%sproperty %s: %s" ifFullType mods fullName fullType |> syn
                symMd.Syntax.Content <- sprintf "%s%s%sproperty %s: %s with %s" atrStr ifDispType mods dispName dispType propertyOps |> syn                    
            elif mem.IsMember then
                symMd.Type <- if mem.CompiledName.StartsWith("op_") then MemberType.Operator else MemberType.Method
                if mem.CompiledName.StartsWith("op_Explicit") then
                    symMd.Name <- encMd.Name + "->" + (typeSyntax true mem.ReturnParameter.Type)
                symMd.DisplayNames <- sprintf "%s%smember %s: %s" ifDispType mods dispName dispType |> syn
                symMd.DisplayNamesWithType <- sprintf "%s%smember %s: %s" ifDispType mods nameWithType dispType |> syn
                symMd.DisplayQualifiedNames <- sprintf "%s%smember %s: %s" ifFullType mods fullName fullType |> syn 
                symMd.Syntax.Content <- sprintf "%s%s%smember %s: %s" atrStr ifDispType mods dispName syntaxType |> syn
                if encEnt.IsDelegate && mem.DisplayName = "Invoke" && encMd.CommentModel <> null then
                    for p in symMd.Syntax.Parameters do
                        if encMd.CommentModel.Parameters.ContainsKey p.Name then
                            p.Description <- encMd.CommentModel.Parameters.[p.Name]
                    symMd.Syntax.Return.Description <- encMd.CommentModel.Returns
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
                    symMd.DisplayNames <- sprintf "val %s: %s" dispName dispParams |> syn
                    symMd.DisplayNamesWithType <- sprintf "val %s: %s" nameWithType dispParams |> syn
                    symMd.DisplayQualifiedNames <- sprintf "val %s: %s" fullName fullParams |> syn
                    symMd.Syntax.Content <- sprintf "%sval %s: %s ->\n%s" atrStr dispName syntaxParams choiceSyntax |> syn                
                else
                    // active pattern with single choice
                    symMd.DisplayNames <- sprintf "val %s: %s" dispName dispType |> syn
                    symMd.DisplayNamesWithType <- sprintf "val %s: %s" nameWithType dispType |> syn
                    symMd.DisplayQualifiedNames <- sprintf "val %s: %s" fullName fullType |> syn
                    symMd.Syntax.Content <- sprintf "%sval %s: %s" atrStr dispName syntaxType |> syn                
            elif mem.FullType.IsFunctionType then
                symMd.Type <- MemberType.Method
                symMd.DisplayNames <- sprintf "val %s: %s" dispName dispType |> syn
                symMd.DisplayNamesWithType <- sprintf "val %s: %s" nameWithType dispType |> syn
                symMd.DisplayQualifiedNames <- sprintf "val %s: %s" fullName fullType |> syn
                symMd.Syntax.Content <- sprintf "%sval %s: %s" atrStr dispName syntaxType |> syn
            else
                symMd.Type <- MemberType.Field
                symMd.DisplayNames <- sprintf "val %s" dispName |> syn
                symMd.DisplayNamesWithType <- sprintf "val %s: %s" nameWithType (typeSyntax false mem.FullType) |> syn
                symMd.DisplayQualifiedNames <- sprintf "val %s: %s" fullName (typeSyntax true mem.FullType) |> syn
                symMd.Syntax.Content <- sprintf "%sval %s: %s" atrStr dispName (typeSyntax false mem.FullType) |> syn

            Some symMd
        | _ -> None

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
        |> Seq.map memberRef

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

    /// Creates a MetadataItem for an F# entity.
    let entityMetadata (ent: FSharpEntity) =
        let md = MetadataItem(Syntax=SyntaxDetail(), Items=List())

        // map F# type to DocFX member type
        let dispName = entityDisplayName ent
        let typ, dispSuffix =
            if ent.IsFSharpModule then MemberType.Class, " (mod)"
            elif ent.IsFSharpRecord then MemberType.Class, " (rec)"
            elif ent.IsFSharpUnion then MemberType.Class, " (union)"
            elif ent.IsEnum then MemberType.Enum, ""
            elif ent.IsFSharpExceptionDeclaration then MemberType.Class, ""
            elif ent.IsFSharpAbbreviation then MemberType.Class, " (abrv)"
            elif ent.IsMeasure then MemberType.Class, " (meas)"
            elif ent.IsDelegate then MemberType.Delegate, ""
            elif ent.IsClass then MemberType.Class, ""
            elif ent.IsInterface then MemberType.Interface, ""
            elif ent.IsValueType then MemberType.Struct, ""
            else failwithf "entity %A has unsupported type" ent            

        // extract basic information
        md.Name <- 
            if ent.IsFSharpAbbreviation then ent.AccessPath + "." + ent.DisplayName
            else ent.FullName
        md.Source <- srcDetail md.Name ent.DeclarationLocation
        md.Attributes <- List(ent.Attributes |> Seq.map attrMetadata)
        md.NamespaceName <- entityNamespace ent
        md.AssemblyNameList <- List([assemblyName])
        md.Type <- typ
        md.DisplayNames <- dispName + dispSuffix |> syn
        md.DisplayNamesWithType <- md.Name |> syn
        md.DisplayQualifiedNames <- md.Name |> syn
        md.Syntax.TypeParameters <- List(ent.GenericParameters |> Seq.map genericParamMetadata)        
        extractXmlDoc md ent.XmlDoc ent.XmlDocSig

        // extract inheritance and implementation information
        let filteredIfs = ["IEquatable"; "IComparable"; "ISerializable"; "Exception"; "ValueType";
                           "IStructuralEquatable"; "IStructuralComparable"; "Object"]
        if not ent.IsFSharpModule && not ent.IsFSharpAbbreviation && not ent.IsMeasure && not ent.IsDelegate then
            md.Inheritance <- List(inheritanceHierarchy ent.BaseType)
            if ent.IsFSharpRecord || ent.IsFSharpUnion || ent.IsFSharpExceptionDeclaration then
                md.InheritedMembers <- List(inheritedMembers filteredIfs ent.BaseType |> Seq.sort)
                md.Implements <- List(ent.AllInterfaces 
                                      |> Seq.filter (fun i -> not (filteredIfs |> List.contains i.TypeDefinition.DisplayName))
                                      |> Seq.map typeRef |> Seq.sort)
            elif ent.IsInterface then
                let implIfs = ent.AllInterfaces |> Seq.filter (fun ift -> ift.TypeDefinition.FullName <> ent.FullName)
                md.InheritedMembers <- List(implIfs |> Seq.collect inhertiableMembers |> Seq.sort)
                md.Implements <- List(implIfs |> Seq.map typeRef |> Seq.sort)
            else
                md.InheritedMembers <- List(inheritedMembers [] ent.BaseType |> Seq.sort)
                md.Implements <- List(ent.AllInterfaces |> Seq.map typeRef |> Seq.sort)

        // gather metadata for contained members, fields, functions, union cases, etc.
        let getMds syms = syms |> Seq.choose (symbolMetadata ent md) |> Seq.cache
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
            md.Syntax.Content <- sprintf "%sinterface %s%s%s" atrStr dispName inheritStr (ifStr filteredIfs) |> syn
        elif ent.IsFSharpModule then
            md.Syntax.Content <- sprintf "%smodule %s" atrStr dispName |> syn
        elif ent.IsFSharpRecord then
            md.Syntax.Content <- sprintf "%srecord %s%s%s" atrStr dispName (ifStr filteredIfs) (defStr fieldMds) |> syn
        elif ent.IsFSharpUnion then
            md.Syntax.Content <- sprintf "%sunion %s%s%s" atrStr dispName (ifStr filteredIfs) (defStr caseMds) |> syn
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
    
        // Skip modules that only contain nested types.
        if ent.IsFSharpModule && Seq.isEmpty md.Items then
            None
        else
            addReferenceFromMetadata md
            Some md   

    /// Generate MetadataItem for namespace containing the specified entities.    
    let namespaceMetadata name (containedMds: seq<MetadataItem>) =
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

        addReferenceFromMetadata md
        md

    /// Metadata for the assembly of this F# compilation.
    let assemblyMetadata() =
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
                    entityMetadata ent |> Option.iter topLevelMds.Add 
                    for subEnt in ent.PublicNestedEntities do
                        toProcess.Push subEnt
                else
                    Log.warning "Skipping F# type or module %s becase it is not within a namespace." 
                                ent.DisplayName

        // Group F# entities by namespace and create namespace metadata.
        md.Items <-
            topLevelMds
            |> Seq.groupBy (fun md -> md.NamespaceName)
            |> Seq.map (fun (nsName, mds) -> namespaceMetadata nsName mds)                
            |> List
        md


    /// Generates a MetadataItem for the assembly corresponding to this F# compilation.
    /// <param name="parameters">Build parameters.</param>
    member __.ExtractMetadata (parameters: IInputParameters) =
        Log.verbose "Extracting F# metadata for assembly %s" assemblyName

        references <- Dictionary<string, ReferenceItem>()
        knownNamespaces <- HashSet<string>()
        let md = assemblyMetadata()

        Log.verbose "F# metadata for assembly %s extracted" assemblyName
        md

    override this.GetBuildController() =
        FSharpBuildController (this) :> IBuildController


/// <summary>Build controller for an F# compilation.</summary>
/// <param name="compilation">the F# compilation</param>
and FSharpBuildController (compilation: FSharpCompilation) =
    interface IBuildController with
        member __.ExtractMetadata parameters =
            compilation.ExtractMetadata parameters

