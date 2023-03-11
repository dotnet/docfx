// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

exports.transform = function (model) {

  for (var key in model) {
    if (key[0] === '_') {
      delete model[key]
    }
  }

  return {
    content: JSON.stringify(model)
  };
}
