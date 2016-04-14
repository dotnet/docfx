// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
function transform(model, _attrs) {
  // Clean up unused predefined properties
  model.newFileRepository = undefined;
  model._docfxVersion = undefined;

  // Clean up open publishing internal used properties
  for (var key in model)
  {
    if (key.indexOf("_op_") == 0)
    {
      model[key] = undefined;
    }
  }

  return {
    content: JSON.stringify(model)
  };
}
