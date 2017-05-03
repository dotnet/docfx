// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.UniversalReference
{
    using Microsoft.DocAsCode.DataContracts.Common;

    using AutoMapper;

    public class ApiNamesProfile : Profile
    {
        public ApiNamesProfile(string[] supportedLanguages)
        {
            CreateMap<ReferenceViewModel, ApiNames>()
                .ForMember(dest => dest.Metadata, opt => opt.MapFrom(src => src.Additional))
                .ForMember(dest => dest.Spec, opt => opt.ResolveUsing(new ApiNamesSpecResolver(supportedLanguages)))
                .ForMember(dest => dest.Name, opt => opt.Ignore())
                .ForMember(dest => dest.NameWithType, opt => opt.Ignore())
                .ForMember(dest => dest.FullName, opt => opt.Ignore())
                .AfterMap((src, dest) =>
                {
                    dest.Name = ModelConverter.ToApiListInDevLangs(src.Name, src.NameInDevLangs, supportedLanguages);
                    dest.NameWithType = ModelConverter.ToApiListInDevLangs(src.NameWithType, src.NameWithTypeInDevLangs, supportedLanguages);
                    dest.FullName = ModelConverter.ToApiListInDevLangs(src.FullName, src.FullNameInDevLangs, supportedLanguages);
                });
        }
    }
}
