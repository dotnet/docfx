// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.YamlConverters
{
    using System;

    using Microsoft.DocAsCode.Plugins;

    public class NonApiYamlHandler : IPipelineItem<ConverterModel, object, ConverterModel>
    {
        public ConverterModel Exec(ConverterModel arg, object context)
        {
            var result = new ConverterModel(arg.BaseDir);
            foreach (var item in arg.Values)
            {
                switch (item.Type)
                {
                    case DocumentType.Article:
                    case DocumentType.Toc:
                    case DocumentType.Override:
                        break;
                    case DocumentType.Resource:
                        result.Add(item.FileAndType, item);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown type: {item.Type.ToString()}");
                }
            }
            return result;
        }
    }
}
