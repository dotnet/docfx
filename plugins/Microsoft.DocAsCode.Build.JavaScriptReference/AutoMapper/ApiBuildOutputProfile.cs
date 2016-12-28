// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.JavaScriptReference
{
    using System.Collections.Generic;

    using AutoMapper;

    public class ApiBuildOutputProfile : Profile
    {
        public ApiBuildOutputProfile(string[] supportedLanguages, IReadOnlyDictionary<string, ApiReferenceBuildOutput> references = null)
        {
            CreateMap<ItemViewModel, ApiBuildOutput>()
                // Children will be mapped with special logic
                .ForMember(dest => dest.Children, opt => opt.Ignore())
                // Null should be mapped to null
                .ForMember(dest => dest.Name, opt => opt.Condition(src => (src.Name != null)))
                .ForMember(dest => dest.NameWithType, opt => opt.Condition(src => (src.NameWithType!= null)))
                .ForMember(dest => dest.FullName, opt => opt.Condition(src => (src.FullName!= null)))
                .ForMember(dest => dest.PackageNameList, opt => opt.Condition(src => (src.PackageNameList != null)))
                .ForMember(dest => dest.Examples, opt => opt.Condition(src => (src.Examples != null)))
                .ForMember(dest => dest.Exceptions, opt => opt.Condition(src => (src.Exceptions != null)))
                .ForMember(dest => dest.SeeAlsos, opt => opt.Condition(src => (src.SeeAlsos != null)))
                .ForMember(dest => dest.Sees, opt => opt.Condition(src => (src.Sees != null)))
                .ForMember(dest => dest.Inheritance, opt => opt.Condition(src => (src.Inheritance != null)))
                .ForMember(dest => dest.DerivedClasses, opt => opt.Condition(src => (src.DerivedClasses != null)))
                .ForMember(dest => dest.Implements, opt => opt.Condition(src => (src.Implements != null)))
                .ForMember(dest => dest.InheritedMembers, opt => opt.Condition(src => (src.InheritedMembers != null)))
                .ForMember(dest => dest.ExtensionMethods, opt => opt.Condition(src => (src.ExtensionMethods != null)))
                .ForMember(dest => dest.Platform, opt => opt.Condition(src => (src.Platform != null)));
            CreateMap<string, List<ApiLanguageValuePair>>()
                .ConvertUsing(new ApiLanguageValuePairTypeConverter(supportedLanguages));
            CreateMap<string, ApiReferenceBuildOutput>()
                .ConvertUsing(new ApiReferenceBuildOutputConverter(supportedLanguages, references));
            CreateMap<ExceptionInfo, ApiExceptionInfoBuildOutput>();
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
            CreateMap<SyntaxDetailViewModel, ApiSyntaxBuildOutput>();
            CreateMap<ApiParameter, ApiParameterBuildOutput>();
        }
    }
}
