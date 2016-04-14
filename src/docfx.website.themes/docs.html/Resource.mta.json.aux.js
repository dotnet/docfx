// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
function transform(model, _attrs) {
  // Clean up unused predefined properties
  model.newFileRepository = undefined;
  model._docfxVersion = undefined;

  return {
    content: JSON.stringify(model)
  };
}
