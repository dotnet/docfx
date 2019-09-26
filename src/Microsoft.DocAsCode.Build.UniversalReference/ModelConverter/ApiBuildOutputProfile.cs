// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.UniversalReference
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.UniversalReference;

    using AutoMapper;

    public class ApiBuildOutputProfile : Profile
    {
        public ApiBuildOutputProfile(
            string[] supportedLanguages,
            IReadOnlyDictionary<string, object> metadata,
            IReadOnlyDictionary<string, ApiNames> references)
        {
            CreateMap<ItemViewModel, ApiBuildOutput>()
                .ForMember(dest => dest.Metadata, opt => opt.ResolveUsing(new ApiBuildOutputMetadataResolver(metadata)))
                .ForMember(dest => dest.Children, opt => opt.Ignore())
                .ForMember(dest => dest.Parent, opt => opt.Ignore())
                .ForMember(dest => dest.Package, opt => opt.Ignore())
                .ForMember(dest => dest.NamespaceName, opt => opt.Ignore())
                .ForMember(dest => dest.Overridden, opt => opt.Ignore())
                .ForMember(dest => dest.Name, opt => opt.Ignore())
                .ForMember(dest => dest.NameWithType, opt => opt.Ignore())
                .ForMember(dest => dest.FullName, opt => opt.Ignore())
                .ForMember(dest => dest.Source, opt => opt.Ignore())
                .ForMember(dest => dest.AssemblyNameList, opt => opt.Ignore())
                .ForMember(dest => dest.Platform, opt => opt.Ignore())
                .ForMember(dest => dest.Implements, opt => opt.Ignore())
                .ForMember(dest => dest.InheritedMembers, opt => opt.Ignore())
                .ForMember(dest => dest.ExtensionMethods, opt => opt.Ignore())
                .ForMember(dest => dest.DerivedClasses, opt => opt.Ignore())
                .ForMember(dest => dest.Exceptions, opt => opt.Ignore())
                .ForMember(dest => dest.Inheritance, opt => opt.Ignore())
                .ForMember(dest => dest.Examples, opt => opt.Condition(src => src.Examples != null))
                .ForMember(dest => dest.SeeAlsos, opt => opt.Condition(src => src.SeeAlsos != null))
                .ForMember(dest => dest.Sees, opt => opt.Condition(src => src.Sees != null))
                .AfterMap((src, dest) =>
                {
                    dest.Parent = ModelConverter.ToApiListInDevLangsResolvingApiNames(src.Parent, src.ParentInDevLangs, supportedLanguages, references);
                    dest.Package = ModelConverter.ToApiListInDevLangsResolvingApiNames(src.Package, src.PackageInDevLangs, supportedLanguages, references);
                    dest.NamespaceName = ModelConverter.ToApiListInDevLangsResolvingApiNames(src.NamespaceName, src.NamespaceNameInDevLangs, supportedLanguages, references);
                    dest.Overridden = ModelConverter.ToApiListInDevLangsResolvingApiNames(src.Overridden, src.OverriddenInDevLangs, supportedLanguages, references);

                    dest.Name = ModelConverter.ToApiListInDevLangs(src.Name, src.Names, supportedLanguages);
                    dest.NameWithType = ModelConverter.ToApiListInDevLangs(src.NameWithType, src.NamesWithType, supportedLanguages);
                    dest.FullName = ModelConverter.ToApiListInDevLangs(src.FullName, src.FullNames, supportedLanguages);
                    dest.Source = ModelConverter.ToApiListInDevLangs(src.Source, src.SourceInDevLangs, supportedLanguages);
                    dest.AssemblyNameList = ModelConverter.ToApiListInDevLangs(src.AssemblyNameList, src.AssemblyNameListInDevLangs, supportedLanguages);
                    dest.Platform = ModelConverter.ToApiListInDevLangs(src.Platform, src.PlatformInDevLangs, supportedLanguages);

                    dest.Implements = ModelConverter.ToApiListInDevLangsResolvingApiNames(src.Implements, src.ImplementsInDevLangs, supportedLanguages, references);
                    dest.InheritedMembers = ModelConverter.ToApiListInDevLangsResolvingApiNames(src.InheritedMembers, src.InheritedMembersInDevLangs, supportedLanguages, references);
                    dest.ExtensionMethods = ModelConverter.ToApiListInDevLangsResolvingApiNames(src.ExtensionMethods, src.ExtensionMethodsInDevLangs, supportedLanguages, references);
                    dest.DerivedClasses = ModelConverter.ToApiListInDevLangsResolvingApiNames(src.DerivedClasses, src.DerivedClassesInDevLangs, supportedLanguages, references);

                    dest.Exceptions = ModelConverter.ToApiListInDevlangsResolvingApiNames(src.Exceptions, src.ExceptionsInDevLangs, supportedLanguages, references);
                    dest.Inheritance = ModelConverter.ToApiListInDevLangsResolvingApiNames(src.Inheritance, src.InheritanceInDevLangs, supportedLanguages, references);
                });
            CreateMap<ReferenceViewModel, ApiBuildOutput>()
                .ForMember(dest => dest.Metadata, opt => opt.MapFrom(src => src.Additional))
                .ForMember(dest => dest.Name, opt => opt.Ignore())
                .ForMember(dest => dest.NameWithType, opt => opt.Ignore())
                .ForMember(dest => dest.FullName, opt => opt.Ignore())
                .ForMember(dest => dest.Children, opt => opt.Ignore())
                .ForMember(dest => dest.SupportedLanguages, opt => opt.Ignore())
                .ForMember(dest => dest.Type, opt => opt.Ignore())
                .ForMember(dest => dest.Source, opt => opt.Ignore())
                .ForMember(dest => dest.Documentation, opt => opt.Ignore())
                .ForMember(dest => dest.AssemblyNameList, opt => opt.Ignore())
                .ForMember(dest => dest.NamespaceName, opt => opt.Ignore())
                .ForMember(dest => dest.Summary, opt => opt.Ignore())
                .ForMember(dest => dest.Remarks, opt => opt.Ignore())
                .ForMember(dest => dest.Examples, opt => opt.Ignore())
                .ForMember(dest => dest.Syntax, opt => opt.Ignore())
                .ForMember(dest => dest.Overridden, opt => opt.Ignore())
                .ForMember(dest => dest.Overload, opt => opt.Ignore())
                .ForMember(dest => dest.Exceptions, opt => opt.Ignore())
                .ForMember(dest => dest.SeeAlsos, opt => opt.Ignore())
                .ForMember(dest => dest.SeeAlsoContent, opt => opt.Ignore())
                .ForMember(dest => dest.Sees, opt => opt.Ignore())
                .ForMember(dest => dest.Inheritance, opt => opt.Ignore())
                .ForMember(dest => dest.DerivedClasses, opt => opt.Ignore())
                .ForMember(dest => dest.Implements, opt => opt.Ignore())
                .ForMember(dest => dest.InheritedMembers, opt => opt.Ignore())
                .ForMember(dest => dest.ExtensionMethods, opt => opt.Ignore())
                .ForMember(dest => dest.Conceptual, opt => opt.Ignore())
                .ForMember(dest => dest.Platform, opt => opt.Ignore())
                .ForMember(dest => dest.Package, opt => opt.Ignore())
                .AfterMap((src, dest) =>
                {
                    dest.Name = ModelConverter.ToApiListInDevLangs(src.Name, src.NameInDevLangs, supportedLanguages);
                    dest.NameWithType = ModelConverter.ToApiListInDevLangs(src.NameWithType, src.NameWithTypeInDevLangs, supportedLanguages);
                    dest.FullName = ModelConverter.ToApiListInDevLangs(src.FullName, src.FullNameInDevLangs, supportedLanguages);
                    if (dest.Metadata.TryGetValue(Constants.PropertyName.Syntax, out object syntax))
                    {
                        dest.Syntax = Mapper.Map<SyntaxDetailViewModel, ApiSyntaxBuildOutput>(syntax as SyntaxDetailViewModel);
                        dest.Metadata.Remove(Constants.PropertyName.Syntax);
                    }
                    if (dest.Metadata.TryGetValue(Constants.PropertyName.Type, out object type))
                    {
                        dest.Type = type as string;
                        dest.Metadata.Remove(Constants.PropertyName.Type);
                    }
                    if (dest.Metadata.TryGetValue(Constants.PropertyName.Summary, out object summary))
                    {
                        dest.Summary = summary as string;
                        dest.Metadata.Remove(Constants.PropertyName.Summary);
                    }
                    if (dest.Metadata.TryGetValue(Constants.PropertyName.Platform, out object platform))
                    {
                        dest.Platform = ModelConverter.ToApiListInDevLangs(platform as List<string>, null, supportedLanguages);
                        dest.Metadata.Remove(Constants.PropertyName.Platform);
                    }
                });
            CreateMap<string, List<ApiLanguageValuePair<ApiNames>>>()
                .ConvertUsing(new ApiListInDevLangsOfApiNamesTypeConverter(supportedLanguages, references));
            CreateMap<SyntaxDetailViewModel, ApiSyntaxBuildOutput>()
                .ForMember(dest => dest.Content, opt => opt.Ignore())
                .ForMember(dest => dest.Return, opt => opt.Ignore())
                .ForMember(dest => dest.Parameters, opt => opt.Condition(src => src.Parameters != null))
                .ForMember(dest => dest.TypeParameters, opt => opt.Condition(src => src.TypeParameters != null))
                .AfterMap((src, dest) =>
                {
                    dest.Content = ModelConverter.ToApiListInDevLangs(src.Content, src.Contents, supportedLanguages);
                    dest.Return = ModelConverter.ToApiListInDevLangsResolvingApiNames(src.Return, src.ReturnInDevLangs, supportedLanguages, references);
                });
            CreateMap<ApiParameter, ApiParameterBuildOutput>()
                .ForMember(dest => dest.Type, opt => opt.Condition(src => src.Type != null));
            CreateMap<string, ApiNames>()
                .ConvertUsing(new ApiNamesTypeConverter(supportedLanguages, references));
            CreateMap<LinkInfo, ApiLinkInfoBuildOutput>()
                .ForMember(dest => dest.Type, opt =>
                {
                    opt.Condition(src => src.LinkType == LinkType.CRef);
                    opt.MapFrom(src => src.LinkId);
                })
                .ForMember(dest => dest.Url, opt =>
                {
                    opt.Condition(src => src.LinkType == LinkType.HRef);
                    opt.ResolveUsing(new ApiHrefLinkInfoBuildOutputUrlResolver());
                });
            CreateMap<InheritanceTree, ApiInheritanceTreeBuildOutput>()
                .ForMember(dest => dest.Level, opt => opt.Ignore());
        }
    }
}
