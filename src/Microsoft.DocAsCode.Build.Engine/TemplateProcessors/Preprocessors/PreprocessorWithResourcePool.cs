// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;

    using Microsoft.DocAsCode.Common;

    internal class PreprocessorWithResourcePool : ITemplatePreprocessor
    {
        private readonly ITemplatePreprocessor _inner;
        private readonly ResourcePoolManager<ITemplatePreprocessor> _preprocessorPool;

        public PreprocessorWithResourcePool(Func<ITemplatePreprocessor> creater, int maxParallelism)
        {
            _preprocessorPool = ResourcePool.Create(creater, maxParallelism);
            try
            {
                using (var preprocessor = _preprocessorPool.Rent())
                {
                    _inner = preprocessor.Resource;
                }
            }
            catch (Exception e)
            {
                _preprocessorPool = null;
                Logger.LogWarning($"Not a valid template preprocessor, ignored: {e.Message}");
            }
        }

        public Func<object, object> GetOptionsFunc => GetOptions;

        public Func<object, object> TransformModelFunc => TransformModel;

        public object GetOptions(object model)
        {
            if (_inner?.GetOptionsFunc == null)
            {
                return model;
            }

            using (var lease = _preprocessorPool.Rent())
            {
                try
                {
                    return lease.Resource.GetOptionsFunc(model);
                }
                catch (Exception e)
                {
                    throw new InvalidPreprocessorException($"Error running GetOptions function inside template preprocessor: {e.Message}");
                }
            }
        }

        public object TransformModel(object model)
        {
            if (_inner?.TransformModelFunc == null)
            {
                return model;
            }

            using (var lease = _preprocessorPool.Rent())
            {
                try
                {
                    return lease.Resource.TransformModelFunc(model);
                }
                catch (Exception e)
                {
                    throw new InvalidPreprocessorException($"Error running GetOptions function inside template preprocessor: {e.Message}");
                }
            }
        }
    }
}
