// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Validators
{
    using System;

    using Markdig.Syntax;

    internal class MarkdownLambdaObjectValidator<TObject> : IMarkdownObjectValidator where TObject : class, IMarkdownObject
    {
        private Action<IMarkdownObject> _preAction;
        private Action<TObject> _validator;
        private Action<IMarkdownObject> _postAction;

        public MarkdownLambdaObjectValidator(
            Action<TObject> validator,
            Action<IMarkdownObject> preAction,
            Action<IMarkdownObject> postAction
            )
        {
            _preAction = preAction;
            _validator = validator;
            _postAction = postAction;
        }

        public void PreValidate(IMarkdownObject markdownObject)
        {
            _preAction?.Invoke(markdownObject);
        }


        public void Validate(IMarkdownObject markdownObject)
        {
            if (markdownObject is TObject obj)
            {
                _validator?.Invoke(obj);
            }
        }

        public void PostValidate(IMarkdownObject markdownObject)
        {
            _postAction?.Invoke(markdownObject);
        }
    }
}