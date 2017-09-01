// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;

    using Microsoft.DocAsCode.Common;

    internal class PreprocessorWithResourcePool : ITemplatePreprocessor
    {
        private readonly ResourcePoolManager<ITemplatePreprocessor> _preprocessorPool;

        public PreprocessorWithResourcePool(Func<ITemplatePreprocessor> creater, int maxParallelism)
        {
            _preprocessorPool = ResourcePool.Create(creater, maxParallelism);
            try
            {
                using (var preprocessor = _preprocessorPool.Rent())
                {
                    var inner = preprocessor.Resource;
                    ContainsGetOptions = inner.ContainsGetOptions;
                    ContainsModelTransformation = inner.ContainsModelTransformation;
                    Path = inner.Path;
                    Name = inner.Name;
                }
            }
            catch (Exception e)
            {
                _preprocessorPool = null;
                Logger.LogWarning($"Not a valid template preprocessor, ignored: {e.Message}");
            }
        }

        public bool ContainsGetOptions { get; }

        public bool ContainsModelTransformation { get; }

        public string Path { get; }

        public string Name { get; }

        public object GetOptions(object model)
        {
            if (!ContainsGetOptions)
            {
                return null;
            }

            using (var lease = _preprocessorPool.Rent())
            {
                try
                {
                    return lease.Resource.GetOptions(model);
                }
                catch (Exception e)
                {
                    throw new InvalidPreprocessorException($"Error running GetOptions function inside template preprocessor: {e.Message}");
                }
            }
        }

        public object TransformModel(object model)
        {
            if (!ContainsModelTransformation)
            {
                return model;
            }

            using (var lease = _preprocessorPool.Rent())
            {
                try
                {
                    return lease.Resource.TransformModel(model);
                }
                catch (Exception e)
                {
                    throw new InvalidPreprocessorException($"Error running GetOptions function inside template preprocessor: {e.Message}");
                }
            }
        }
    }
}
