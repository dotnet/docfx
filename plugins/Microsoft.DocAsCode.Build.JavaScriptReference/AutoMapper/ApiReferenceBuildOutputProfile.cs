// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.JavaScriptReference
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.DataContracts.Common;

    using AutoMapper;

    public class ApiReferenceBuildOutputProfile : Profile
    {
        public ApiReferenceBuildOutputProfile(string[] supportedLanguages)
        {
            var apiLanguageValuePairTypeConverter = new ApiLanguageValuePairTypeConverter(supportedLanguages);

            CreateMap<ReferenceViewModel, ApiReferenceBuildOutput>()
                .ForMember(dest => dest.Metadata, opt => opt.MapFrom(src => src.Additional))
                .ForMember(dest => dest.Spec,
                    opt => opt.ResolveUsing(new ApiReferenceBuildOutputSpecResolver(supportedLanguages)))
                // Null should be mapped to null
                .ForMember(dest => dest.Name, opt => opt.Condition(src => (src.Name != null)))
                .ForMember(dest => dest.NameWithType, opt => opt.Condition(src => (src.NameWithType != null)))
                .ForMember(dest => dest.FullName, opt => opt.Condition(src => (src.FullName != null)));
            CreateMap<string, List<ApiLanguageValuePair>>()
                .ConvertUsing(apiLanguageValuePairTypeConverter);
        }
    }
}
