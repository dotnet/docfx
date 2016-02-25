// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
function transform(model, _attrs){
  model._tocPath = _attrs._tocPath;
  model._tocRel = _attrs._tocRel;
  return model;
}
