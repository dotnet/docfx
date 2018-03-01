// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    public class SyntaxDetail
    {
        [YamlMember(Alias = "content")]
        [JsonProperty("content")]
        public SortedList<SyntaxLanguage, string> Content { get; set; }

        [YamlMember(Alias = "parameters")]
        [JsonProperty("parameters")]
        public List<ApiParameter> Parameters { get; set; }

        [YamlMember(Alias = "typeParameters")]
        [JsonProperty("typeParameters")]
        public List<ApiParameter> TypeParameters { get; set; }

        [YamlMember(Alias = "return")]
        [JsonProperty("return")]
        public ApiParameter Return { get; set; }

        public void CopyInheritedData(SyntaxDetail src)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            CopyInheritedParameterList(Parameters, src.Parameters);
            CopyInheritedParameterList(TypeParameters, src.TypeParameters);
            if (Return != null && src.Return != null)
                Return.CopyInheritedData(src.Return);
        }

        static void CopyInheritedParameterList(List<ApiParameter> dest, List<ApiParameter> src)
        {
            if (dest == null || src == null || dest.Count != src.Count)
                return;
            for (int ndx = 0; ndx < dest.Count; ndx++)
            {
                var myParam = dest[ndx];
                var srcParam = src[ndx];
                if (myParam.Name == srcParam.Name && myParam.Type == srcParam.Type)
                {
                    myParam.CopyInheritedData(srcParam);
                }
            }
        }
    }
}
