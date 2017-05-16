// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

exports.preTransform = function (model) {
  model._disableSideFilter = true;
  return model;
}