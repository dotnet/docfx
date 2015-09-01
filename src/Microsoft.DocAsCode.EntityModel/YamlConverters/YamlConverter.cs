// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.YamlConverters
{
    using System;
    using System.Collections.Generic;

    public static class YamlConverter
    {
        private static readonly Pipeline<FileCollection, YamlConverterContext, ConverterModel> YamlPipeline =
            Pipeline.StartWith<FileCollection, YamlConverterContext>()
            .Append(new YamlLoader())
            .Append(new UidIndexBuilder())
            .Append(new OverrideDocumentHandler())
            .AsParallel(cm => new ConverterModel(cm.BaseDir))
            .AppendParallel(new MergeApiYamlHandler(), (r, cm) => cm.AddRange(r))
            .AppendParallel(new NonApiYamlHandler(), (r, cm) => cm.AddRange(r))
            ;

        public static IEnumerable<FileModel> Convert(
            FileCollection files)
        {
            if (files == null)
            {
                throw new ArgumentNullException(nameof(files));
            }
            var context = new YamlConverterContext();
            var result = YamlPipeline.Run(files, context);
            return result.Values;
        }
    }
}
